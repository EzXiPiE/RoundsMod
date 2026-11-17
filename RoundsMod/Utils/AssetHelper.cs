using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RoundsMod.Utils
{
    public static class AssetHelper
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite fallback;

        public static Sprite GetCardSprite(string fileName)
        {
            if (Cache.TryGetValue(fileName, out var s) && s != null) return s;
            var spr = LoadFromResource(fileName);
            if (spr != null) { Cache[fileName] = spr; return spr; }

            if (fallback == null) fallback = LoadFromResource("kojner_power.png");
            return fallback;
        }

        private static Sprite LoadFromResource(string suffix)
        {
            var asm = Assembly.GetExecutingAssembly();
            string res = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix));
            if (string.IsNullOrEmpty(res)) return null;

            using (Stream st = asm.GetManifestResourceStream(res))
            {
                if (st == null) return null;
                byte[] buf = new byte[st.Length];
                st.Read(buf, 0, buf.Length);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                tex.LoadImage(buf);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
