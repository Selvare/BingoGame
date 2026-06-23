using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace BingoGame.Common.UI.Layout;

/// <summary>
/// 栈面板基类，用于线性排列子元素
/// </summary>
internal abstract class BingoStackPanel : UIElement
{
	private readonly List<StackSlot> _slots = new();
	private readonly bool _vertical;

	/// <summary>
	/// 子元素间的间距
	/// </summary>
	public float Spacing { get; set; }

	/// <summary>
	/// 创建栈面板
	/// </summary>
	/// <param name="vertical">是否为垂直排列</param>
	/// <param name="spacing">子元素间的间距</param>
	protected BingoStackPanel(bool vertical, float spacing = 0f)
	{
		_vertical = vertical;
		Spacing = Math.Max(0f, spacing);
		OverflowHidden = true;
	}

	/// <summary>
	/// 添加固定大小的子元素
	/// </summary>
	public T AddFixed<T>(T child, float pixels) where T : UIElement
	{
		AddSlot(child, Math.Max(0f, pixels), 0f);
		return child;
	}

	/// <summary>
	/// 添加按权重分配空间的子元素
	/// </summary>
	public T AddWeighted<T>(T child, float weight = 1f, float minimumPixels = 0f) where T : UIElement
	{
		AddSlot(child, Math.Max(0f, minimumPixels), Math.Max(0.0001f, weight));
		return child;
	}

	public override void Recalculate()
	{
		base.Recalculate();
	}

	public override void RecalculateChildren()
	{
		if (_slots.Count == 0)
			return;

		CalculatedStyle inner = GetInnerDimensions();
		float mainLength = Math.Max(0f, _vertical ? inner.Height : inner.Width);
		float crossLength = Math.Max(0f, _vertical ? inner.Width : inner.Height);
		float gap = _slots.Count > 1 ? Math.Min(Spacing, mainLength / (_slots.Count - 1)) : 0f;
		float contentLength = Math.Max(0f, mainLength - gap * (_slots.Count - 1));
		float basisTotal = 0f;
		float weightTotal = 0f;
		foreach (StackSlot slot in _slots)
		{
			basisTotal += slot.Basis;
			weightTotal += slot.Weight;
		}

		float basisScale = basisTotal > contentLength && basisTotal > 0f ? contentLength / basisTotal : 1f;
		float remaining = Math.Max(0f, contentLength - basisTotal * basisScale);
		float cursor = 0f;
		for (int index = 0; index < _slots.Count; index++)
		{
			StackSlot slot = _slots[index];
			float mainSize = slot.Basis * basisScale;
			if (weightTotal > 0f && slot.Weight > 0f)
				mainSize += remaining * slot.Weight / weightTotal;
			mainSize = Math.Clamp(mainSize, 0f, Math.Max(0f, mainLength - cursor));

			if (_vertical)
				SetChildBounds(slot.Child, 0f, cursor, crossLength, mainSize);
			else
				SetChildBounds(slot.Child, cursor, 0f, mainSize, crossLength);
			slot.Child.Recalculate();
			cursor = Math.Min(mainLength, cursor + mainSize + (index < _slots.Count - 1 ? gap : 0f));
		}
	}

	private void AddSlot(UIElement child, float basis, float weight)
	{
		ArgumentNullException.ThrowIfNull(child);
		_slots.Add(new StackSlot(child, basis, weight));
		Append(child);
	}

	/// <summary>
	/// 设置子元素的边界
	/// </summary>
	internal static void SetChildBounds(UIElement child, float left, float top, float width, float height)
	{
		left = Math.Max(0f, left);
		top = Math.Max(0f, top);
		width = Math.Max(0f, width);
		height = Math.Max(0f, height);
		child.HAlign = 0f;
		child.VAlign = 0f;
		child.Left.Set(left, 0f);
		child.Top.Set(top, 0f);
		child.Width.Set(width, 0f);
		child.Height.Set(height, 0f);
		child.MinWidth.Set(0f, 0f);
		child.MinHeight.Set(0f, 0f);
		child.MaxWidth.Set(width, 0f);
		child.MaxHeight.Set(height, 0f);
	}

	private readonly record struct StackSlot(UIElement Child, float Basis, float Weight);
}
