using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using RoundsMod.Utils;
using UnityEngine;

namespace RoundsMod.Components
{
    public class BlackHoleNetworkMono : MonoBehaviour, IOnEventCallback
    {
        private Player cachedPlayer;
        private Block cachedBlock;
        private PhotonView photonView;
        private const byte BlackHoleEventCode = 91;
        private bool isCooldown;
        private Coroutine activeHoleCoroutine;
        private GameObject activeChargingVisual;
        private GameObject activeHoleVisual;
        // Остальные поля и инициализация компонента...
        public void SetupBlackHole(Player player, Block block)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
            cachedBlock = block;
            if (cachedBlock != null) cachedBlock.BlockAction += OnBlockActivated;
        }

        private void Start()
        {
            PhotonNetwork.AddCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.AddHook("PointStart", OnRoundStart);
        }

        private System.Collections.IEnumerator OnRoundStart(UnboundLib.GameModes.IGameModeHandler gm)
        {
            CleanupHole();
            yield break;
        }
        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType != BlockTrigger.BlockTriggerType.Default || isCooldown) return;
            if (photonView != null && !photonView.IsMine) return;
            if (cachedPlayer?.data?.dead == true) return;
            if (MainCam.instance?.cam != null)
            {
                Vector3 targetPos = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
                targetPos.z = 0f;
                if (photonView != null)
                {
                    object[] content = { photonView.ViewID, targetPos };
                    PhotonNetwork.RaiseEvent(BlackHoleEventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
                }
                StartCoroutine(CooldownRoutine());
            }
        }
        public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (photonEvent.Code != BlackHoleEventCode) return;
            object[] data = (object[])photonEvent.CustomData;
            if (photonView != null && photonView.ViewID == (int)data[0])
            {
                CleanupHole();
                activeHoleCoroutine = StartCoroutine(SpawnBlackHoleRoutine((Vector3)data[1]));
            }
        }

        private System.Collections.IEnumerator CooldownRoutine()
        {
            isCooldown = true;
            yield return new WaitForSeconds(5f);
            isCooldown = false;
        }
        private System.Collections.IEnumerator SpawnBlackHoleRoutine(Vector3 center)
        {
            activeChargingVisual = CreateCircle("BlackHoleCharging", transform.position, new Color(0.8f, 0f, 0.8f, 0.9f), 1.6f, 200);
            float chargeEnd = Time.time + 1f;
            while (Time.time < chargeEnd)
            {
                if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) { CleanupHole(); yield break; }
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
                if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) { CleanupHole(); yield break; }
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
                    var flash = CreateCircle("HoleFlash", center, new Color(0.8f, 0.2f, 0.8f, 0.6f), 1.2f, 60);
                    Destroy(flash, 0.1f);
                }
                yield return null;
            }
            CleanupHole();
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

        private void CleanupHole()
        {
            if (activeHoleCoroutine != null) { StopCoroutine(activeHoleCoroutine); activeHoleCoroutine = null; }
            if (activeChargingVisual != null) { Destroy(activeChargingVisual); activeChargingVisual = null; }
            if (activeHoleVisual != null) { Destroy(activeHoleVisual); activeHoleVisual = null; }
        }

        private void OnDisable() { CleanupHole(); isCooldown = false; }
        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.RemoveHook("PointStart", OnRoundStart);
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
            CleanupHole();
        }
    }
}