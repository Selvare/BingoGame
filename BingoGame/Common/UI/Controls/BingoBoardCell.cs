using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BingoGame.Common.Systems;

internal sealed class BingoBoardCell : UIPanel
{
	private readonly int _itemType;
	private readonly byte _owner;
	private readonly Func<Color> _borderColor;

	public BingoBoardCell(int itemType, byte owner, Func<Color> borderColor)
	{
		_itemType = itemType;
		_owner = owner;
		_borderColor = borderColor;
		BackgroundColor = Color.Transparent;
		SetPadding(0f);
		OverflowHidden = true;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		BackgroundColor = _owner is >= 1 and <= 5
			? BingoUITheme.WithOpacity(BingoBoardElement.GetTeamColor(_owner))
			: Color.Transparent;
		Color borderColor = _borderColor?.Invoke() ?? Color.White;
		BorderColor = BingoUITheme.WithFullOpacity(borderColor);
		base.DrawSelf(spriteBatch);

		Rectangle bounds = GetDimensions().ToRectangle();
		if (BingoWorldSystem.IsUsableItemId(_itemType))
			BingoItemIconRenderer.Draw(spriteBatch, bounds, _itemType, 7f);

		if (bounds.Contains(Main.MouseScreen.ToPoint()) && BingoWorldSystem.IsUsableItemId(_itemType))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = $"{Lang.GetItemNameValue(_itemType)} ({_itemType})";
		}
	}
}

