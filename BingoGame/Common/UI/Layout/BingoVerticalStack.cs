namespace BingoGame.Common.UI.Layout;

/// <summary>
/// 垂直栈面板，将子元素竖直排列
/// </summary>
internal sealed class BingoVerticalStack : BingoStackPanel
{
	/// <summary>
	/// 创建垂直栈面板
	/// </summary>
	/// <param name="spacing">子元素间的间距</param>
	public BingoVerticalStack(float spacing = 0f) : base(true, spacing)
	{
	}
}
