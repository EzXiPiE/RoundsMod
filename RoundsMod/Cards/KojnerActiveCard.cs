using ModdingUtils.Extensions;
using UnityEngine;

namespace RoundsMod.Cards
{
    public abstract class KojnerActiveCard : KojnerBaseCard
    {
        private static CardCategory activeCategory;
        public static CardCategory ActiveCategory
        {
            get
            {
                if (activeCategory == null)
                {
                    activeCategory = ScriptableObject.CreateInstance<CardCategory>();
                    activeCategory.name = "KojnerActiveSlot";
                }
                return activeCategory;
            }
        }

        public override void Callback()
        {
            CardInfo info = gameObject.GetComponent<CardInfo>();
            if (info != null)
            {
                info.categories = new[] { ActiveCategory };
                info.GetAdditionalData().canBeReassigned = false;
            }
        }
    }
}
