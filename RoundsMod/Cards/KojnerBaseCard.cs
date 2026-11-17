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
            var spr = AssetHelper.GetCardSprite(ArtFileName);
            Debug.Log($"[KOJNER ART] Card={GetTitle()}, File={ArtFileName}, SpriteNull={spr == null}");

            var artObj = new GameObject($"Art_{ArtFileName}");
            DontDestroyOnLoad(artObj);
            artObj.transform.position = new Vector3(9999f, 9999f, 0f);

            var rect = artObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var img = artObj.AddComponent<Image>();
            img.sprite = spr;
            img.color = new Color(0.65f, 0.65f, 0.65f, 1f);

            var refresher = artObj.AddComponent<ArtRefresher>();
            refresher.savedSprite = spr;

            return artObj;
        }
    }

    public class ArtRefresher : MonoBehaviour
    {
        public Sprite savedSprite;
        private void OnEnable()
        {
            var img = GetComponent<Image>();
            if (img != null && savedSprite != null) img.sprite = savedSprite;
        }
    }
}
