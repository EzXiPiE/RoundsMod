using UnboundLib.Cards;
using UnityEngine;
using UnityEngine.UI;
using RoundsMod.Utils;

namespace RoundsMod.Cards
{
    public abstract class KojnerBaseCard : CustomCard
    {
        protected abstract string ArtFileName { get; }
        public override string GetModName() => "POWER OF KOJNER";

        protected override GameObject GetCardArt()
        {
            var artObj = new GameObject("CardArt");
            var image = artObj.AddComponent<Image>();
            image.sprite = AssetHelper.GetCardArt(ArtFileName);
            image.color = new Color(0.65f, 0.65f, 0.65f, 1f);

            var rect = artObj.GetComponent<RectTransform>() ?? artObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return artObj;
        }
    }
}
