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
    public class TimeTrackerComponent : MonoBehaviour, IOnEventCallback, IKojnerAbility
    {
        private struct Snapshot { public Vector3 pos; public float hp; public int ammo; }
        private readonly Queue<Snapshot> history = new Queue<Snapshot>();
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo CurAmmoField = typeof(GunAmmo).GetField("currentAmmo", Flags);
        private static readonly FieldInfo IsReloadField = typeof(GunAmmo).GetField("isReloading", Flags);
        private static readonly FieldInfo ReloadCntField = typeof(GunAmmo).GetField("reloadCounter", Flags);
        private static readonly FieldInfo ReloadTmrField = typeof(GunAmmo).GetField("reloadTimer", Flags);
        private static readonly MethodInfo SetBulletsMethod = typeof(GunAmmo).GetMethod("SetActiveBullets", Flags);

        private Player cachedPlayer;
        private HealthHandler playerHealth;
        private GunAmmo playerAmmo;
        private PhotonView playerView;
        private float cooldownEndTime;
        private float nextRecordTime;
        private const byte RewindEventCode = 90;

        public string AbilityName => "CHRONO SHIFT";
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

        public void SetupTracker(Player p, HealthHandler h, GunAmmo a)
        {
            cachedPlayer = p; playerHealth = h; playerAmmo = a;
            playerView = p.GetComponent<PhotonView>();
            var mgr = p.gameObject.GetComponent<KojnerAbilityManager>() ?? p.gameObject.AddComponent<KojnerAbilityManager>();
            mgr.TryBindSlotE(this);
        }

        private void Start()
        {
            history.Clear();
            PhotonNetwork.AddCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.AddHook("PointStart", OnRoundStart);
        }

        private IEnumerator OnRoundStart(UnboundLib.GameModes.IGameModeHandler gm)
        {
            history.Clear();
            cooldownEndTime = 0f;
            yield break;
        }

        private void FixedUpdate()
        {
            if (Time.time < nextRecordTime) return;
            nextRecordTime = Time.time + 0.05f;

            if (cachedPlayer != null && cachedPlayer.data != null && !cachedPlayer.data.dead)
            {
                int ammo = (playerAmmo != null && CurAmmoField != null) ? (int)CurAmmoField.GetValue(playerAmmo) : 0;
                history.Enqueue(new Snapshot { pos = transform.position, hp = cachedPlayer.data.health, ammo = ammo });
                while (history.Count > 100) history.Dequeue();
            }
            else if (history.Count > 0)
            {
                history.Clear();
            }
        }


        public void TryActivateAbility()
        {
            if (CooldownRemaining > 0f) return;
            if (playerView != null && !playerView.IsMine) return;
            if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) return;

            Snapshot snap = history.Count > 0
                ? history.Peek()
                : new Snapshot { pos = transform.position, hp = cachedPlayer.data.health, ammo = playerAmmo != null && CurAmmoField != null ? (int)CurAmmoField.GetValue(playerAmmo) : 0 };

            cooldownEndTime = Time.time + 6f;

            if (playerView != null && PhotonNetwork.InRoom)
            {
                object[] content = { playerView.ViewID, snap.pos, snap.hp, snap.ammo };
                PhotonNetwork.RaiseEvent(RewindEventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
            else
            {
                ExecuteRewind(snap.pos, snap.hp, snap.ammo);
            }
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != RewindEventCode) return;
            object[] d = (object[])photonEvent.CustomData;
            if (playerView != null && playerView.ViewID == (int)d[0])
                ExecuteRewind((Vector3)d[1], (float)d[2], (int)d[3]);
        }

        private void ExecuteRewind(Vector3 targetPos, float targetHp, int targetAmmo)
        {
            cooldownEndTime = Time.time + 6f;
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
        }

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
        }
    }
}
