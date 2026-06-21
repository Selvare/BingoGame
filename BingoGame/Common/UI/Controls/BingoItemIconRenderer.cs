using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace BingoGame.Common.Systems;

internal static class BingoItemIconRenderer
{
	public static void Draw(SpriteBatch spriteBatch, Rectangle bounds, int itemType, float padding)
	{
		if (!BingoWorldSystem.IsUsableItemId(itemType))
			return;
		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = texture.Frame();
		float availableWidth = Math.Max(1f, bounds.Width - padding * 2f);
		float availableHeight = Math.Max(1f, bounds.Height - padding * 2f);
		float scale = Math.Min(1f, Math.Min(availableWidth / frame.Width, availableHeight / frame.Height));
		spriteBatch.Draw(texture, bounds.Center.ToVector2(), frame, Color.White, 0f, frame.Size() / 2f,
			scale, SpriteEffects.None, 0f);
	}
}

