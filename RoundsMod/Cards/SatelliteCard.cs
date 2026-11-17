using UnboundLib.Cards;
using UnityEngine;
using RoundsMod.Components;

namespace RoundsMod.Cards
{
    public class SatelliteCard : KojnerBaseCard
    {
        protected override string ArtFileName => "satellite_guard.png";
        protected override string GetTitle() => "AEGIS ORBIT";
        protected override string GetDescription() => "Three orbital drones surround you. Each drone intercepts and completely negates one instance of incoming damage.";
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.TechWhite;

        public override void SetupCard(CardInfo info, Gun gun, ApplyCardStats s, CharacterStatModifiers m, Block b)
        {
            info.allowMultiple = false;
        }
        public override void OnAddCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st)
        {
            if (p == null) return;
            var guard = p.gameObject.GetComponent<SatelliteShieldMono>() ?? p.gameObject.AddComponent<SatelliteShieldMono>();
            guard.SetupShield(3);
        }

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat { positive = true, stat = "Orbital Shields", amount = "+3", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = true, stat = "Damage Negation", amount = "100% / Hit", simepleAmount = CardInfoStat.SimpleAmount.Some }
        };

        public override void OnRemoveCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st) { }
    }
}
