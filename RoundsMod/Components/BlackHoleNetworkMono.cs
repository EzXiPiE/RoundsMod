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
        private static readonly byte BlackHoleEventCode = NetEventRegistry.GetCode("KojnerMod.BlackHole");
        private bool isBusy;
        private float cooldownEndTime;
        private Coroutine activeHoleCoroutine;
        private GameObject activeChargingVisual;
        private GameObject activeHoleVisual;
        private static readonly System.Reflection.FieldInfo PlayerVelField = typeof(PlayerVelocity).GetField("velocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);


        public string AbilityName => "EVENT HORIZON";
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

        public void SetupBlackHole(Player player)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();
            var mgr = player.gameObject.GetComponent<KojnerAbilityManager>() ?? player.gameObject.AddComponent<KojnerAbilityManager>();
            mgr.TryBindAbility(this);
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
                    RaycastHit2D wallHit = Physics2D.Linecast(center, col.transform.position);
                    if (wallHit.collider != null && !wallHit.collider.isTrigger && wallHit.collider.transform.root != col.transform.root && wallHit.collider.attachedRigidbody == null)
                        factor *= 0.35f;

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
            DetonateHole(center);
            CleanupVisuals();
        }
        private void DetonateHole(Vector3 center)
        {
            Destroy(CreateCircle("HoleBlastOuter", center, new Color(0.6f, 0.1f, 1f, 0.45f), 9.0f, 55), 0.18f);
            Destroy(CreateCircle("HoleBlastCore", center, new Color(1f, 0.7f, 1f, 0.9f), 3.2f, 65), 0.12f);

            foreach (var col in Physics2D.OverlapCircleAll(center, 1.8f))
            {
                var p = col.GetComponentInParent<Player>();
                if (p?.data != null && !p.data.dead)
                    p.GetComponent<HealthHandler>()?.TakeDamage(Vector2.up * (p.data.maxHealth * 0.04f), p.transform.position);
            }

            StartCoroutine(BlastWaveRoutine(center));
        }

        private System.Collections.IEnumerator BlastWaveRoutine(Vector3 center)
        {
            var pushedPlayers = new System.Collections.Generic.HashSet<Player>();
            float endTime = Time.time + 0.18f;

            while (Time.time < endTime)
            {
                float progress = 1f - ((endTime - Time.time) / 0.18f);
                float waveMultiplier = Mathf.Lerp(1f, 0.15f, progress);

                foreach (var col in Physics2D.OverlapCircleAll(center, 50f))
                {
                    if (col == null || col.gameObject == activeHoleVisual) continue;
                    Vector2 targetPoint = col.bounds.center;
                    Vector2 pushDir = targetPoint - (Vector2)center;
                    float dist = pushDir.magnitude;
                    pushDir = dist < 0.1f ? Vector2.up : pushDir / dist;

                    float falloff = Mathf.Clamp01(1f - dist / 50f);
                    if (falloff <= 0.01f) continue;

                    var p = col.GetComponentInParent<Player>();
                    if (p?.data != null && !p.data.dead && (p.data.view == null || p.data.view.IsMine))
                    {
                        if (pushedPlayers.Add(p) && p.data.playerVel != null && PlayerVelField != null)
                        {
                            Vector2 curVel = (Vector2)PlayerVelField.GetValue(p.data.playerVel);
                            PlayerVelField.SetValue(p.data.playerVel, curVel + pushDir * (120f * falloff));
                        }
                    }
                    else if ((!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) && col.attachedRigidbody != null && col.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                    {
                        col.attachedRigidbody.position += pushDir * (120f * falloff * waveMultiplier * Time.deltaTime);
                    }

                }
                yield return null;
            }
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
