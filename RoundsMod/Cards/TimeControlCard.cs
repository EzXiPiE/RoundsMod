using UnboundLib.Cards;
using UnityEngine;
using RoundsMod.Components;

namespace RoundsMod.Cards
{
    public class TimeControlCard : KojnerActiveCard
    {
        protected override string ArtFileName => "time_control.png";
        protected override string GetTitle() => "CHRONO SHIFT";
        protected override string GetDescription() => "Binds to a free ability slot (Q/E/F/V). Rewind 5 seconds into the past, restoring position, health, and ammo.";
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.ColdBlue;

        public override void SetupCard(CardInfo info, Gun gun, ApplyCardStats s, CharacterStatModifiers m, Block b) => info.allowMultiple = false;

        public override void OnAddCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st)
        {
            if (p == null) return;
            st.health *= 0.85f;
            var tracker = p.gameObject.GetComponent<TimeTrackerComponent>() ?? p.gameObject.AddComponent<TimeTrackerComponent>();
            tracker.SetupTracker(p, h, a);
        }

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat { positive = true, stat = "Active Slot", amount = "5s Rewind", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = true, stat = "Restore State", amount = "HP & Ammo", simepleAmount = CardInfoStat.SimpleAmount.Some },
            new CardInfoStat { positive = false, stat = "Max Health", amount = "-15%", simepleAmount = CardInfoStat.SimpleAmount.Some }
        };

        public override void OnRemoveCard(Player p, Gun g, GunAmmo a, CharacterData d, HealthHandler h, Gravity gr, Block b, CharacterStatModifiers st) { }
    }
}
