using UnboundLib.Cards;
using UnityEngine;
using RoundsMod.Components;

namespace RoundsMod.Cards
{
    public class KojnerPowerCard : KojnerBaseCard
    {
        protected override string ArtFileName => "kojner_power.png";
        protected override string GetTitle() => "1% POWER OF KOJNER";
        protected override string GetDescription() => "JUST 1% POWER OF KOJNER";
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.DestructiveRed;

        public override void SetupCard(CardInfo info, Gun gun, ApplyCardStats s, CharacterStatModifiers m, Block b)
        {
            info.allowMultiple = false;
            gun.ammo = 30;
        }
        public override void OnAddCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st)
        {
            if (p == null || d == null || h == null) return;
            d.maxHealth *= 5f;
            st.movementSpeed = 2.1f;
            st.jump = 1.4f;
            st.sizeMultiplier = 1.4f;
            g.damage *= 3f;
            g.projectileSpeed *= 3f;
            g.attackSpeed *= 0.235f;
            g.knockback += 10f;
            g.shake += 0.5f;
            h.Heal(d.maxHealth);
            h.SendMessage("BuildHP", SendMessageOptions.DontRequireReceiver);
            var tp = p.gameObject.GetComponent<KojnerTeleportNetworkMono>() ?? p.gameObject.AddComponent<KojnerTeleportNetworkMono>();
            tp.SetupTeleport(p, b);
        }
        protected override CardInfoStat[] GetStats() => new[]
{
            new CardInfoStat { positive = true, stat = "KOJNER", amount = "ABSOLUTE", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = true, stat = "EZXIPIE", amount = "DISTROFIK", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = true, stat = "ANDITY", amount = "RAILGUN", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = true, stat = "HAMI TSUN KUNE", amount = "LIGHT SPEED", simepleAmount = CardInfoStat.SimpleAmount.Some }
        };

        public override void OnRemoveCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st) { }
    }
}
