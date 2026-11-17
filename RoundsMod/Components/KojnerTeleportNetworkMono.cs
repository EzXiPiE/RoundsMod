// Подключаем стандартные пространства имен для работы с Photon и Unity
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

namespace RoundsMod.Components
{
    // Сетевой компонент для мгновенного перемещения персонажа
    public class KojnerTeleportNetworkMono : MonoBehaviour, IOnEventCallback
    {
        private Player cachedPlayer;
        private Block cachedBlock;
        private PhotonView photonView;
        private const byte TeleportEventCode = 89;
        private bool isTeleporting;
        // Метод инициализации и привязки к компоненту блока игрока
        public void SetupTeleport(Player player, Block block)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
            cachedBlock = block;
            if (cachedBlock != null) cachedBlock.BlockAction += OnBlockActivated;
        }

        // Регистрируем слушатель сетевых событий Photon при старте
        private void Start() => PhotonNetwork.AddCallbackTarget(this);

        // Срабатывает при нажатии кнопки блока игроком
        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType != BlockTrigger.BlockTriggerType.Default || isTeleporting) return;
            if (photonView != null && !photonView.IsMine) return;
            if (cachedPlayer == null || cachedPlayer.data == null || cachedPlayer.data.dead) return;
            // Получаем мировые координаты курсора и отправляем пакет по сети
            if (MainCam.instance != null && MainCam.instance.cam != null)
            {
                Vector3 pos = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0f;
                if (photonView != null)
                {
                    object[] content = { photonView.ViewID, pos };
                    var opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(TeleportEventCode, content, opts, SendOptions.SendReliable);
                }
            }
        }

        // Принимаем сетевое событие на всех клиентах и запускаем перемещение
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != TeleportEventCode) return;
            object[] data = (object[])photonEvent.CustomData;
            if (photonView != null && photonView.ViewID == (int)data[0])
                StartCoroutine(ExecuteTeleportRoutine((Vector3)data[1]));
        }
        // Корутина синхронного телепорта со сбросом физики и сетевой интерполяции
        private IEnumerator ExecuteTeleportRoutine(Vector3 targetPos)
        {
            isTeleporting = true;
            if (cachedPlayer != null)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
                var col = cachedPlayer.GetComponentInParent<PlayerCollision>();
                col?.IgnoreWallForFrames(3);
                cachedPlayer.gameObject.SendMessage("ClearQuantizedObject", SendMessageOptions.DontRequireReceiver);
                transform.position = targetPos;
                if (transform.root != null) transform.root.position = targetPos;
                Physics2D.SyncTransforms();
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(0.05f);
            isTeleporting = false;
        }

        // Отписываемся от событий при удалении объекта
        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (cachedBlock != null) cachedBlock.BlockAction -= OnBlockActivated;
        }
    }
}
