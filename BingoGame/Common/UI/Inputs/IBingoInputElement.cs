namespace BingoGame.Common.UI.Inputs;

/// <summary>
/// Bingo 输入元素接口 - 定义输入框的通用接口
/// </summary>
internal interface IBingoInputElement
{
	/// <summary>
	/// 获取或设置是否被聚焦
	/// </summary>
	bool IsFocused { get; }

	/// <summary>
	/// 获取或设置是否无效
	/// </summary>
	bool IsInvalid { get; set; }

	/// <summary>
	/// 聚焦此输入框
	/// </summary>
	void Focus();

	/// <summary>
	/// 失焦此输入框，可选择是否提交内容
	/// </summary>
	void Blur(bool commit = true);

	/// <summary>
	/// 获取当前输入值
	/// </summary>
	string GetValue();

	/// <summary>
	/// 设置输入值
	/// </summary>
	void SetValue(string value);

	/// <summary>
	/// 获取提示文本
	/// </summary>
	string GetHint();
}
