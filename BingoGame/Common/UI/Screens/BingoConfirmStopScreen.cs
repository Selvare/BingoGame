using BingoGame.Common.UI.Core;
using BingoGame.Common.UI.Theme;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Components;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 停止确认屏幕 - 确认是否停止当前游戏
/// </summary>
internal sealed class BingoConfirmStopScreen : BingoBaseScreen
{
	protected override void BuildUI()
	{
		// 创建主窗口
		BingoResponsivePanel mainWindow = new(400f, 280f, 350f, 260f, 500f, 380f, 16f,
			new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f),
			pos => { }, (w, h) => { });
		mainWindow.BackgroundColor = BingoColorTokens.WindowBackground;
		mainWindow.BorderColor = BingoColorTokens.WindowBorder;
		UiState.Append(mainWindow);
		
		// 根栈
		UIVerticalStack root = new(0f);
		root.Width.Set(0f, 1f);
		root.Height.Set(0f, 1f);
		mainWindow.Append(root);
	}
}
