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
	private readonly BingoClaimRecord? _claim;
	private readonly Func<Color> _borderColor;
	private readonly bool _singlePlayer;

	private Item _hoverItem = new();

	public BingoBoardCell(int itemType, byte owner, BingoClaimRecord? claim, Func<Color> borderColor,
		bool singlePlayer)
	{
		_itemType = itemType;
		_owner = owner;
		_claim = claim;
		_borderColor = borderColor;
		_singlePlayer = singlePlayer;

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
			string body = BuildTooltipBody();
			Color? bodyColor = _owner is >= 1 and <= 5
				? BingoBoardElement.GetTeamColor(_owner)
				: null;
			BingoHoverTooltipGlobalItem.Show(_hoverItem, title, body, bodyColor);
		}
	}

	private string BuildTooltipBody()
	{
		if (_owner == 0)
			return Language.GetTextValue(_singlePlayer
				? "Mods.BingoGame.UI.SingleItemUnclaimed"
				: "Mods.BingoGame.UI.ItemUnclaimed");

		string elapsed = _claim.HasValue
			? BingoWorldSystem.FormatElapsed(_claim.Value.ElapsedTicks)
			: "--:--";
		if (_singlePlayer)
			return Language.GetTextValue("Mods.BingoGame.UI.SingleItemClaimed", elapsed);

		string playerName = _claim.HasValue
			? _claim.Value.PlayerName
			: Language.GetTextValue("Mods.BingoGame.UI.UnknownPlayer");
		return Language.GetTextValue("Mods.BingoGame.UI.ItemClaimed", elapsed,
			BingoTeamDisplay.GetName(_owner), playerName);
	}
}
