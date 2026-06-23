using System;
using BingoGame.Common.Systems;
using BingoGame.Common.UI.Screens;

namespace BingoGame.Common.UI.Core;

/// <summary>
/// Bingo UI 路由器，根据当前UI状态返回相应的屏幕（页面）
/// 决定当前应该显示哪个界面
/// </summary>
internal sealed class BingoUiRouter
{
	/// <summary>
	/// 根据当前状态解析应该显示的屏幕
	/// </summary>
	public BingoScreen Resolve(BingoUiContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		var viewState = context.ViewState;

		// 优先级1：错误状态需要显示验证失败界面
		if (viewState.ValidationFailure.HasValue)
		{
			return new BingoValidationFailureScreen(viewState.ValidationFailure.Value);
		}

		// 优先级2：库存操作错误
		if (!string.IsNullOrEmpty(viewState.InventoryActionError))
		{
			return new BingoErrorScreen(viewState.InventoryActionError);
		}

		// 优先级3：确认停止游戏
		if (viewState.ConfirmingStop)
		{
			return new BingoConfirmStopScreen();
		}

		// 优先级4：各种编辑模式
		if (viewState.EditingBoard)
		{
			return new BingoBoardEditorScreen();
		}

		if (viewState.EditingWhitelists)
		{
			return new BingoWhitelistEditorScreen(viewState.EditingWhitelistIndex);
		}

		if (viewState.EditingInitialItemLists)
		{
			return new BingoInitialItemEditorScreen(viewState.EditingInitialItemListIndex);
		}

		if (viewState.EditingAdvancedSettings)
		{
			return new BingoAdvancedSettingsScreen();
		}

		// 优先级5：游戏阶段显示
		return BingoWorldSystem.Phase switch
		{
			BingoGamePhase.NotStarted => new BingoSettingsScreen(),
			BingoGamePhase.InProgress => new BingoGameBoardScreen(),
			BingoGamePhase.Finished => new BingoResultScreen(),
			BingoGamePhase.Preparing => new BingoSettingsScreen(),
			_ => new BingoSettingsScreen()
		};
	}

	/// <summary>
	/// 基础屏幕接口，所有UI屏幕实现此接口
	/// </summary>
	public interface IScreen
	{
		void Build(Terraria.UI.UIState uiState, BingoUiContext context);
	}
}

/// <summary>
/// Bingo 屏幕基类
/// </summary>
internal abstract class BingoScreen
{
	public abstract void Build(Terraria.UI.UIState uiState, BingoUiContext context);
}
