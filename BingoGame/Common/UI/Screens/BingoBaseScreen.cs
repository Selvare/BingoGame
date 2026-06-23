using BingoGame.Common.UI.Core;
using Terraria.UI;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 屏幕基类，所有游戏屏幕继承此类
/// 负责屏幕的构建和销毁
/// </summary>
internal abstract class BingoBaseScreen : BingoScreen
{
	protected BingoUiContext Context { get; private set; }
	protected UIState UiState { get; private set; }

	/// <summary>
	/// 初始化屏幕
	/// </summary>
	protected virtual void Initialize()
	{
		// 子类可覆盖此方法进行初始化
	}

	/// <summary>
	/// 清理屏幕资源
	/// </summary>
	protected virtual void Cleanup()
	{
		// 子类可覆盖此方法进行清理
	}

	public override void Build(UIState uiState, BingoUiContext context)
	{
		UiState = uiState;
		Context = context;
		Initialize();
		BuildUI();
	}

	/// <summary>
	/// 构建 UI 元素，子类需要实现此方法
	/// </summary>
	protected abstract void BuildUI();
}
