using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BingoGame.Common.UI.Layout;

/// <summary>
/// 可滚动列表，支持鼠标滚轮滚动
/// </summary>
internal sealed class BingoScrollList : UIElement
{
	private readonly List<(UIElement Element, float Height)> _rows = new();
	private readonly float _spacing;
	private float _scrollOffset;
	private float _maximumScroll;

	/// <summary>
	/// 创建可滚动列表
	/// </summary>
	/// <param name="spacing">行之间的间距</param>
	public BingoScrollList(float spacing = 4f)
	{
		_spacing = Math.Max(0f, spacing);
		OverflowHidden = true;
		OnScrollWheel += (evt, _) =>
		{
			_scrollOffset = Math.Clamp(_scrollOffset - evt.ScrollWheelValue, 0f, _maximumScroll);
			RecalculateChildren();
		};
	}

	/// <summary>
	/// 添加一行
	/// </summary>
	public void AddRow(UIElement element, float height)
	{
		ArgumentNullException.ThrowIfNull(element);
		_rows.Add((element, Math.Max(1f, height)));
		Append(element);
	}

	/// <summary>
	/// 清空所有行
	/// </summary>
	public void ClearRows()
	{
		_rows.Clear();
		RemoveAllChildren();
		_scrollOffset = 0f;
		_maximumScroll = 0f;
	}

	/// <summary>
	/// 滚动到顶部
	/// </summary>
	public void ScrollToTop()
	{
		_scrollOffset = 0f;
		RecalculateChildren();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	public override void RecalculateChildren()
	{
		CalculatedStyle inner = GetInnerDimensions();
		float totalHeight = 0f;
		for (int i = 0; i < _rows.Count; i++)
			totalHeight += _rows[i].Height + (i > 0 ? _spacing : 0f);
		_maximumScroll = Math.Max(0f, totalHeight - inner.Height);
		_scrollOffset = Math.Clamp(_scrollOffset, 0f, _maximumScroll);

		float top = -_scrollOffset;
		foreach ((UIElement element, float height) in _rows)
		{
			element.HAlign = 0f;
			element.VAlign = 0f;
			element.Left.Set(0f, 0f);
			element.Top.Set(top, 0f);
			element.Width.Set(0f, 1f);
			element.Height.Set(height, 0f);
			element.MinWidth.Set(0f, 0f);
			element.MinHeight.Set(0f, 0f);
			element.MaxWidth.Set(0f, 1f);
			element.MaxHeight.Set(height, 0f);
			element.Recalculate();
			top += height + _spacing;
		}
	}
}
