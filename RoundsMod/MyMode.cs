using BepInEx;
using ExitGames.Client.Photon;
using ModdingUtils.Extensions;
using ModdingUtils.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;
using static MyFirstRoundsMod.TimeTrackerComponent;
using static System.Net.Mime.MediaTypeNames;
namespace MyFirstRoundsMod
{
    [BepInPlugin("com.username.myfirstmod", "My First Mod", "1.0.0")]
    [BepInDependency("com.willis.rounds.unbound")]
    public class MyMod : BaseUnityPlugin
    {
        // Статическая переменная для хранения готового префаба арта карточки 
        public static Sprite CardArtSprite;

        public static CardCategory KojnerCategory;

        public static MyMod instance;

        void Start()
        {
            instance = this;
            RefreshCardSprite();

            // Регистрируем наши кастомные карты в игре через UnboundLib 
            CustomCard.BuildCard<HealthyBoyCard>();
            CustomCard.BuildCard<BlackHoleCard>();
            CustomCard.BuildCard<TimeControlCard>();
            CustomCard.BuildCard<SatelliteCard>();
        }

        // Вынесли логику в отдельный метод, который можно безопасно вызывать
        public static void RefreshCardSprite()
        {
            if (CardArtSprite == null || CardArtSprite.texture == null)
            {
                Texture2D texture = instance.LoadTextureFromResources("RoundsMod.card_art.png");
                if (texture != null)
                {
                    CardArtSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    DontDestroyOnLoad(CardArtSprite);
                }
                else
                {
                    instance.Logger.LogError("КРИТИЧЕСКАЯ ОШИБКА: Не удалось загрузить RoundsMod.card_art.png!");
                }
            }
        }



        // Вспомогательный метод для чтения бинарных данных картинки из недр скомпилированного.dll файла
        private Texture2D LoadTextureFromResources(string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null) return null;
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);

                    // --- ИСПРАВЛЕНИЕ: Убрали флаг линейного пространства, ломавший контрастность---
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.LoadImage(buffer);
                    texture.filterMode = FilterMode.Point; // Сохраняем пиксели четкими, убирая размытие Unity
                    
