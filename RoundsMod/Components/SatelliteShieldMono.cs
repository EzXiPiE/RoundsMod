using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using RoundsMod.Utils;
using UnityEngine;

namespace RoundsMod.Components
{
    public class SatelliteShieldMono : MonoBehaviour, IOnEventCallback
    {
        private readonly List<GameObject> orbiters = new List<GameObject>();
        private PhotonView netView;
        private CharacterData charData;
        private int maxOrbiters = 3;
        private float invulnUntil;
        private const byte ShieldBreakCode = 92;

        public void SetupShield(int count)
        {
            maxOrbiters = count;
            RebuildOrbiters();
        }

        private void Awake()
        {
            netView = GetComponent<PhotonView>();
            charData = GetComponent<CharacterData>();
        }

        private void Start()
        {
            PhotonNetwork.AddCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.AddHook("PointStart", OnRoundBegin);
            RebuildOrbiters();
        }

        private IEnumerator OnRoundBegin(UnboundLib.GameModes.IGameModeHandler gm)
        {
            RebuildOrbiters();
            yield break;
        }

        public bool TryInterceptDamage()
        {
            if (charData == null || charData.dead) return false;
            if (Time.time < invulnUntil) return true;
            if (orbiters.Count == 0) return false;

            invulnUntil = Time.time + 0.4f;
            if (netView != null && netView.IsMine)
            {
                var opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(ShieldBreakCode, netView.ViewID, opts, SendOptions.SendReliable);
            }
            return true;
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != ShieldBreakCode) return;
            if (netView == null || netView.ViewID != (int)photonEvent.CustomData) return;

            invulnUntil = Time.time + 0.4f;
            if (orbiters.Count > 0)
            {
                int idx = orbiters.Count - 1;
                GameObject target = orbiters[idx];
                orbiters.RemoveAt(idx);
                if (target != null)
                {
                    SpawnShatterFx(target.transform.position);
                    Destroy(target);
                }
            }
        }

        private void Update()
        {
            if (orbiters.Count == 0 || charData == null || charData.dead) return;
            float step = 360f / orbiters.Count;
            for (int i = 0; i < orbiters.Count; i++)
            {
                if (orbiters[i] == null) continue;
                float rad = ((Time.time * 180f) + (i * step)) * Mathf.Deg2Rad;
                orbiters[i].transform.position = transform.position + new Vector3(Mathf.Cos(rad) * 1.4f, Mathf.Sin(rad) * 1.4f, 0f);
            }
        }

        private void RebuildOrbiters()
        {
            foreach (var obj in orbiters) if (obj != null) Destroy(obj);
            orbiters.Clear();
            for (int i = 0; i < maxOrbiters; i++)
            {
                var orb = new GameObject($"Orbiter_{i}");
                var sr = orb.AddComponent<SpriteRenderer>();
                sr.sprite = VisualHelper.GetCircle();
                sr.color = new Color(1f, 0.84f, 0f, 1f);
                sr.sortingOrder = 100;
                orb.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                orbiters.Add(orb);
            }
        }

        private void SpawnShatterFx(Vector3 pos)
        {
            var fx = new GameObject("ShieldBreakFx");
            fx.transform.position = pos;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = VisualHelper.GetCircle();
            sr.color = Color.red;
            fx.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            Destroy(fx, 0.15f);
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            UnboundLib.GameModes.GameModeManager.RemoveHook("PointStart", OnRoundBegin);
            foreach (var obj in orbiters) if (obj != null) Destroy(obj);
        }
    }
}
