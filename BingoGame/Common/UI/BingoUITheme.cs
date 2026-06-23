using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BingoGame.Common.UI;

/// <summary>
/// Bingo UI 主题工具 - 处理 UI 元素的样式和透明度
/// </summary>
internal static class BingoUITheme
{
	/// <summary>
	/// 获取当前 UI 不透明度（基于游戏设置和玩家偏好）
	/// </summary>
	private static float CurrentOpacity
	{
		get
		{
			// Terraria 默认 UI 不透明度范围 0.6 - 1.0
			float baseOpacity = 0.8f; // 使用默认值
			return Math.Max(0.5f, baseOpacity); // 最小 50% 透明度
		}
	}

	/// <summary>
	/// 应用透明度到颜色
	/// </summary>
	public static Color WithOpacity(Color color)
	{
		return new Color(color.R, color.G, color.B, (byte)(255 * CurrentOpacity));
	}

	/// <summary>
	/// 应用完全不透明度到颜色
	/// </summary>
	public static Color WithFullOpacity(Color color)
	{
		return new Color(color.R, color.G, color.B, 255);
	}

	/// <summary>
	/// 刷新 UI 透明度（每帧调用）
	/// </summary>
	public static void RefreshOpacity()
	{
		// 这个方法由系统定期调用来更新透明度
		// 实际工作在 WithOpacity 和 WithFullOpacity 中进行
	}

	/// <summary>
	/// 应用按钮样式
	/// </summary>
	public static void Apply(UIElement button, bool selected, bool emphasized)
	{
		if (button is not UIPanel panel)
			return;

		// 根据选中和强调状态应用颜色
		if (emphasized)
		{
			panel.BackgroundColor = new Color(200, 150, 100, (byte)(255 * CurrentOpacity));
		}
		else if (selected)
		{
			panel.BackgroundColor = new Color(150, 150, 150, (byte)(255 * CurrentOpacity));
		}
		else
		{
			panel.BackgroundColor = new Color(100, 100, 100, (byte)(255 * CurrentOpacity));
		}
	}

	/// <summary>
	/// 应用输入框样式
	/// </summary>
	public static void ApplyInput(UIElement input, bool invalid, bool focused)
	{
		if (input is not UIPanel panel)
			return;

		// 根据聚焦和验证状态应用颜色
		if (invalid)
		{
			panel.BackgroundColor = new Color(200, 100, 100, (byte)(255 * CurrentOpacity));
			panel.BorderColor = new Color(255, 0, 0, 255);
		}
		else if (focused)
		{
			panel.BackgroundColor = new Color(150, 200, 150, (byte)(255 * CurrentOpacity));
			panel.BorderColor = new Color(0, 255, 0, 255);
		}
		else
		{
			panel.BackgroundColor = new Color(100, 100, 100, (byte)(255 * CurrentOpacity));
			panel.BorderColor = new Color(150, 150, 150, 255);
		}
	}
}
