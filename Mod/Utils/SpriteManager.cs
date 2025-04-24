using Mod.Cheats.Patches;
using Mod.Utils;
using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal static class SpriteManager
    {
        // Placeholder for sprites to be assigned elsewhere
        public static Sprite npcSprite { get; set; }

        public static Sprite ToSprite(this string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        public static Sprite GetSprite()
        {
            if (npcSprite == null)
            {
                // Assuming BaseSprites.npcmapicon is a valid Base64 string
                string base64String = SpriteBases.npcMapIcon.ToString();
                npcSprite = base64String.ToSprite();
            }
            return npcSprite;
        }
    }
}
