using BingoGame.Common.UI.Core;
using BingoGame.Common.UI.Theme;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Components;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 游戏棋盘屏幕 - 显示当前游戏的棋盘
/// </summary>
internal sealed class BingoGameBoardScreen : BingoBaseScreen
{
	protected override void BuildUI()
	{
		// 创建主窗口
		BingoResponsivePanel mainWindow = new(480f, 520f, 400f, 480f, 560f, 600f, 16f, 
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
