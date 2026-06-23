using Microsoft.Xna.Framework;

namespace BingoGame.Common.UI.Theme;

/// <summary>
/// Bingo UI 颜色令牌，集中管理所有UI颜色
/// </summary>
internal static class BingoColorTokens
{
	// 窗口颜色
	public static readonly Color WindowBackground = new(33, 43, 79);
	public static readonly Color WindowBorder = new(89, 116, 213);

	// 输入框颜色
	public static readonly Color InputBackground = new(63, 82, 151);
	public static readonly Color InputBorder = new(89, 116, 213);
	public static readonly Color InputFocusedBorder = new(130, 210, 255);
	public static readonly Color InputInvalidBorder = Color.OrangeRed;

	// 按钮颜色
	public static readonly Color ButtonNormal = new(63, 82, 151);
	public static readonly Color ButtonHovered = new(89, 116, 213);
	public static readonly Color ButtonSelected = new(120, 170, 255);
	public static readonly Color ButtonEmphasis = new(120, 170, 255);

	// 功能颜色
	public static readonly Color SuccessBackground = new(45, 150, 70);
	public static readonly Color DangerBackground = new(170, 45, 45);

	// 文本颜色
	public static readonly Color TextPrimary = Color.White;
	public static readonly Color TextMuted = Color.Gray;
	public static readonly Color TextWarning = Color.OrangeRed;

	// 面板颜色
	public static readonly Color PanelBackground = new(33, 43, 79);
	public static readonly Color PanelBorder = new(89, 116, 213);
	public static readonly Color SectionBackground = new(40, 50, 90);
	public static readonly Color SectionBorder = new(70, 90, 160);
}
