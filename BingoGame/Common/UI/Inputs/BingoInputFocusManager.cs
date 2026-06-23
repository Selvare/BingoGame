namespace BingoGame.Common.UI.Inputs;

/// <summary>
/// Bingo 输入焦点管理器 - 统一管理所有输入框的焦点
/// 避免多个输入框同时处于聚焦状态
/// </summary>
internal static class BingoInputFocusManager
{
	private static IBingoInputElement _focused;

	/// <summary>
	/// 是否有输入框被聚焦
	/// </summary>
	public static bool AnyFocused => _focused != null;

	/// <summary>
	/// 聚焦指定输入框
	/// </summary>
	public static void Focus(IBingoInputElement input)
	{
		if (_focused != input)
		{
			// 失焦之前的输入框
			if (_focused != null)
				_focused.Blur(commit: true);

			_focused = input;
			input.Focus();
		}
	}

	/// <summary>
	/// 失焦指定输入框
	/// </summary>
	public static void Blur(IBingoInputElement input, bool commit = true)
	{
		if (_focused == input)
		{
			_focused = null;
			input.Blur(commit);
		}
	}

	/// <summary>
	/// 清除所有焦点
	/// </summary>
	public static void ClearFocus(bool commit = true)
	{
		if (_focused != null)
		{
			IBingoInputElement target = _focused;
			_focused = null;
			target.Blur(commit);
		}
	}

	/// <summary>
	/// 检查指定输入框是否被聚焦
	/// </summary>
	public static bool IsFocused(IBingoInputElement input)
	{
		return _focused == input;
	}

	/// <summary>
	/// 获取当前聚焦的输入框
	/// </summary>
	public static IBingoInputElement GetFocused() => _focused;
}
