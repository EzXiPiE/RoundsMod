using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace RoundsMod.Components
{
    public class KojnerTeleportNetworkMono : MonoBehaviour, IOnEventCallback
    {
        private Player cachedPlayer;
        private Block cachedBlock;
        private PhotonView photonView;
        private static readonly byte TeleportEventCode = RoundsMod.Utils.NetEventRegistry.GetCode("KojnerMod.Teleport");
        private bool isTeleporting;

        public void SetupTeleport(Player player, Block block)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
            cachedBlock = block;
            if (cachedBlock != null) cachedBlock.BlockAction += OnBlockActivated;
        }

        private void Start() => PhotonNetwork.AddCallbackTarget(this);

        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType != BlockTrigger.BlockTriggerType.Default || isTeleporting) return;
            if (photonView != null && !photonView.IsMine) return;
            if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) return;
            if (MainCam.instance?.cam == null) return;

            Vector3 targetPos = GetSafeTeleportPos(MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition));
            if (photonView != null && PhotonNetwork.InRoom)
            {
                object[] content = { photonView.ViewID, targetPos };
                PhotonNetwork.RaiseEvent(TeleportEventCode, content, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
            else
            {
                StartCoroutine(ExecuteTeleportRoutine(targetPos));
            }

        }

        private Vector3 GetSafeTeleportPos(Vector3 rawPos)
        {
            rawPos.z = 0f;
            Vector2 start = transform.position;
            Vector2 target = rawPos;
            Vector2 dir = (start - target);
            float dist = dir.magnitude;
            Vector2 stepDir = dist > 0.01f ? dir / dist : Vector2.up;

            for (float offset = 0f; offset <= dist; offset += 0.35f)
            {
                Vector2 candidate = target + stepDir * offset;
                if (!IsInsideSolid(candidate))
                    return new Vector3(candidate.x, candidate.y, 0f);
            }
            return new Vector3(start.x, start.y, 0f);
        }
        private bool IsInsideSolid(Vector2 point)
        {
            foreach (var col in Physics2D.OverlapCircleAll(point, 0.55f))
            {
                if (col == null || col.isTrigger) continue;
                if (col.transform.root == transform.root) continue;
                if (col.attachedRigidbody != null && col.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                return true;
            }
            return false;
        }
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != TeleportEventCode) return;
            object[] data = (object[])photonEvent.CustomData;
            if (photonView != null && photonView.ViewID == (int)data[0])
                StartCoroutine(ExecuteTeleportRoutine((Vector3)data[1]));
        }

        private IEnumerator ExecuteTeleportRoutine(Vector3 targetPos)
        {
            isTeleporting = true;
            if (cachedPlayer != null)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
                cachedPlayer.GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(2);
                cachedPlayer.gameObject.SendMessage("ClearQuantizedObject", SendMessageOptions.DontRequireReceiver);
                transform.position = targetPos;
                if (transform.root != null) transform.root.position = targetPos;
                Physics2D.SyncTransforms();
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(0.05f);
            isTeleporting = false;
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
        }
    }
}
