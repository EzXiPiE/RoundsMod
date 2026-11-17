using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RoundsMod.Utils
{
    public static class AssetHelper
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetCardArt(string fileName)
        {
            if (Cache.TryGetValue(fileName, out var s) && s != null) return s;
            var sprite = LoadSprite($"RoundsMod.{fileName}") ?? LoadSprite("RoundsMod.card_art.png");
            if (sprite != null) Cache[fileName] = sprite;
            return sprite;
        }

        private static Sprite LoadSprite(string path)
        {
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            {
                if (st == null) return null;
                byte[] buf = new byte[st.Length];
                st.Read(buf, 0, buf.Length);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                tex.LoadImage(buf);
                Object.DontDestroyOnLoad(tex);
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                spr.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(spr);
                return spr;
            }
        }
    }
}
