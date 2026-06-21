using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BingoGame.Common.Systems;

internal sealed class BingoButton : UIPanel
{
	private readonly Action _action;
	private readonly Color _normalColor;

	public BingoButton(Action action, bool selected, bool emphasized, Color? backgroundColor = null)
	{
		_action = action;
		OverflowHidden = true;
		SetPadding(4f);
		BingoUITheme.Apply(this, selected, emphasized);
		_normalColor = backgroundColor ?? BackgroundColor;
		BackgroundColor = BingoUITheme.WithOpacity(_normalColor);
		OnLeftClick += (_, _) => _action();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Color normalColor = BingoUITheme.WithOpacity(_normalColor);
		BackgroundColor = IsMouseHovering
			? BingoUITheme.WithOpacity(Color.Lerp(normalColor, Color.White, 0.16f))
			: normalColor;
		BorderColor = BingoUITheme.WithFullOpacity(BorderColor);
		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}

