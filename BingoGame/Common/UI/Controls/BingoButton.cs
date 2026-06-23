using System;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BingoGame.Common.Systems;

internal sealed class BingoButton : UIPanel
{
	private readonly Action _action;
	private readonly Color _normalColor;
	private readonly bool _enabled;

	public BingoButton(Action action, bool selected, bool emphasized, Color? backgroundColor = null,
		bool enabled = true)
	{
		_action = action;
		_enabled = enabled;
		OverflowHidden = true;
		SetPadding(4f);
		BingoUITheme.Apply(this, selected, emphasized);
		_normalColor = backgroundColor ?? BackgroundColor;
		BackgroundColor = BingoUITheme.WithOpacity(_normalColor);
		OnLeftClick += (_, _) =>
		{
			if (_enabled)
				_action();
		};
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Color normalColor = BingoUITheme.WithOpacity(_normalColor);
		BackgroundColor = IsMouseHovering && _enabled
			? BingoUITheme.WithOpacity(Color.Lerp(normalColor, Color.White, 0.16f))
			: normalColor;
		BorderColor = BingoUITheme.WithFullOpacity(BorderColor);
		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}
