using System;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Theme;
using Microsoft.Xna.Framework;
using Terraria;

namespace BingoGame.Common.UI.Components;

/// <summary>
/// Bingo 按钮，支持多种变体的按钮控件
/// </summary>
internal sealed class BingoButton : BingoPanelBase
{
	private readonly Action _action;
	private readonly Color _normalColor;
	private readonly BingoButtonVariant _variant;

	/// <summary>
	/// 创建按钮
	/// </summary>
	/// <param name="action">点击回调</param>
	/// <param name="variant">按钮变体</param>
	/// <param name="enabled">是否启用</param>
	/// <param name="backgroundColor">自定义背景颜色（可选）</param>
	public BingoButton(Action action, BingoButtonVariant variant = BingoButtonVariant.Normal,
		bool enabled = true, Color? backgroundColor = null)
	{
		_action = action;
		_variant = variant;
		Enabled = enabled;
		SetPadding(4f);

		if (backgroundColor.HasValue)
		{
			_normalColor = backgroundColor.Value;
		}
		else
		{
			_normalColor = BingoTheme.GetButtonBackground(_variant);
		}

		BackgroundColor = _normalColor;
		BorderColor = BingoTheme.GetButtonBorder(_variant);

		OnLeftClick += (_, _) =>
		{
			if (Enabled)
				_action();
		};
	}

	/// <summary>
	/// 创建兼容旧版本的按钮（向后兼容）
	/// </summary>
	[Obsolete("使用新的构造函数，传递 BingoButtonVariant")]
	public BingoButton(Action action, bool selected, bool emphasized, Color? backgroundColor = null,
		bool enabled = true)
		: this(action, GetVariantFromFlags(selected, emphasized), enabled, backgroundColor)
	{
	}

	private static BingoButtonVariant GetVariantFromFlags(bool selected, bool emphasized)
	{
		if (selected)
			return BingoButtonVariant.Selected;
		if (emphasized)
			return BingoButtonVariant.Emphasized;
		return BingoButtonVariant.Normal;
	}

	protected override void ApplyStyle()
	{
		Color normalColor = BingoTheme.WithOpacity(_normalColor);
		BackgroundColor = IsMouseHovering && Enabled
			? BingoTheme.WithOpacity(Color.Lerp(normalColor, Color.White, 0.16f))
			: normalColor;
		BorderColor = BingoTheme.WithFullOpacity(BorderColor);
	}
}
