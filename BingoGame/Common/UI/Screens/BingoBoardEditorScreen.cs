using BingoGame.Common.UI.Core;
using BingoGame.Common.UI.Theme;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Components;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 棋盘编辑屏幕 - 编辑棋盘物品
/// </summary>
internal sealed class BingoBoardEditorScreen : BingoBaseScreen
{
	protected override void BuildUI()
	{
		// 创建主窗口
		BingoResponsivePanel mainWindow = new(400f, 480f, 350f, 450f, 500f, 550f, 16f,
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
