namespace BingoGame.Common.UI.Layout;

/// <summary>
/// 水平栈面板，将子元素水平排列
/// </summary>
internal sealed class BingoHorizontalStack : BingoStackPanel
{
	/// <summary>
	/// 创建水平栈面板
	/// </summary>
	/// <param name="spacing">子元素间的间距</param>
	public BingoHorizontalStack(float spacing = 0f) : base(false, spacing)
	{
	}
}
