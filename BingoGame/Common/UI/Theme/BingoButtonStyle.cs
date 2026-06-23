using Microsoft.Xna.Framework;

namespace BingoGame.Common.UI.Theme;

/// <summary>
/// 按钮样式对象 - 定义不同状态下的按钮颜色
/// </summary>
internal sealed class BingoButtonStyle
{
	public Color BackgroundColor { get; set; }
	public Color HoveredBackgroundColor { get; set; }
	public Color BorderColor { get; set; }
	public Color TextColor { get; set; } = Color.White;
	public Color DisabledTextColor { get; set; } = Color.Gray;

	/// <summary>
	/// 应用样式到按钮
	/// </summary>
	public Color GetBackgroundColor(bool hovered, bool enabled)
	{
		if (!enabled)
			return Color.DarkGray;
		return hovered ? HoveredBackgroundColor : BackgroundColor;
	}

	/// <summary>
	/// 获取文本颜色
	/// </summary>
	public Color GetTextColor(bool enabled)
	{
		return enabled ? TextColor : DisabledTextColor;
	}
}
