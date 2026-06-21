using System;
using BingoGame.Common.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace BingoGame.Common.Systems;

internal sealed class BingoBoardCell : UIPanel
{
	private readonly int _itemType;
	private readonly byte _owner;
	private readonly Func<Color> _borderColor;

	private Item _hoverItem = new();

	public BingoBoardCell(int itemType, byte owner, Func<Color> borderColor)
	{
		_itemType = itemType;
		_owner = owner;
		_borderColor = borderColor;

		if (BingoWorldSystem.IsUsableItemId(_itemType))
		{
			_hoverItem.SetDefaults(_itemType);
			_hoverItem.stack = 1;
		}

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
		if (!BingoWorldSystem.IsUsableItemId(_itemType))
			return;
		BingoItemIconRenderer.Draw(spriteBatch, bounds, _itemType, 7f);

		if (bounds.Contains(Main.MouseScreen.ToPoint()))
		{
			string title = $"{Lang.GetItemNameValue(_itemType)} ({_itemType})";
			string body = _owner == 0
				? Language.GetTextValue("Mods.BingoGame.UI.ItemUnclaimed")
				: Language.GetTextValue("Mods.BingoGame.UI.ItemClaimed", BingoTeamDisplay.GetName(_owner));
			BingoHoverTooltipGlobalItem.Show(_hoverItem, title, body);
		}
	}
}
