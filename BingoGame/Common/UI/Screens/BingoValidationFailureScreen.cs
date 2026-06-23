using BingoGame.Common.Systems;

using BingoGame.Common.UI.Core;
using BingoGame.Common.UI.Theme;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Components;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 验证失败屏幕 - 显示配置验证失败的原因
/// </summary>
internal sealed class BingoValidationFailureScreen : BingoBaseScreen
{
	private readonly BingoValidationFailure _failure;

	public BingoValidationFailureScreen(BingoValidationFailure failure)
	{
		_failure = failure;
	}

	protected override void BuildUI()
	{
		// 创建主窗口
		BingoResponsivePanel mainWindow = new(400f, 300f, 350f, 280f, 500f, 400f, 16f,
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
