using System;
using BingoGame.Common.UI.Components;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Theme;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BingoGame.Common.UI.Services;

/// <summary>
/// Bingo 窗口服务，负责窗口创建和尺寸管理
/// </summary>
internal sealed class BingoWindowService
{
	private readonly float _screenWidth;
	private readonly float _screenHeight;

	public BingoWindowService()
	{
		_screenWidth = Main.screenWidth;
		_screenHeight = Main.screenHeight;
	}

	/// <summary>
	/// 创建一个响应式窗口
	/// </summary>
	public BingoResponsivePanel CreateWindow(
		UIState parent,
		float minimumWidth,
		float minimumHeight,
		float defaultWidth = 0f,
		float defaultHeight = 0f,
		string title = null)
	{
		if (defaultWidth <= 0)
			defaultWidth = minimumWidth;
		if (defaultHeight <= 0)
			defaultHeight = minimumHeight;

		float width = Math.Max(minimumWidth, Math.Min(defaultWidth, _screenWidth - 32f));
		float height = Math.Max(minimumHeight, Math.Min(defaultHeight, _screenHeight - 32f));

		// 计算窗口中心位置
		Vector2 center = new Vector2(_screenWidth / 2f, _screenHeight / 2f);

		// 创建响应式面板
		BingoResponsivePanel panel = new BingoResponsivePanel(
			width, height,
			minimumWidth, minimumHeight,
			width, height, // 参考尺寸
			BingoSpacingTokens.ScreenMargin,
			center,
			pos => { /* 位置改变回调 */ },
			(w, h) => { /* 缩放完成回调 */ }
		);

		BingoTheme.RefreshOpacity();
		panel.BackgroundColor = BingoTheme.WithOpacity(BingoColorTokens.WindowBackground);
		panel.BorderColor = BingoTheme.WithFullOpacity(BingoColorTokens.WindowBorder);

		parent.Append(panel);
		return panel;
	}

	/// <summary>
	/// 获取屏幕尺寸
	/// </summary>
	public (float width, float height) GetScreenSize() => (_screenWidth, _screenHeight);

	/// <summary>
	/// 检查窗口是否超出屏幕边界
	/// </summary>
	public void ClampWindowBounds(BingoResponsivePanel panel)
	{
		float margin = BingoSpacingTokens.ScreenMargin;

		var pos = panel.GetDimensions();
		
		float maxX = _screenWidth - margin;
		float maxY = _screenHeight - margin;
		float minX = margin;
		float minY = margin;

		if (pos.X + pos.Width > maxX)
			panel.Left = new StyleDimension(maxX - pos.Width, 0f);
		if (pos.X < minX)
			panel.Left = new StyleDimension(minX, 0f);
		if (pos.Y + pos.Height > maxY)
			panel.Top = new StyleDimension(maxY - pos.Height, 0f);
		if (pos.Y < minY)
			panel.Top = new StyleDimension(minY, 0f);
	}
}
