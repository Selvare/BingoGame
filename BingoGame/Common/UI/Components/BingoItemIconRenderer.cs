using System;
using BingoGame.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace BingoGame.Common.UI.Components;

/// <summary>
/// 物品图标渲染器，负责绘制物品图标
/// </summary>
internal static class BingoItemIconRenderer
{
	/// <summary>
	/// 在指定矩形范围内绘制物品图标
	/// </summary>
	/// <param name="spriteBatch">SpriteBatch实例</param>
	/// <param name="bounds">绘制矩形</param>
	/// <param name="itemType">物品类型ID</param>
	/// <param name="padding">内边距</param>
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