                                        texture.wrapMode = TextureWrapMode.Clamp;
                    return texture;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Ошибка чтения потока встроенного ресурса: {e.Message}");
                return null;
            }
        }
    }

    // Класс самой игровой карточки 
    public class HealthyBoyCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gunInfo, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false; // Запрещает брать карту несколько раз 

            // Нативное добавление +30 патронов через ModdingUtils
            gunInfo.ammo = 30;
        }

        // 1. Изменение характеристик игрока при выборе этой карты
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (player == null || player.gameObject == null || data == null || health == null) return;

            // Баффы персонажа
            data.maxHealth *= 5f;
            characterStats.movementSpeed *= 2.1f;
            characterStats.jump *= 1.4f;
            characterStats.sizeMultiplier *= 1.4f;

            // Баффы оружия
            gun.damage *= 3f;
            gun.projectileSpeed *= 3f;

            // Скорострельность поднята еще на 40% от прошлых настроек
            gun.attackSpeed *= 0.235f;

            // Физический отброс объектов от твоих пуль
            gun.knockback += 10f;

            // Тряска экрана
            gun.shake += 0.5f;
            // Восстанавливаем ХП и принудительно обновляем полоску здоровья в интерфейсе
            health.Heal(data.maxHealth);
            health.SendMessage("BuildHP", SendMessageOptions.DontRequireReceiver);

            // Настраиваем сетевой компонент телепортации (чистый, без кругов)
            var teleportNet = player.gameObject.GetComponent<KojnerTeleportNetworkMono>();
            if (teleportNet == null)
            {
                teleportNet = player.gameObject.AddComponent<KojnerTeleportNetworkMono>();
            }
            teleportNet.SetupTeleport(player, block);
        }

        // 2. Отображение характеристик в виде текста на самой карте 
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat() { positive = true, stat = "KOJNER", amount = "ABSOLUTE", simepleAmount = CardInfoStat.SimpleAmount.Some },
                new CardInfoStat() { positive = true, stat = "EZXIPIE", amount = "DISTROFIK", simepleAmount = CardInfoStat.SimpleAmount.Some },
                new CardInfoStat() { positive = true, stat = "ANDITY", amount = "RAILGUN", simepleAmount = CardInfoStat.SimpleAmount.Some },
                new CardInfoStat() { positive = true, stat = "HAMI TSUN KUNE", amount = "LIGHT SPEED", simepleAmount = CardInfoStat.SimpleAmount.Some }
            };
        }

        protected override string GetTitle() => "1% POWER OF KOJNER";
        protected override string GetDescription() => "JUST 1% POWER OF KOJNER";
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.DestructiveRed;

        // Прямое и стабильное создание арта карты из ресурсов без слетания текстуры
        protected override GameObject GetCardArt()
        {
            GameObject artObj = new GameObject("CardArt");
            var image = artObj.AddComponent<UnityEngine.UI.Image>();

            // Берем спрайт напрямую из главного класса плагина, где он инициализирован при старте
            if (MyFirstRoundsMod.MyMod.CardArtSprite != null)
            {
                image.sprite = MyFirstRoundsMod.MyMod.CardArtSprite;
            }

            image.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Затемнение под Bloom 

            // Растягивание картинки по границам карты
            RectTransform rect = artObj.GetComponent<RectTransform>() ?? artObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, 0f);
            rect.localScale = Vector3.one;

            return artObj;
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers stats)
        {
        }

        public override string GetModName() => "POWER OF KOJNER";
    }

    public class KojnerTeleportNetworkMono : MonoBehaviour, IOnEventCallback
    {
        private Player cachedPlayer;
        private Block cachedBlock;
        private PhotonView photonView;
        private const byte TeleportEventCode = 89;
        private bool isTeleporting = false;

        public void SetupTeleport(Player player, Block block)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();

            if (cachedBlock != null)
            {
                cachedBlock.BlockAction -= OnBlockActivated;
            }
            cachedBlock = block;
            if (cachedBlock != null)
            {
                cachedBlock.BlockAction += OnBlockActivated;
            }
        }

        void Start()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            // Активация только по дефолтному блоку и только для владельца персонажа
            if (triggerType == BlockTrigger.BlockTriggerType.Default && !isTeleporting)
            {
                if (photonView != null && !photonView.IsMine) return;

                if (MainCam.instance != null && MainCam.instance.cam != null)
                {
                    Vector3 mouseWorldPos = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0f;
                    SendTeleportEvent(mouseWorldPos);
                }
            }
        }

        private void SendTeleportEvent(Vector3 targetPosition)
        {
            if (photonView == null) return;

            object[] content = new object[] { photonView.ViewID, targetPosition };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(TeleportEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == TeleportEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                int targetViewID = (int)data[0];
                Vector3 targetPos = (Vector3)data[1];

                if (photonView != null && photonView.ViewID == targetViewID)
                {
                    StartCoroutine(ExecuteTeleportRoutine(targetPos));
                }
            }
        }

        private IEnumerator ExecuteTeleportRoutine(Vector3 targetPos)
        {
            isTeleporting = true;

            if (cachedPlayer != null)
            {
                // Очищаем физические силы, чтобы игрока не швыряло после телепорта
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                // Отключаем коллизию со стенами на 3 кадра
                PlayerCollision collision = cachedPlayer.GetComponentInParent<PlayerCollision>();
                if (collision != null)
                {
                    collision.IgnoreWallForFrames(3);
                }

                // ИСПРАВЛЕНИЕ ДЛЯ ОНЛАЙНА: Сбрасываем сетевую интерполяцию позиций ROUNDS.
                // Вызываем метод "ClearQuantizedObject", чтобы Photon не пытался плавно тащить тело по воздуху.
                cachedPlayer.gameObject.SendMessage("ClearQuantizedObject", SendMessageOptions.DontRequireReceiver);

                // Мгновенно перемещаем все связанные объекты на всех клиентах синхронно
                transform.position = targetPos;
                if (transform.root != null)
                {
                    transform.root.position = targetPos;
                }

                // Принудительно заставляем физический движок Unity обновить координаты в этот же миг
                Physics2D.SyncTransforms();
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.05f);
            isTeleporting = false;
        }

        void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (cachedBlock != null)
            {
                cachedBlock.BlockAction -= OnBlockActivated;
            }
        }
    }

    public class BlackHoleCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gunInfo, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false; // 
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            characterStats.movementSpeed *= 0.85f; // Твой дебафф на скорость

            var holeNet = player.gameObject.GetComponent<BlackHoleNetworkMono>();
            if (holeNet == null)
            {
                holeNet = player.gameObject.AddComponent<BlackHoleNetworkMono>();
            }
            holeNet.SetupBlackHole(player, block);
        }


        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
            new CardInfoStat() { positive = true, stat = "Pull Radius", amount = "GLOBAL-ISH",simepleAmount = CardInfoStat.SimpleAmount.Some }, // 
            new CardInfoStat() { positive = false, stat = "Movement Speed", amount = "-15%",simepleAmount = CardInfoStat.SimpleAmount.Some } // 
            };
        }

        protected override string GetTitle() => "BLACK HOLE"; // 
        protected override string GetDescription() => "Conjures a delayed gravitational anomaly at your crosshair!"; // 
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare; // 
        protected override CardThemeColor.CardThemeColorType GetTheme() =>
