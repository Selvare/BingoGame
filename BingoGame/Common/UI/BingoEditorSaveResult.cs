namespace BingoGame.Common.UI;

/// <summary>
/// 编辑器保存结果状态
/// </summary>
public enum BingoEditorSaveResult
{
	/// <summary>
	/// 待定（未执行）
	/// </summary>
	Pending,

	/// <summary>
	/// 成功保存
	/// </summary>
	Success,

	/// <summary>
	/// 用户取消
	/// </summary>
	Cancelled,

	/// <summary>
	/// 保存出错
	/// </summary>
	Error
}
