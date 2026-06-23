namespace BingoGame.Common.UI;

/// <summary>
/// Bingo 窗口页面类型
/// </summary>
public enum BingoWindowPage
{
	/// <summary>
	/// 设置窗口
	/// </summary>
	Settings,

	/// <summary>
	/// 棋盘窗口
	/// </summary>
	GameBoard,

	/// <summary>
	/// 棋盘编辑器窗口
	/// </summary>
	BoardEditor,

	/// <summary>
	/// 白名单编辑器窗口
	/// </summary>
	WhitelistEditor,

	/// <summary>
	/// 初始物品编辑器窗口
	/// </summary>
	InitialItemEditor,

	/// <summary>
	/// 高级设置窗口
	/// </summary>
	AdvancedSettings,

	/// <summary>
	/// 验证失败窗口
	/// </summary>
	ValidationFailure,

	/// <summary>
	/// 错误窗口
	/// </summary>
	Error,

	/// <summary>
	/// 停止确认窗口
	/// </summary>
	ConfirmStop,

	/// <summary>
	/// 结果窗口
	/// </summary>
	Result
}
