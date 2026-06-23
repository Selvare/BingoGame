using System;
using System.Collections.Generic;
using BingoGame.Common.Systems;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Theme;
using Terraria;

namespace BingoGame.Common.UI.Core;

/// <summary>
/// Bingo UI 上下文，包含整个UI系统运行所需的所有信息和服务
/// 在BingoMenuState中创建和维护
/// </summary>
internal sealed class BingoUiContext
{
	public readonly BingoUiViewState ViewState;
	public readonly BingoUiActions Actions;
	public readonly BingoUiRouter Router;

	/// <summary>
	/// 当前打开的所有窗口集合
	/// </summary>
	private readonly List<BingoResponsivePanel> _openWindows = [];

	public IReadOnlyList<BingoResponsivePanel> OpenWindows => _openWindows.AsReadOnly();

	public BingoUiContext()
	{
		ViewState = new BingoUiViewState();
		Actions = new BingoUiActions(
			phase => { /* 页面刷新会自动处理 */ },
			error => ViewState.InventoryActionError = error,
			failure => ViewState.ValidationFailure = failure
		);
		Router = new BingoUiRouter();
	}

	/// <summary>
	/// 注册打开的窗口
	/// </summary>
	public void RegisterWindow(BingoResponsivePanel window)
	{
		if (!_openWindows.Contains(window))
		{
			_openWindows.Add(window);
		}
	}

	/// <summary>
	/// 注销窗口
	/// </summary>
	public void UnregisterWindow(BingoResponsivePanel window)
	{
		_openWindows.Remove(window);
	}

	/// <summary>
	/// 关闭所有窗口
	/// </summary>
	public void CloseAllWindows()
	{
		_openWindows.Clear();
	}

	/// <summary>
	/// 获取显示房间的棋盘大小
	/// </summary>
	public (int width, int height) GetBoardSize()
	{
		return (BingoWorldSystem.BoardSize, BingoWorldSystem.BoardSize);
	}

	/// <summary>
	/// 获取当前游戏阶段
	/// </summary>
	public BingoGamePhase CurrentPhase => BingoWorldSystem.Phase;

	/// <summary>
	/// 检查是否是主机
	/// </summary>
	public bool IsHost => BingoWorldSystem.IsLocalPlayerHost;

	/// <summary>
	/// 获取经过的时间
	/// </summary>
	public TimeSpan ElapsedTime => TimeSpan.FromTicks(BingoWorldSystem.ElapsedTicks * 10000);

	/// <summary>
	/// 更新定时器文本显示
	/// </summary>
	public void UpdateTimerText(BingoAdaptiveText timerText)
	{
		if (timerText == null)
			return;

		// 根据当前阶段显示不同的时间信息
		string timerDisplay = BingoWorldSystem.Phase switch
		{
			BingoGamePhase.NotStarted => "等待中",
			BingoGamePhase.InProgress => FormatTimeSpan(ElapsedTime),
			BingoGamePhase.Finished => FormatTimeSpan(ElapsedTime),
			BingoGamePhase.Preparing => $"准备中 {FormatTimeSpan(TimeSpan.FromTicks(BingoWorldSystem.PreparationRemainingTicks * 10000))}",
			_ => string.Empty
		};

		timerText.SetText(timerDisplay);
	}

	private static string FormatTimeSpan(TimeSpan time)
	{
		if (time.TotalHours >= 1)
			return time.ToString(@"hh\:mm\:ss");
		return time.ToString(@"mm\:ss");
	}

	/// <summary>
	/// 重置UI以备打开
	/// </summary>
	public void PrepareForOpen()
	{
		ViewState.ResetForOpen();
		CloseAllWindows();
		ViewState.SyncShownState();
	}

	/// <summary>
	/// 检查并刷新UI
	/// </summary>
	public bool CheckAndRefreshState()
	{
		if (ViewState.NeedRefreshUI())
		{
			ViewState.SyncShownState();
			return true;
		}
		return false;
	}
}
