using System;
using System.Collections.Generic;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using Terraria;

namespace BingoGame.Common.UI.Core;

/// <summary>
/// Bingo UI 操作集合，定义UI中可触发的所有用户交互行为
/// </summary>
internal sealed class BingoUiActions
{
	private readonly Action<BingoGamePhase> _onPhaseChanged;
	private readonly Action<string> _onShowError;
	private readonly Action<BingoValidationFailure> _onValidationFailed;

	// 编辑器状态
	private int _draftSize = 5;
	private BingoWinRule _draftRule = BingoWinRule.Line;
	private List<int> _draftItems = new();
	private List<int> _draftWhitelist = new();
	private List<int> _draftInitialItems = new();

	// 高级选项状态
	private bool _lineTiebreakEnabled = true;
	private bool _lineAutoDegradeEnabled = true;
	private bool _timeLimitEnabled = false;
	private int _timeLimitMinutes = 30;
	private int _timeLimitSeconds = 0;
	private bool _killStealEnabled = false;
	private float _killStealChance = 0.5f;
	private bool _randomStartEnabled = false;
	private bool _randomStartTeamTogether = false;
	private bool _forcePvpEnabled = false;
	private bool _noRetreatEnabled = false;
	private bool _fogOfWarEnabled = false;
	private bool _preparationEnabled = false;
	private int _preparationSeconds = 10;

	public BingoUiActions(
		Action<BingoGamePhase> onPhaseChanged,
		Action<string> onShowError,
		Action<BingoValidationFailure> onValidationFailed)
	{
		_onPhaseChanged = onPhaseChanged;
		_onShowError = onShowError;
		_onValidationFailed = onValidationFailed;
	}