CardThemeColor.CardThemeColorType.EvilPurple; // 
        public override string GetModName() => "POWER OF KOJNER"; // 

        // ФИКС КАРТИНОК: Самый безопасный метод клонирования UI арта для ROUNDS 
        protected override GameObject GetCardArt()
        {
            MyFirstRoundsMod.MyMod.RefreshCardSprite();
            GameObject artObj = new GameObject("CardArt");
            var image = artObj.AddComponent<UnityEngine.UI.Image>();

            // Присваиваем наш бессмертный спрайт 
            image.sprite = MyFirstRoundsMod.MyMod.CardArtSprite;
            image.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Затемнение под Bloom 

            // --- ДОБАВЬТЕ ЭТОТ КУСОК ДЛЯ РАСТЯГИВАНИЯ КАРТИНКИ --- 
            RectTransform rect = artObj.GetComponent<RectTransform>() ?? artObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); // Привязка к левому нижнему углу 
            rect.anchorMax = new Vector2(1f, 1f); // Привязка к правому верхнему углу 
            rect.offsetMin = new Vector2(0f, 0f); // Отступ снизу и слева 
            rect.offsetMax = new Vector2(0f, 0f); // Отступ сверху и справа 
            rect.localScale = Vector3.one;        // Стандартный масштаб 
                                                  // ----------------------------------------------------- 

            return artObj;
        }



    }
    public class BlackHoleNetworkMono : MonoBehaviour, IOnEventCallback
    {
        private Player cachedPlayer;
        private Block cachedBlock;
        private PhotonView photonView;
        private const byte BlackHoleEventCode = 91;
        private bool isCooldown = false;
        private const float CooldownDuration = 5f;

        public void SetupBlackHole(Player player, Block block)
        {
            cachedPlayer = player;
            photonView = player.GetComponent<PhotonView>();

            if (cachedBlock != null)
                cachedBlock.BlockAction -= OnBlockActivated;
            cachedBlock = block;
            if (cachedBlock != null)
                cachedBlock.BlockAction += OnBlockActivated;
        }

        void Start() => PhotonNetwork.AddCallbackTarget(this);

        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType != BlockTrigger.BlockTriggerType.Default || isCooldown) return;
            if (photonView != null && !photonView.IsMine) return;

            if (MainCam.instance != null && MainCam.instance.cam != null)
            {
                Vector3 targetPos = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
                targetPos.z = 0f;
                SendBlackHoleSpawn(targetPos);
                StartCoroutine(StartCooldownRoutine());
            }
        }

        private void SendBlackHoleSpawn(Vector3 spawnPosition)
        {
            if (photonView == null) return;
            object[] content = new object[] { photonView.ViewID, spawnPosition };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(BlackHoleEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == BlackHoleEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                int targetViewID = (int)data[0];
                Vector3 spawnPos = (Vector3)data[1];
                if (photonView != null && photonView.ViewID == targetViewID)
                {
                    StartCoroutine(SpawnBlackHoleRoutine(spawnPos));
                }
            }
        }

        private IEnumerator StartCooldownRoutine()
        {
            isCooldown = true;
            yield return new WaitForSeconds(CooldownDuration);
            isCooldown = false;
        }

        private IEnumerator SpawnBlackHoleRoutine(Vector3 center)
        {
            // --- ИЗМЕНЕННЫЙ БЛОК: Динамический масштаб круга зарядки ---
            // Считываем базовый масштаб игрока (или берем единичный, если игрока нет)
            Vector3 playerScale = cachedPlayer != null ? cachedPlayer.transform.localScale : Vector3.one;

            // Вычисляем масштаб круга: размер игрока + фиксированный отступ по осям X и Y.
            // Значение 0.6f подобрано так, чтобы круг был в два раза меньше исходного, 
            // но сохранял пропорциональный видимый зазор вокруг игрока любого масштаба.
            float adaptiveScaleX = playerScale.x + 0.6f;
            float adaptiveScaleY = playerScale.y + 0.6f;

            // Создаем круг с начальными координатами
            GameObject chargingVisual = CreateDetachedCircle(transform.position, new Color(0.8f, 0f, 0.8f, 0.9f), 1f);
            if (chargingVisual != null)
            {
                chargingVisual.transform.localScale = new Vector3(adaptiveScaleX, adaptiveScaleY, 1f);
            }

            float chargeEndTime = Time.time + 1f;
            while (Time.time < chargeEndTime)
            {
                if (chargingVisual != null && cachedPlayer != null)
                {
                    chargingVisual.transform.position = cachedPlayer.transform.position;

                    // Постоянно обновляем масштаб на случай, если игрок изменил размер прямо во время зарядки
                    Vector3 currentScale = cachedPlayer.transform.localScale;
                    chargingVisual.transform.localScale = new Vector3(currentScale.x + 0.6f, currentScale.y + 0.6f, 1f);
                }
                yield return null;
            }
            if (chargingVisual != null) Destroy(chargingVisual);
            // --- КОНЕЦ ИЗМЕНЕННОГО БЛОКА ---

            // 2. Параметры дыры (ОСТАВЛЕНО БЕЗ ИЗМЕНЕНИЙ)
            float holeVisualScale = 4.0f;
            float damageRadius = 1.8f;
            float pullRadius = 28f;
            float pullForce = 12f;
            float duration = 3f;
            float damageInterval = 0.2f;
            float damagePercent = 0.02f;

            GameObject holeVisual = CreateStaticCircle(center, new Color(0.3f, 0f, 0.6f, 0.8f), holeVisualScale);
            float startTime = Time.time;
            float lastDamageTime = 0f;

            while (Time.time - startTime < duration)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(center, pullRadius);
                foreach (var col in colliders)
                {
                    if (col.gameObject == holeVisual) continue;
                    Vector2 direction = (Vector2)center - (Vector2)col.transform.position;
                    float distance = direction.magnitude;
                    if (distance < 0.3f) continue;

                    float forceFactor = Mathf.Clamp01(1f - (distance / pullRadius));

                    // 1. ПРОВЕРКА НА ИГРОКА
                    Player player = col.GetComponent<Player>();
                    if (player != null && player.data != null && !player.data.dead)
                    {
                        float moveDist = pullForce * forceFactor * Time.deltaTime;
                        player.transform.position += (Vector3)direction.normalized * moveDist;
                        continue;
                    }

                    // 2. ПРОВЕРКА НА ДИНАМИЧЕСКИЕ ОБЪЕКТЫ
                    Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
                    if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                    {
                        rb.AddForce(direction.normalized * forceFactor * pullForce * 8f, ForceMode2D.Force);
                        float moveDist = pullForce * forceFactor * Time.deltaTime * 2f;
                        col.transform.position += (Vector3)direction.normalized * moveDist;
                    }
                }

                // === ПЕРИОДИЧЕСКИЙ УРОН ===
                if (Time.time >= lastDamageTime + damageInterval)
                {
                    lastDamageTime = Time.time;
                    Collider2D[] damageColliders = Physics2D.OverlapCircleAll(center, damageRadius);
                    foreach (var col in damageColliders)
                    {
                        Player targetPlayer = col.GetComponent<Player>();
                        if (targetPlayer != null && targetPlayer.data != null && !targetPlayer.data.dead)
                        {
                            HealthHandler healthHandler = targetPlayer.GetComponent<HealthHandler>();
                            if (healthHandler != null)
                            {
                                float damage = targetPlayer.data.maxHealth * damagePercent;
                                healthHandler.TakeDamage(Vector2.up * damage, targetPlayer.transform.position);
                            }
                        }
                    }
                    CreateFlash(center);
                }
                yield return null;
            }
            if (holeVisual != null) Destroy(holeVisual);
        }


        // Создаёт отдельный круг (не дочерний) – для зарядки
        private GameObject CreateDetachedCircle(Vector3 pos, Color color, float scale)
        {
            GameObject visual = new GameObject("BlackHoleCharging");
            visual.transform.position = pos;
            SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sprite = VisualCircleGenerator.GetCircle();
            sprite.color = color;
            sprite.sortingOrder = 200;      // высокий порядок отрисовки
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            return visual;
        }

        // Создаёт статический круг в точке – для самой дыры
        private GameObject CreateStaticCircle(Vector3 pos, Color color, float scale)
        {
            GameObject visual = new GameObject("BlackHoleVisual");
            visual.transform.position = pos;
            SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sprite = VisualCircleGenerator.GetCircle();
            sprite.color = color;
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            return visual;
        }

        private void CreateFlash(Vector3 center)
        {
            GameObject flash = new GameObject("HoleFlash");
            flash.transform.position = center;
            SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = VisualCircleGenerator.GetCircle();
            sr.color = new Color(0.8f, 0.2f, 0.8f, 0.6f);
            flash.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            Destroy(flash, 0.1f);
        }

        void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (cachedBlock != null)
                cachedBlock.BlockAction -= OnBlockActivated;
        }
    }



    public class TimeControlCard : CustomCard
    {

        public override void SetupCard(CardInfo cardInfo, Gun gunInfo, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false; // <--- СЮДА! 
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo,CharacterData data, HealthHandler health, Gravity gravity, Block block,CharacterStatModifiers stats)
        {
            if (player == null || player.gameObject == null) return;

            stats.health *= 0.85f;
            TimeTrackerComponent timeTracker =player.gameObject.GetComponent<TimeTrackerComponent>() ??player.gameObject.AddComponent<TimeTrackerComponent>();
            timeTracker.SetupTracker(player, health, gunAmmo, block);
        }







        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
            new CardInfoStat() { positive = true, stat = "Rewind Time", amount = "5.0 Sec",simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat() { positive = true, stat = "Restore State", amount = "HP & Ammo",simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat() { positive = false, stat = "Max Health", amount = "-15%",simepleAmount = CardInfoStat.SimpleAmount.Some }
            };
        }

        protected override string GetTitle() => "CONTROL TIME";
        protected override string GetDescription() => "Activating your block instantly rewinds you 5 seconds into the past, restoring your exact HP and Ammo from that moment!"; 
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() =>CardThemeColor.CardThemeColorType.ColdBlue;
        protected override GameObject GetCardArt()
        {
            MyFirstRoundsMod.MyMod.RefreshCardSprite();
            GameObject artObj = new GameObject("CardArt");
            var image = artObj.AddComponent<UnityEngine.UI.Image>();

            // Присваиваем наш бессмертный спрайт 
            image.sprite = MyFirstRoundsMod.MyMod.CardArtSprite;
            image.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Затемнение под Bloom 

            // --- ДОБАВЬТЕ ЭТОТ КУСОК ДЛЯ РАСТЯГИВАНИЯ КАРТИНКИ --- 
            RectTransform rect = artObj.GetComponent<RectTransform>() ??artObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); // Привязка к левому нижнему углу 
            rect.anchorMax = new Vector2(1f, 1f); // Привязка к правому верхнему углу 
            rect.offsetMin = new Vector2(0f, 0f); // Отступ снизу и слева 
            rect.offsetMax = new Vector2(0f, 0f); // Отступ сверху и справа 
            rect.localScale = Vector3.one;        // Стандартный масштаб 
                                                  // ----------------------------------------------------- 

            return artObj;
        }





        public override string GetModName() => "POWER OF KOJNER";
    }

    public class TimeTrackerComponent : MonoBehaviour, IOnEventCallback
    {
        private struct PlayerStateSnapshot
        {
            public Vector3 position;
            public float health;
            public int ammo;
            public PlayerStateSnapshot(Vector3 pos, float hp, int am)
            {
                position = pos;
                health = hp;
                ammo = am;
            }
        }

        private Queue<PlayerStateSnapshot> stateHistory = new Queue<PlayerStateSnapshot>();
        private const int maxSavedPoints = 100;
        private Player cachedPlayer;
        private HealthHandler playerHealth;
        private GunAmmo playerAmmo;
        private Block cachedBlock;
        private Photon.Pun.PhotonView playerView;
        private bool isRewinding = false;
        private const byte RewindSyncEventCode = 90;

        public void SetupTracker(Player player, HealthHandler health, GunAmmo ammo, Block block)
        {
            cachedPlayer = player;
            playerHealth = health;
            playerAmmo = ammo;
            playerView = player.GetComponent<Photon.Pun.PhotonView>();

            // Безопасная переподписка, чтобы избежать дублирования или зависания в памяти
            if (cachedBlock != null)
            {
                cachedBlock.BlockAction -= OnBlockActivated;
            }
            cachedBlock = block;
            if (cachedBlock != null)
            {
                cachedBlock.BlockAction += OnBlockActivated;
            }
        }

        void Start()
        {
            stateHistory.Clear();
            PhotonNetwork.AddCallbackTarget(this);
            StartCoroutine(RecordPositionsRoutine());
        }

        private void OnBlockActivated(BlockTrigger.BlockTriggerType triggerType)
        {
            // Проверяем, что блок дефолтный и что способность не на кулдауне / не в процессе ревинда
            if (triggerType == BlockTrigger.BlockTriggerType.Default && !isRewinding)
            {
                ActivateRewind();
            }
        }

        private IEnumerator RecordPositionsRoutine()
        {
            while (true)
            {
                if (!isRewinding && cachedPlayer != null && cachedPlayer.data != null && !cachedPlayer.data.dead)
                {
                    float currentHp = cachedPlayer.data.health;
                    int currentAmmo = 0;
                    if (playerAmmo != null)
                    {
                        object ammoValue = typeof(GunAmmo).GetField("currentAmmo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(playerAmmo);
                        currentAmmo = ammoValue != null ? (int)ammoValue : 0;
                    }
                    stateHistory.Enqueue(new PlayerStateSnapshot(transform.position, currentHp, currentAmmo));
                    while (stateHistory.Count > maxSavedPoints)
                    {
                        stateHistory.Dequeue();
                    }
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        public void ActivateRewind()
        {
            if (isRewinding || stateHistory.Count == 0) return;
            if (playerView != null && !playerView.IsMine) return;

            PlayerStateSnapshot targetSnapshot = stateHistory.Peek();
            object[] content = new object[]
            {
 playerView.ViewID,
 targetSnapshot.position,
 targetSnapshot.health,
 targetSnapshot.ammo
            };

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(RewindSyncEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (photonEvent.Code == RewindSyncEventCode)
            {
                object[] dataArray = (object[])photonEvent.CustomData;
                int targetViewID = (int)dataArray[0];
                Vector3 targetPos = (Vector3)dataArray[1];
                float targetHp = (float)dataArray[2];
                int targetAmmo = (int)dataArray[3];

                // Проверяем существование view перед сверкой ID
                if (playerView != null && playerView.ViewID == targetViewID)
                {
                    StartCoroutine(ExecuteRewindGlobalRoutine(targetPos, targetHp, targetAmmo));
                }
            }
        }

        private IEnumerator ExecuteRewindGlobalRoutine(Vector3 targetPos, float targetHp, int targetAmmo)
        {
            isRewinding = true;
            if (cachedPlayer != null && cachedPlayer.data != null && playerView != null)
            {
                CreateVisualCircle(transform.position, new Color(0f, 0.4f, 0.9f, 0.45f));
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
                PlayerCollision playerCollision = cachedPlayer.GetComponentInParent<PlayerCollision>();
                if (playerCollision != null) playerCollision.IgnoreWallForFrames(5);

                transform.position = targetPos;
                if (transform.root != null) transform.root.position = targetPos;
                Physics2D.SyncTransforms();
                yield return new WaitForEndOfFrame();

                cachedPlayer.data.dead = false;
                cachedPlayer.data.health = targetHp;

                if (playerHealth != null)
                {
                    playerHealth.Heal(0f);
                }

                if (playerAmmo != null)
                {
                    playerAmmo.StopAllCoroutines();
                    System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    typeof(GunAmmo).GetField("isReloading", flags)?.SetValue(playerAmmo, false);
                    typeof(GunAmmo).GetField("reloadCounter", flags)?.SetValue(playerAmmo, 0f);
                    typeof(GunAmmo).GetField("reloadTimer", flags)?.SetValue(playerAmmo, 0f);
                    typeof(GunAmmo).GetField("currentAmmo", flags)?.SetValue(playerAmmo, targetAmmo);

                    playerAmmo.SendMessage("SetAmmo", targetAmmo, SendMessageOptions.DontRequireReceiver);

                    Transform UI_Root = cachedPlayer.transform.Find("Canvas") ?? transform.root.GetComponentInChildren<Canvas>()?.transform;
                    if (UI_Root != null)
                    {
                        foreach (Transform child in UI_Root)
                        {
                            if (child.name.Contains("Reload") || child.name.Contains("Indicator"))
                                Destroy(child.gameObject);
                        }
                    }
                    playerAmmo.SendMessage("Reconstruct", SendMessageOptions.DontRequireReceiver);
                }
                CreateVisualCircle(targetPos, new Color(0f, 1f, 0.6f, 0.6f));
            }
            stateHistory.Clear();
            yield return new WaitForSeconds(0.1f);
            isRewinding = false;
        }

        private void CreateVisualCircle(Vector3 pos, Color color)
        {
            GameObject visual = new GameObject("TimeVisualCircle");
            visual.transform.position = pos;
            SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sprite = VisualCircleGenerator.GetCircle();
            sprite.color = color;
            visual.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            Destroy(visual, 0.35f);
        }

        void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (cachedBlock != null)
            {
                cachedBlock.BlockAction -= OnBlockActivated;
            }
        }
    }


    public class SatelliteCard : CustomCard
    {

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats,CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = false;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo,CharacterData data, HealthHandler health, Gravity gravity, Block block,CharacterStatModifiers characterStats)
        {
            if (player == null || player.gameObject == null) return;

            player.gameObject.AddComponent<SatelliteShieldMono>();
        }





        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo,CharacterData data, HealthHandler health, Gravity gravity, Block block,CharacterStatModifiers characterStats)
        {
            SatelliteShieldMono component =player.gameObject.GetComponent<SatelliteShieldMono>();
            if (component != null)
            {
                Destroy(component);
            }
        }

        protected override string GetTitle() => "SATELLITE GUARD";
        protected override string GetDescription() => "Spawns 3 orbital satellites. Each satellite completely absorbs ONE instance of ANY lethal damage."; 
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() =>CardThemeColor.CardThemeColorType.DestructiveRed;
        protected override GameObject GetCardArt()
        {
            MyFirstRoundsMod.MyMod.RefreshCardSprite();
            GameObject artObj = new GameObject("CardArt");
            var image = artObj.AddComponent<UnityEngine.UI.Image>();

            // Присваиваем наш бессмертный спрайт 
            image.sprite = MyFirstRoundsMod.MyMod.CardArtSprite;
            image.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Затемнение под Bloom 

            // --- ДОБАВЬТЕ ЭТОТ КУСОК ДЛЯ РАСТЯГИВАНИЯ КАРТИНКИ --- 
            RectTransform rect = artObj.GetComponent<RectTransform>() ??artObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); // Привязка к левому нижнему углу 
            rect.anchorMax = new Vector2(1f, 1f); // Привязка к правому верхнему углу 
            rect.offsetMin = new Vector2(0f, 0f); // Отступ снизу и слева 
            rect.offsetMax = new Vector2(0f, 0f); // Отступ сверху и справа 
            rect.localScale = Vector3.one;        // Стандартный масштаб 
                                                  // ----------------------------------------------------- 

            return artObj;
        }







        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
            new CardInfoStat() { positive = true, stat = "Damage Blocks", amount = "3 Charges",simepleAmount = CardInfoStat.SimpleAmount.Some }
            };
        }

        public override string GetModName() => "POWER OF KOJNER";
    }

    public class SatelliteShieldMono : MonoBehaviour, IOnEventCallback
    {
        private Player player;
        private CharacterData data;
        private HealthHandler health;
        private PhotonView view; // Добавили PhotonView для сетевых проверок 
        private int satelliteCount = 3;
        private List<GameObject> satellites = new List<GameObject>();
        private float rotationSpeed = 150f;
        private float orbitRadius = 1.3f;
        private float hitCooldown = 0.4f;
        private float lastHitTime = -1f;
        private float lastFrameHealth;

        // Новый сетевой код Photon для жесткой синхронизации уничтожения спутников 
        private const byte SatelliteSyncEventCode = 92;

        private void Start()
        {
            player = GetComponent<Player>();
            data = GetComponent<CharacterData>();
            health = GetComponent<HealthHandler>();
            view = GetComponent<PhotonView>();

            PhotonNetwork.AddCallbackTarget(this); // Регистрируем сетевой слушатель 

            if (data != null) lastFrameHealth = data.health;

            UnboundLib.GameModes.GameModeManager.AddHook("PointStart",OnRoundStart);
            ResetAndSpawnSatellites();
        }

        private IEnumerator OnRoundStart(IGameModeHandler gameModeHandler)
        {
            ResetAndSpawnSatellites();
            yield break;
        }

        private void ResetAndSpawnSatellites()
        {
            foreach (var sat in satellites) { if (sat != null) Destroy(sat); }
            satellites.Clear();

            for (int i = 0; i < satelliteCount; i++)
            {
                GameObject sat = new GameObject($"KojnerSatellite_{i}");
                SpriteRenderer spriteRenderer = sat.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = VisualCircleGenerator.GetCircle();
                spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
                spriteRenderer.material.color = new Color(1f, 0.84f, 0f, 1f);
                spriteRenderer.sortingOrder = 100;
                sat.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                satellites.Add(sat);
            }
            if (data != null) lastFrameHealth = data.health;
        }

        private void Update()
        {
            if (data == null || health == null || data.dead || view == null) return;

            // ПРОВЕРКА ЗДОРОВЬЯ: Проверяет урон ТОЛЬКО хозяин персонажа (IsMine) 
            if (view.IsMine && satellites.Count > 0)
            {
                if (data.health < lastFrameHealth)
                {
                    // Если кулдаун ЕЩЕ ИДЕТ (с момента последнего реального удара прошло меньше 0.4 сек) 
                    if (Time.time < lastHitTime + hitCooldown)
                    {
                        data.health = data.maxHealth; // Просто восстанавливаем ХП, шары НЕ тратим
                    }
                    else
                    {
                        // Кулдаун ПРОШЕЛ: Засекаем время нового удара у себя 
                        lastHitTime = Time.time;
                        data.health = data.maxHealth; // Локально спасаем от летального урона 

                        // Шлем сетевой пакет: передаем ViewID, максимальное ХП и ТОЧНОЕ время удара по часам Unity
                        object[] content = new object[] { view.ViewID, data.maxHealth, Time.time };
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                        {
                            Receivers =ReceiverGroup.All
                        };
                        PhotonNetwork.RaiseEvent(SatelliteSyncEventCode, content,raiseEventOptions, SendOptions.SendReliable);
                    }
                }
            }

            // Критически важно: синхронизируем отслеживание ХП предыдущего кадра 
            if (view.IsMine)
            {
                lastFrameHealth = data.health;
            }

            // Логика вращения (без изменений) 
            if (satellites.Count == 0) return;
            float angleStep = 360f / satellites.Count;
            for (int i = 0; i < satellites.Count; i++)
            {
                if (satellites[i] == null) continue;
                float currentAngle = (Time.time * rotationSpeed) + (i * angleStep);
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad) * orbitRadius, Mathf.Sin(rad) *orbitRadius, 0f);
                satellites[i].transform.position = transform.position + offset;
            }
        }

        // Сетевой приемник пакетов (OnEvent) 
        public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (photonEvent.Code == SatelliteSyncEventCode)
            {
                object[] dataArray = (object[])photonEvent.CustomData;
                int targetViewID = (int)dataArray[0];
                float maxHp = (float)dataArray[1];
                float networkHitTime = (float)dataArray[2];

                if (view != null && view.ViewID == targetViewID)
                {
                    lastHitTime = networkHitTime;

                    if (data != null)
                    {
                        data.health = maxHp;
                        lastFrameHealth = maxHp;
                    }
                    if (health != null)
                    {
                        // Просто лечим игрока локально на экранах других клиентов, 
                        // так как данные ХП мы уже синхронизировали строчкой выше
                        health.Heal(0f);
                    }

                    // Удаляем ровно ОДИН спутник синхронно у всех игроков
                    if (satellites.Count > 0)
                    {
                        int lastIndex = satellites.Count - 1;
                        GameObject satToDestroy = satellites[lastIndex];
                        satellites.RemoveAt(lastIndex);
                        if (satToDestroy != null)
                        {
                            CreateBreakEffect(satToDestroy.transform.position);
                            Destroy(satToDestroy);
                        }
                    }
                }
            }
        }



        private void CreateBreakEffect(Vector3 position)
        {
            GameObject effect = new GameObject("BreakVisualCircle");
            effect.transform.position = position;
            SpriteRenderer sprite = effect.AddComponent<SpriteRenderer>();
            sprite.sprite = VisualCircleGenerator.GetCircle();
            sprite.material = new Material(Shader.Find("Sprites/Default"));
            sprite.color = Color.red;
            effect.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            Destroy(effect, 0.15f);
        }

        void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this); // Обязательно отключаем слушатель сети
            UnboundLib.GameModes.GameModeManager.RemoveHook("PointStart",OnRoundStart);
            foreach (var sat in satellites) { if (sat != null) Destroy(sat); }
        }
    }

    public static class VisualCircleGenerator
    {
        private static Sprite circle;
        public static Sprite GetCircle()
        {
            if (circle != null) return circle;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) <= 31f)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circle;
        }
    }


}