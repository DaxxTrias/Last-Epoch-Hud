using Mod.Cheats.Patches;
using Mod.Utils;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal static class SpriteManager
	{
		// Placeholder for sprites to be assigned elsewhere
		public static Sprite? npcSprite { get; set; }
		private static Texture2D? npcTexture;

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
				string base64String = SpriteBases.npcMapIcon;
				// Create sprite and capture the underlying texture reference for cleanup
				byte[] bytes = Convert.FromBase64String(base64String);
				npcTexture = new Texture2D(1, 1);
				npcTexture.LoadImage(bytes);
				npcTexture.Apply();
				npcSprite = Sprite.Create(npcTexture, new Rect(0, 0, npcTexture.width, npcTexture.height), new Vector2(0.5f, 0.5f));
			}
			return npcSprite;
		}

		public static void Cleanup()
		{
			if (npcSprite != null)
			{
				UnityEngine.Object.Destroy(npcSprite);
				npcSprite = null;
			}
			if (npcTexture != null)
			{
				UnityEngine.Object.Destroy(npcTexture);
				npcTexture = null;
			}
		}
	}
}