	/// <summary>
	/// 调整草稿大小
	/// </summary>
	public void ResizeDraft(int newSize)
	{
		if (newSize < 2 || newSize > 10)
		{
			_onShowError?.Invoke("棋盘大小必须在 2-10 之间");
			return;
		}

		try
		{
			_draftSize = newSize;
			int requiredItems = newSize * newSize;
			if (_draftItems.Count > requiredItems)
			{
				_draftItems.RemoveRange(requiredItems, _draftItems.Count - requiredItems);
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 设置赢法则
	/// </summary>
	public void SetRule(BingoWinRule rule)
	{
		try
		{
			_draftRule = rule;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换行进度并列 Tiebreak
	/// </summary>
	public void ToggleLineProgressTiebreak()
	{
		try
		{
			_lineTiebreakEnabled = !_lineTiebreakEnabled;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换行自动降级
	/// </summary>
	public void ToggleLineAutoDegrade()
	{
		try
		{
			_lineAutoDegradeEnabled = !_lineAutoDegradeEnabled;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换时间限制
	/// </summary>
	public void ToggleTimeLimit()
	{
		try
		{
			_timeLimitEnabled = !_timeLimitEnabled;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 设置时间限制
	/// </summary>
	public void SetTimeLimit(float seconds)
	{
		try
		{
			int totalSeconds = (int)seconds;
			_timeLimitMinutes = totalSeconds / 60;
			_timeLimitSeconds = totalSeconds % 60;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换白名单
	/// </summary>
	public void ToggleWhitelist()
	{
		// 需要检查是否有白名单项
		// 这里仅作为状态管理，具体逻辑由 UI 处理
	}

	/// <summary>
	/// 切换杀死盗取
	/// </summary>
	public void ToggleKillSteal()
	{
		try
		{
			_killStealEnabled = !_killStealEnabled;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 设置杀死盗取概率
	/// </summary>
	public void SetKillStealChance(float chance)
	{
		try
		{
			if (chance < 0f || chance > 1f)
			{
				_onShowError?.Invoke("杀死盗取概率必须在 0-1 之间");
				return;
			}
			_killStealChance = chance;
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 打开编辑器
	/// </summary>
	public void OpenEditor()
	{
		try
		{
			// 准备编辑器状态 - 从当前棋盘或默认值初始化
			_draftSize = BingoWorldSystem.HasBoard ? BingoWorldSystem.BoardSize : 5;
			_draftRule = BingoWorldSystem.HasBoard ? BingoWorldSystem.WinRule : BingoWinRule.Line;
			_draftItems = new List<int>(BingoWorldSystem.HasBoard ? BingoWorldSystem.ItemTypes : Array.Empty<int>());
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 关闭编辑器
	/// </summary>
	public void CloseEditor()
	{
		try
		{
			_draftItems.Clear();
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 清空草稿物品
	/// </summary>
	public void ClearDraftItems()
	{
		try
		{
			_draftItems.Clear();
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 打开白名单列表
	/// </summary>
	public void OpenWhitelistList()
	{
		try
		{
			// 准备白名单编辑界面
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 关闭白名单列表
	/// </summary>
	public void CloseWhitelistList()
	{
		try
		{
			_draftWhitelist.Clear();
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 添加白名单
	/// </summary>
	public void AddWhitelist()
	{
		try
		{
			_draftWhitelist.Add(0); // 添加新的白名单项（默认物品ID为0）
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 提交白名单名称
	/// </summary>
	public void CommitWhitelistName(string name)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				_onShowError?.Invoke("白名单名称不能为空");
				return;
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换白名单条目
	/// </summary>
	public void ToggleWhitelistEntry(int whitelistIndex, int itemId)
	{
		try
		{
			if (whitelistIndex >= 0 && whitelistIndex < _draftWhitelist.Count)
			{
				if (_draftWhitelist[whitelistIndex] == itemId)
					_draftWhitelist[whitelistIndex] = 0; // 移除
				else
					_draftWhitelist[whitelistIndex] = itemId; // 添加
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 删除白名单
	/// </summary>
	public void DeleteWhitelist(int whitelistIndex)
	{
		try
		{
			if (whitelistIndex >= 0 && whitelistIndex < _draftWhitelist.Count)
			{
				_draftWhitelist.RemoveAt(whitelistIndex);
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 打开白名单编辑器
	/// </summary>
	public void OpenWhitelistEditor(int whitelistIndex)
	{
		try
		{
			if (whitelistIndex >= 0 && whitelistIndex < _draftWhitelist.Count)
			{
				// 准备白名单编辑界面
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 保存白名单编辑
	/// </summary>
	public void SaveWhitelistEditor(int whitelistIndex)
	{
		try
		{
			// 保存白名单编辑
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 尝试提交白名单编辑
	/// </summary>
	public void TryCommitWhitelistEditor()
	{
		try
		{
			// 验证并提交白名单编辑
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 打开初始物品列表
	/// </summary>
	public void OpenInitialItemList()
	{
		try
		{
			// 准备初始物品编辑界面
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 关闭初始物品列表
	/// </summary>
	public void CloseInitialItemList()
	{
		try
		{
			_draftInitialItems.Clear();
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 添加初始物品列表
	/// </summary>
	public void AddInitialItemList()
	{
		try
		{
			_draftInitialItems.Add(0); // 添加默认物品ID为0
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 删除初始物品列表
	/// </summary>
	public void DeleteInitialItemList(int listIndex)
	{
		try
		{
			if (listIndex >= 0 && listIndex < _draftInitialItems.Count)
			{
				_draftInitialItems.RemoveAt(listIndex);
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 打开初始物品编辑器
	/// </summary>
	public void OpenInitialItemEditor(int listIndex)
	{
		try
		{
			if (listIndex >= 0 && listIndex < _draftInitialItems.Count)
			{
				// 准备初始物品编辑界面
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 保存初始物品编辑
	/// </summary>
	public void SaveInitialItemEditor(int listIndex)
	{
		try
		{
			// 保存初始物品编辑
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 尝试提交初始物品编辑
	/// </summary>
	public void TryCommitInitialItemEditor()
	{
		try
		{
			// 验证并提交初始物品编辑
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 创建新房间 - 发送启动游戏请求到网络或本地
	/// </summary>
	public void CreateNewSession(int size, BingoWinRule rule, int[] itemIds)
	{
		try
		{
			if (size < 2 || size > 10)
			{
				_onValidationFailed?.Invoke(new BingoValidationFailure(BingoValidationError.InvalidSize, -1));
				return;
			}

			if (itemIds == null || itemIds.Length != size * size)
			{
				_onValidationFailed?.Invoke(new BingoValidationFailure(BingoValidationError.InvalidCellCount, -1));
				return;
			}

			// 通过网络或本地启动游戏
			BingoGame.RequestStart(
				size, rule, itemIds,
				_draftWhitelist.Count > 0, // whitelistEnabled
				_draftWhitelist.ToArray(), // whitelistTypes
				_draftInitialItems.ToArray(), // initialItemTypes
				_timeLimitEnabled, _timeLimitMinutes, _timeLimitSeconds,
				_lineTiebreakEnabled, _lineAutoDegradeEnabled,
				_killStealEnabled, _killStealChance,
				_randomStartEnabled, _randomStartTeamTogether,
				_forcePvpEnabled, _noRetreatEnabled, _fogOfWarEnabled,
				_preparationEnabled, _preparationSeconds
			);

			_onPhaseChanged?.Invoke(BingoGamePhase.Preparing);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 编辑棋盘
	/// </summary>
	public void EditBoard(Action onComplete)
	{
		try
		{
			if (!BingoWorldSystem.HasBoard)
			{
				_onShowError?.Invoke("当前没有有效的棋盘");
				return;
			}

			OpenEditor();
			onComplete?.Invoke();
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 保存棋盘编辑
	/// </summary>
	public void SaveBoardEdit()
	{
		try
		{
			if (_draftItems.Count != _draftSize * _draftSize)
			{
				_onShowError?.Invoke($"棋盘物品数量不正确，需要 {_draftSize * _draftSize} 个");
				return;
			}

			// 在此处可以添加棋盘编辑保存逻辑
			// 例如更新 BingoWorldSystem 状态
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 开始游戏 - 从准备阶段转到进行中
	/// </summary>
	public void StartGame()
	{
		try
		{
			if (!BingoWorldSystem.HasBoard)
			{
				_onShowError?.Invoke("当前没有有效的棋盘");
				return;
			}

			_onPhaseChanged?.Invoke(BingoGamePhase.InProgress);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 停止游戏 - 请求结束当前游戏
	/// </summary>
	public void StopGame()
	{
		try
		{
			BingoGame.RequestEnd(BingoEndAction.Cancel);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 显示停止确认对话
	/// </summary>
	public void ShowStopConfirmation()
	{
		try
		{
			// 这个操作通常由 UI 路由器处理
			// 这里仅作为命令分发点
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 取消停止确认
	/// </summary>
	public void CancelStopConfirmation()
	{
		try
		{
			// 返回游戏进行中状态
			_onPhaseChanged?.Invoke(BingoGamePhase.InProgress);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 声明格子 - 玩家声明完成了一个目标
	/// </summary>
	public void ClaimCell(int cellIndex, byte playerTeam)
	{
		try
		{
			if (cellIndex < 0 || cellIndex >= BingoWorldSystem.ItemTypes.Length)
			{
				_onShowError?.Invoke("无效的格子索引");
				return;
			}

			BingoWorldSystem.TryClaimInventory(Main.LocalPlayer);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 请求结束游戏
	/// </summary>
	public void RequestEnd()
	{
		try
		{
			BingoGame.RequestEnd(BingoEndAction.Settle);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 切换游戏窗口锁定
	/// </summary>
	public void ToggleGameWindowLock()
	{
		try
		{
			// 获取当前状态并切换
			bool currentLocked = BingoWorldSystem.HasBoard; // 这里需要访问实际的锁定状态
			// 更新 UI 窗口的锁定状态
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 更新会话状态
	/// </summary>
	public void UpdateSession()
	{
		try
		{
			// 同步最新的游戏状态从 BingoWorldSystem
			_onPhaseChanged?.Invoke(BingoWorldSystem.Phase);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 获取当前草稿大小
	/// </summary>
	public int GetDraftSize() => _draftSize;

	/// <summary>
	/// 获取当前赢法则
	/// </summary>
	public BingoWinRule GetDraftRule() => _draftRule;

	/// <summary>
	/// 获取当前草稿物品列表
	/// </summary>
	public IReadOnlyList<int> GetDraftItems() => _draftItems.AsReadOnly();

	/// <summary>
	/// 添加物品到草稿
	/// </summary>
	public void AddDraftItem(int itemId)
	{
		try
		{
			int maxItems = _draftSize * _draftSize;
			if (_draftItems.Count >= maxItems)
			{
				_onShowError?.Invoke($"草稿已满 ({maxItems} 个物品)");
				return;
			}

			_draftItems.Add(itemId);
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 移除草稿中的物品
	/// </summary>
	public void RemoveDraftItem(int index)
	{
		try
		{
			if (index >= 0 && index < _draftItems.Count)
			{
				_draftItems.RemoveAt(index);
			}
		}
		catch (Exception ex)
		{
			_onShowError?.Invoke(ex.Message);
		}
	}

	/// <summary>
	/// 获取时间限制状态
	/// </summary>
	public bool IsTimeLimitEnabled() => _timeLimitEnabled;

	/// <summary>
	/// 获取时间限制分钟数
	/// </summary>
	public int GetTimeLimitMinutes() => _timeLimitMinutes;

	/// <summary>
	/// 获取时间限制秒数
	/// </summary>
	public int GetTimeLimitSeconds() => _timeLimitSeconds;

	/// <summary>
	/// 获取杀死盗取是否启用
	/// </summary>
	public bool IsKillStealEnabled() => _killStealEnabled;

	/// <summary>
	/// 获取杀死盗取概率
	/// </summary>
	public float GetKillStealChance() => _killStealChance;

	/// <summary>
	/// 获取行进度并列是否启用
	/// </summary>
	public bool IsLineTiebreakEnabled() => _lineTiebreakEnabled;

	/// <summary>
	/// 获取行自动降级是否启用
	/// </summary>
	public bool IsLineAutoDegradeEnabled() => _lineAutoDegradeEnabled;
}
