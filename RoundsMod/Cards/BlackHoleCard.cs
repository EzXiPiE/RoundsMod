using UnboundLib.Cards;
using UnityEngine;
using RoundsMod.Components;

namespace RoundsMod.Cards
{
    public class BlackHoleCard : KojnerBaseCard
    {
        protected override string ArtFileName => "black_hole.png";
        protected override string GetTitle() => "EVENT HORIZON";
        protected override string GetDescription() => "Blocking tears open a singularity at your cursor, dragging foes and debris into oblivion.";
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.EvilPurple;

        public override void SetupCard(CardInfo info, Gun gun, ApplyCardStats s, CharacterStatModifiers m, Block b)
        {
            info.allowMultiple = false;
        }
        public override void OnAddCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st)
        {
            if (p == null) return;
            st.movementSpeed *= 0.85f;
            var hole = p.gameObject.GetComponent<BlackHoleNetworkMono>() ?? p.gameObject.AddComponent<BlackHoleNetworkMono>();
            hole.SetupBlackHole(p, b);
        }

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat { positive = true, stat = "Pull Radius", amount = "GLOBAL-ISH", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = false, stat = "Movement Speed", amount = "-15%", simepleAmount = CardInfoStat.SimpleAmount.Some }
        };

        public override void OnRemoveCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st) { }
    }
}
