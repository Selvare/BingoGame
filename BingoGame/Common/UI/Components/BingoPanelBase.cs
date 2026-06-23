using BingoGame.Common.UI.Theme;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BingoGame.Common.UI.Components;

/// <summary>
/// Bingo 面板基类，所有自定义面板的共同基类
/// </summary>
internal abstract class BingoPanelBase : UIPanel
{
	/// <summary>
	/// 是否阻止鼠标穿透
	/// </summary>
	public bool BlocksMouse { get; set; } = true;

	/// <summary>
	/// 是否启用此控件
	/// </summary>
	public bool Enabled { get; set; } = true;

	protected BingoPanelBase()
	{
		SetPadding(0f);
		OverflowHidden = true;
	}

	public override void Update(GameTime gameTime)
	{
		ApplyStyle();
		base.Update(gameTime);

		if (BlocksMouse && IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}

	/// <summary>
	/// 应用样式到此面板，应在每次Update时调用
	/// </summary>
	protected abstract void ApplyStyle();
}
