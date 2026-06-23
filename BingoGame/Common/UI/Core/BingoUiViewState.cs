using Microsoft.Xna.Framework;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using BingoGame.Common.UI;

namespace BingoGame.Common.UI.Core;

/// <summary>
/// Bingo UI 视图状态管理，持有所有UI页面间的共享状态
/// 原来分散在 BingoMenuState 中的各种字段现在集中在这里
/// </summary>
internal sealed class BingoUiViewState
{
	/// <summary>
	/// 草稿模式：新建或编辑房间时的暂存配置
	/// </summary>
	public int DraftSize = 5;
	public BingoWinRule DraftRule = BingoWinRule.Line;
	public int[] DraftItems = [];

	/// <summary>
	/// 棋盘编辑模式
	/// </summary>
	public bool EditingBoard;

	/// <summary>
	/// 白名单编辑模式
	/// </summary>
	public bool EditingWhitelists;
	public int EditingWhitelistIndex = -1;
	public string WhitelistEditorText = string.Empty;
	public BingoEditorSaveResult WhitelistEditorResult;

	/// <summary>
	/// 初始物品列表编辑模式
	/// </summary>
	public bool EditingInitialItemLists;
	public int EditingInitialItemListIndex = -1;
	public string InitialItemEditorText = string.Empty;
	public BingoEditorSaveResult InitialItemEditorResult;

	/// <summary>
	/// 高级设置编辑模式
	/// </summary>
	public bool EditingAdvancedSettings;

	/// <summary>
	/// 错误状态
	/// </summary>
	public string InventoryActionError = string.Empty;
	public BingoValidationFailure? ValidationFailure;

	/// <summary>
	/// 窗口位置和状态
	/// </summary>
	public Vector2 WindowCenterOffset;
	public bool GameWindowLocked;
	public bool ConfirmingStop;

	/// <summary>
	/// 显示状态（用于判断是否需要刷新UI内容）
	/// </summary>
	public BingoGamePhase ShownPhase;
	public int ShownHost;
	public int ShownRevision;
	public int ShownWidth;
	public int ShownHeight;

	/// <summary>
	/// 重置为打开状态
	/// </summary>
	public void ResetForOpen()
	{
		EditingBoard = false;
		EditingWhitelists = false;
		EditingInitialItemLists = false;
		EditingAdvancedSettings = false;
		InventoryActionError = string.Empty;
		ValidationFailure = null;
		ConfirmingStop = false;
		
		// 清除显示状态以强制刷新（设置一个不太可能的初始值）
		ShownPhase = BingoGamePhase.NotStarted;
		ShownRevision = -1;
	}

	/// <summary>
	/// 检查是否需要刷新UI内容（世界状态改变）
	/// </summary>
	public bool NeedRefreshUI()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.NotStarted)
		{
			return ShownPhase != BingoGamePhase.NotStarted || ShownHost != 0;
		}

		return ShownPhase != BingoWorldSystem.Phase
			|| ShownHost != BingoWorldSystem.HostPlayerId
			|| ShownRevision != BingoWorldSystem.StateRevision
			|| ShownWidth != BingoWorldSystem.BoardSize
			|| ShownHeight != BingoWorldSystem.BoardSize;
	}

	/// <summary>
	/// 同步显示状态，标记为已更新
	/// </summary>
	public void SyncShownState()
	{
		ShownPhase = BingoWorldSystem.Phase;
		ShownHost = BingoWorldSystem.HostPlayerId;
		ShownRevision = BingoWorldSystem.StateRevision;
		ShownWidth = BingoWorldSystem.BoardSize;
		ShownHeight = BingoWorldSystem.BoardSize;
	}
}
