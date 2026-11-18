using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using RoundsMod.Utils;
using UnityEngine;

namespace RoundsMod.Components
{
    public class BlackHoleNetworkMono : MonoBehaviour, IOnEventCallback, IKojnerAbility
    {
        private Player cachedPlayer;
        private PhotonView photonView;
        private const byte BlackHoleEventCode = 91;
        private bool isBusy;
        private float cooldownEndTime;
        private Coroutine activeHoleCoroutine;
        private GameObject activeChargingVisual;
        private GameObject activeHoleVisual;

        public string AbilityName => "EVENT HORIZON";
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

        public void SetupBlackHole(Player player)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();
            var mgr = player.gameObject.GetComponent<KojnerAbilityManager>() ?? player.gameObject.AddComponent<KojnerAbilityManager>();
            mgr.TryBindSlotE(this);
        }

        private void Start()
        {
            PhotonNetwork.AddCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.AddHook("PointStart", OnRoundStart);
        }

        private System.Collections.IEnumerator OnRoundStart(UnboundLib.GameModes.IGameModeHandler gm) { FullReset(); yield break; }

        public void TryActivateAbility()
        {
            if (isBusy || CooldownRemaining > 0f) return;
            if (photonView != null && !photonView.IsMine) return;
            if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) return;
            if (MainCam.instance?.cam == null) return;

            Vector3 targetPos = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
            targetPos.z = 0f;
            isBusy = true;
            cooldownEndTime = Time.time + 9f;

            if (photonView != null && PhotonNetwork.InRoom)
            {
                object[] content = { photonView.ViewID, targetPos };
                PhotonNetwork.RaiseEvent(BlackHoleEventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
            else
            {
                CleanupVisuals();
                isBusy = true;
                activeHoleCoroutine = StartCoroutine(SpawnBlackHoleRoutine(targetPos));
            }
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != BlackHoleEventCode) return;
            object[] data = (object[])photonEvent.CustomData;
            if (photonView != null && photonView.ViewID == (int)data[0])
            {
                CleanupVisuals();
                isBusy = true;
                cooldownEndTime = Time.time + 9f;
                activeHoleCoroutine = StartCoroutine(SpawnBlackHoleRoutine((Vector3)data[1]));
            }
        }

        private System.Collections.IEnumerator SpawnBlackHoleRoutine(Vector3 center)
        {
            activeChargingVisual = CreateCircle("BlackHoleCharging", transform.position, new Color(0.8f, 0f, 0.8f, 0.9f), 1.6f, 200);
            float chargeEnd = Time.time + 1f;
            while (Time.time < chargeEnd)
            {
                if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) { FullReset(); yield break; }
                if (activeChargingVisual != null)
                {
                    activeChargingVisual.transform.position = cachedPlayer.transform.position;
                    Vector3 sc = cachedPlayer.transform.localScale;
                    activeChargingVisual.transform.localScale = new Vector3(sc.x + 0.6f, sc.y + 0.6f, 1f);
                }
                yield return null;
            }
            if (activeChargingVisual != null) Destroy(activeChargingVisual);

            activeHoleVisual = CreateCircle("BlackHoleVisual", center, new Color(0.3f, 0f, 0.6f, 0.8f), 4.0f, 50);
            float start = Time.time, lastTick = 0f;
            while (Time.time - start < 3f)
            {
                if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) { FullReset(); yield break; }
                foreach (var col in Physics2D.OverlapCircleAll(center, 28f))
                {
                    if (col == null || col.gameObject == activeHoleVisual) continue;
                    Vector2 dir = (Vector2)center - (Vector2)col.transform.position;
                    if (dir.magnitude < 0.2f) continue;
                    float factor = Mathf.Clamp01(1f - dir.magnitude / 28f);
                    var p = col.GetComponent<Player>();
                    if (p?.data != null && !p.data.dead && p.data.view?.IsMine == true)
                        p.transform.position += (Vector3)dir.normalized * (25f * factor * Time.deltaTime);
                    else if (PhotonNetwork.IsMasterClient && col.attachedRigidbody?.bodyType == RigidbodyType2D.Dynamic)
                        col.attachedRigidbody.position += dir.normalized * (17f * factor * Time.deltaTime);
                }
                if (Time.time >= lastTick + 0.2f)
                {
                    lastTick = Time.time;
                    foreach (var col in Physics2D.OverlapCircleAll(center, 1.8f))
                    {
                        var p = col.GetComponent<Player>();
                        if (p?.data != null && !p.data.dead)
                            p.GetComponent<HealthHandler>()?.TakeDamage(Vector2.up * (p.data.maxHealth * 0.02f), p.transform.position);
                    }
                    Destroy(CreateCircle("HoleFlash", center, new Color(0.8f, 0.2f, 0.8f, 0.6f), 1.2f, 60), 0.1f);
                }
                yield return null;
            }
            CleanupVisuals();
        }

        private GameObject CreateCircle(string objName, Vector3 pos, Color col, float scale, int order)
        {
            var go = new GameObject(objName);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = VisualHelper.GetCircle();
            sr.color = col;
            sr.sortingOrder = order;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            return go;
        }

        private void CleanupVisuals()
        {
            if (activeHoleCoroutine != null) { StopCoroutine(activeHoleCoroutine); activeHoleCoroutine = null; }
            if (activeChargingVisual != null) { Destroy(activeChargingVisual); activeChargingVisual = null; }
            if (activeHoleVisual != null) { Destroy(activeHoleVisual); activeHoleVisual = null; }
            isBusy = false;
        }

        private void FullReset()
        {
            CleanupVisuals();
            cooldownEndTime = 0f;
        }

        private void OnDisable() => FullReset();

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.RemoveHook("PointStart", OnRoundStart);
            FullReset();
        }
    }
}
