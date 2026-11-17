using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using RoundsMod.Utils;
using UnityEngine;

namespace RoundsMod.Components
{
    public class TimeTrackerComponent : MonoBehaviour, IOnEventCallback
    {
        private struct Snapshot { public Vector3 pos; public float hp; public int ammo; }
        private readonly Queue<Snapshot> history = new Queue<Snapshot>();
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo CurAmmoField = typeof(GunAmmo).GetField("currentAmmo", Flags);
        private static readonly FieldInfo IsReloadField = typeof(GunAmmo).GetField("isReloading", Flags);
        private static readonly FieldInfo ReloadCntField = typeof(GunAmmo).GetField("reloadCounter", Flags);
        private static readonly FieldInfo ReloadTmrField = typeof(GunAmmo).GetField("reloadTimer", Flags);
        private Player cachedPlayer;
        private HealthHandler playerHealth;
        private GunAmmo playerAmmo;
        private Block cachedBlock;
        private PhotonView playerView;
        private bool isRewinding;
        private const byte RewindEventCode = 90;

        public void SetupTracker(Player p, HealthHandler h, GunAmmo a, Block b)
        {
            cachedPlayer = p; playerHealth = h; playerAmmo = a;
            playerView = p.GetComponent<PhotonView>();
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
            cachedBlock = b;
            if (cachedBlock != null) cachedBlock.BlockAction += OnBlockActivated;
        }
        private void Start()
        {
            history.Clear();
            PhotonNetwork.AddCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.AddHook("PointStart", OnRoundStart);
            StartCoroutine(RecordRoutine());
        }

        private IEnumerator OnRoundStart(UnboundLib.GameModes.IGameModeHandler gm)
        {
            history.Clear();
            yield break;
        }

        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType == BlockTrigger.BlockTriggerType.Default && !isRewinding)
                ActivateRewind();
        }
        private IEnumerator RecordRoutine()
        {
            while (true)
            {
                if (!isRewinding && cachedPlayer?.data != null && !cachedPlayer.data.dead)
                {
                    int ammo = (playerAmmo != null && CurAmmoField != null) ? (int)CurAmmoField.GetValue(playerAmmo) : 0;
                    history.Enqueue(new Snapshot { pos = transform.position, hp = cachedPlayer.data.health, ammo = ammo });
                    while (history.Count > 100) history.Dequeue();
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
        public void ActivateRewind()
        {
            if (isRewinding || history.Count == 0) return;
            if (playerView != null && !playerView.IsMine) return;
            if (cachedPlayer?.data?.dead == true) return;
            Snapshot snap = history.Peek();
            object[] content = { playerView.ViewID, snap.pos, snap.hp, snap.ammo };
            PhotonNetwork.RaiseEvent(RewindEventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != RewindEventCode) return;
            object[] d = (object[])photonEvent.CustomData;
            if (playerView != null && playerView.ViewID == (int)d[0])
                StartCoroutine(ExecuteRewindRoutine((Vector3)d[1], (float)d[2], (int)d[3]));
        }
        private IEnumerator ExecuteRewindRoutine(Vector3 targetPos, float targetHp, int targetAmmo)
        {
            isRewinding = true;
            if (cachedPlayer != null && cachedPlayer.data != null && !cachedPlayer.data.dead)
            {
                DrawEffect(transform.position, Color.cyan);
                var body = GetComponent<Rigidbody2D>();
                if (body != null) { body.velocity = Vector2.zero; body.angularVelocity = 0f; }

                cachedPlayer.GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(3);
                cachedPlayer.gameObject.SendMessage("ClearQuantizedObject", SendMessageOptions.DontRequireReceiver);
                transform.position = targetPos;
                if (transform.root != null) transform.root.position = targetPos;
                Physics2D.SyncTransforms();

                cachedPlayer.data.health = targetHp;
                playerHealth?.Heal(0f);
                ResetWeaponAmmo(targetAmmo);
                DrawEffect(targetPos, new Color(0.3f, 0.7f, 1f));
            }
            history.Clear();
            yield return new WaitForSeconds(0.1f);
            isRewinding = false;
        }
        private static readonly MethodInfo SetBulletsMethod = typeof(GunAmmo).GetMethod("SetActiveBullets", Flags);

        private void ResetWeaponAmmo(int amount)
        {
            if (playerAmmo == null) return;
            CurAmmoField?.SetValue(playerAmmo, amount);
            IsReloadField?.SetValue(playerAmmo, false);
            ReloadCntField?.SetValue(playerAmmo, 0f);
            ReloadTmrField?.SetValue(playerAmmo, 0f);
            playerAmmo.SendMessage("Reconstruct", SendMessageOptions.DontRequireReceiver);
            if (SetBulletsMethod != null)
            {
                var parameters = SetBulletsMethod.GetParameters();
                SetBulletsMethod.Invoke(playerAmmo, parameters.Length == 1 ? new object[] { true } : null);
            }
        }

        private void DrawEffect(Vector3 pos, Color tint)
        {
            var fx = new GameObject("ChronoFx");
            fx.transform.position = pos;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = VisualHelper.GetCircle();
            sr.color = tint;
            fx.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            Destroy(fx, 0.25f);
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.RemoveHook("PointStart", OnRoundStart);
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
        }
    }
}
