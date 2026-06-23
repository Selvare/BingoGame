using System;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using BingoGame.Common.UI.Layout;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace BingoGame.Common.UI.Theme;

/// <summary>
/// 按钮变体，定义按钮的显示风格
/// </summary>
internal enum BingoButtonVariant
{
	/// <summary>
	/// 普通按钮
	/// </summary>
	Normal,

	/// <summary>
	/// 主要按钮（如确认、开始）
	/// </summary>
	Primary,

	/// <summary>
	/// 成功按钮
	/// </summary>
	Success,

	/// <summary>
	/// 危险按钮（如删除、停止）
	/// </summary>
	Danger,

	/// <summary>
	/// 幽灵按钮（透明、小型）
	/// </summary>
	Ghost,

	/// <summary>
	/// 选中状态
	/// </summary>
	Selected,

	/// <summary>
	/// 强调按钮
	/// </summary>
	Emphasized
}

/// <summary>
/// Bingo UI 主题管理器
/// </summary>
internal static class BingoTheme
{
	private static byte _backgroundAlpha = 204;

	/// <summary>
	/// UI背景透明度（0-255）
	/// </summary>
	public static byte BackgroundAlpha => _backgroundAlpha;

	/// <summary>
	/// 刷新UI透明度，根据游戏阶段调整
	/// </summary>
	public static void RefreshOpacity()
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		float value = BingoWorldSystem.Phase is BingoGamePhase.Preparing or BingoGamePhase.InProgress
			? config.InProgressUIOpacity
			: config.InactiveUIOpacity;
		_backgroundAlpha = (byte)MathF.Round(255f * Math.Clamp(value, 0f, 1f));
	}

	/// <summary>
	/// 为颜色应用当前透明度
	/// </summary>
	public static Color WithOpacity(Color color)
	{
		color.A = BackgroundAlpha;
		return color;
	}

	/// <summary>
	/// 为颜色应用完全透明度
	/// </summary>
	public static Color WithFullOpacity(Color color)
	{
		color.A = byte.MaxValue;
		return color;
	}

	/// <summary>
	/// 应用面板样式（向后兼容）
	/// </summary>
	public static void Apply(UIPanel panel, bool selected = false, bool emphasized = false)
	{
		Color background = panel.BackgroundColor;
		Color border = panel.BorderColor;
		if (selected)
		{
			background = Color.Lerp(background, Color.White, 0.2f);
			border = Color.Lerp(border, Color.White, 0.35f);
		}
		else if (emphasized)
		{
			background = Color.Lerp(background, new Color(120, 170, 255), 0.18f);
			border = Color.Lerp(border, Color.White, 0.18f);
		}
		panel.BackgroundColor = WithOpacity(background);
		panel.BorderColor = WithFullOpacity(border);
	}

	/// <summary>
	/// 应用输入框样式
	/// </summary>
	public static void ApplyInput(UIPanel input, bool invalid, bool focused)
	{
		input.BackgroundColor = WithOpacity(BingoColorTokens.InputBackground);
		input.BorderColor = WithFullOpacity(invalid
			? BingoColorTokens.InputInvalidBorder
			: focused ? BingoColorTokens.InputFocusedBorder : BingoColorTokens.InputBorder);
	}

	/// <summary>
	/// 获取按钮变体对应的背景颜色
	/// </summary>
	public static Color GetButtonBackground(BingoButtonVariant variant, bool hovered = false)
	{
		Color baseColor = variant switch
		{
			BingoButtonVariant.Primary => new Color(89, 116, 213),
			BingoButtonVariant.Success => BingoColorTokens.SuccessBackground,
			BingoButtonVariant.Danger => BingoColorTokens.DangerBackground,
			BingoButtonVariant.Ghost => Color.Transparent,
			BingoButtonVariant.Selected => new Color(120, 170, 255),
			BingoButtonVariant.Emphasized => new Color(120, 170, 255),
			_ => BingoColorTokens.ButtonNormal
		};

		if (hovered && variant != BingoButtonVariant.Ghost)
			baseColor = Color.Lerp(baseColor, Color.White, 0.16f);

		return WithOpacity(baseColor);
	}

	/// <summary>
	/// 获取按钮变体对应的边框颜色
	/// </summary>
	public static Color GetButtonBorder(BingoButtonVariant variant)
	{
		return WithFullOpacity(BingoColorTokens.WindowBorder);
	}
}
