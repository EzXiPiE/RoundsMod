using UnityEngine;

namespace RoundsMod.Utils
{
    public static class VisualHelper
    {
        private static Sprite circle;

        public static Sprite GetCircle()
        {
            if (circle != null) return circle;
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) <= 31f ? Color.white : Color.clear);
            tex.Apply();
            Object.DontDestroyOnLoad(tex);
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            circle.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(circle);
            return circle;
        }
    }
}
