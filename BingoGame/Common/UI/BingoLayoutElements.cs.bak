using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace BingoGame.Common.UI;

internal enum BingoTextRole
{
	Title,
	Normal,
	Compact
}

internal sealed class BingoAdaptiveText : UIElement
{
	private string _text;
	private readonly float _baseScale;
	private readonly float _horizontalOrigin;
	private readonly float _verticalOrigin;
	private readonly BingoTextRole _role;
	private readonly Func<float> _layoutScale;

	public Color TextColor { get; set; } = Color.White;

	public BingoAdaptiveText(string text, float baseScale, float horizontalOrigin, float verticalOrigin,
		BingoTextRole role, Func<float> layoutScale)
	{
		_text = text ?? string.Empty;
		_baseScale = baseScale;
		_horizontalOrigin = Math.Clamp(horizontalOrigin, 0f, 1f);
		_verticalOrigin = Math.Clamp(verticalOrigin, 0f, 1f);
		_role = role;
		_layoutScale = layoutScale;
	}

	public void SetText(string text) => _text = text ?? string.Empty;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetInnerDimensions();
		if (dimensions.Width <= 0f || dimensions.Height <= 0f || _text.Length == 0)
			return;

		float scale = CalculateScale(_text, dimensions.Width, dimensions.Height, _baseScale, _role,
			_layoutScale?.Invoke() ?? 1f);
		Vector2 position = new(dimensions.X + dimensions.Width * _horizontalOrigin,
			dimensions.Y + dimensions.Height * _verticalOrigin);
		Utils.DrawBorderString(spriteBatch, _text, position, TextColor, scale, _horizontalOrigin, _verticalOrigin);
	}

	internal static float CalculateScale(string text, float width, float height, float baseScale,
		BingoTextRole role, float layoutScale)
	{
		(float minimum, float maximum) = role switch
		{
			BingoTextRole.Title => (0.8f, 1.35f),
			BingoTextRole.Compact => (0.55f, 0.9f),
			_ => (0.65f, 1.1f)
		};

		float desired = Math.Clamp(baseScale * Math.Clamp(layoutScale, 0.75f, 1.35f), minimum, maximum);
		Vector2 measured = FontAssets.MouseText.Value.MeasureString(text);
		float fitWidth = Math.Max(1f, width - 6f) / Math.Max(1f, measured.X);
		float fitHeight = Math.Max(1f, height - 2f) / Math.Max(1f, measured.Y);
		float fitted = Math.Min(desired, Math.Min(fitWidth, fitHeight));
		return Math.Clamp(fitted, 0.4f, maximum);
	}
}

internal abstract class UIStack : UIElement
{
	private readonly List<StackSlot> _slots = new();
	private readonly bool _vertical;

	public float Spacing { get; set; }

	protected UIStack(bool vertical, float spacing = 0f)
	{
		_vertical = vertical;
		Spacing = Math.Max(0f, spacing);
		OverflowHidden = true;
	}

	public T AddFixed<T>(T child, float pixels) where T : UIElement
	{
		AddSlot(child, Math.Max(0f, pixels), 0f);
		return child;
	}

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

internal sealed class UIVerticalStack : UIStack
{
	public UIVerticalStack(float spacing = 0f) : base(true, spacing)
	{
	}
}

internal sealed class UIHorizontalStack : UIStack
{
	public UIHorizontalStack(float spacing = 0f) : base(false, spacing)
	{
	}
}

internal sealed class BingoScrollList : UIElement
{
	private readonly List<(UIElement Element, float Height)> _rows = new();
	private readonly float _spacing;
	private float _scrollOffset;
	private float _maximumScroll;

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

	public void AddRow(UIElement element, float height)
	{
		ArgumentNullException.ThrowIfNull(element);
		_rows.Add((element, Math.Max(1f, height)));
		Append(element);
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

internal sealed class UIBingoBoardGrid : UIElement
{
	private readonly List<UIElement> _cells = new();
	private readonly int _columns;

	public float Spacing { get; set; }

	public UIBingoBoardGrid(int columns, float spacing = 4f)
	{
		_columns = Math.Max(1, columns);
		Spacing = Math.Max(0f, spacing);
		OverflowHidden = true;
	}

	public T AddCell<T>(T child) where T : UIElement
	{
		ArgumentNullException.ThrowIfNull(child);
		_cells.Add(child);
		Append(child);
		return child;
	}

	public override void Recalculate()
	{
		base.Recalculate();
	}

	public override void RecalculateChildren()
	{
		if (_cells.Count == 0)
			return;

		CalculatedStyle inner = GetInnerDimensions();
		float width = Math.Max(0f, inner.Width);
		float height = Math.Max(0f, inner.Height);
		int rows = (_cells.Count + _columns - 1) / _columns;
		float horizontalGap = _columns > 1 ? Math.Min(Spacing, width / (_columns - 1)) : 0f;
		float verticalGap = rows > 1 ? Math.Min(Spacing, height / (rows - 1)) : 0f;
		float cellSize = Math.Max(0f, Math.Min(
			(width - horizontalGap * (_columns - 1)) / _columns,
			(height - verticalGap * (rows - 1)) / rows));
		float boardWidth = cellSize * _columns + horizontalGap * (_columns - 1);
		float boardHeight = cellSize * rows + verticalGap * (rows - 1);
		float left = Math.Max(0f, (width - boardWidth) * 0.5f);
		float top = Math.Max(0f, (height - boardHeight) * 0.5f);

		for (int index = 0; index < _cells.Count; index++)
		{
			int row = index / _columns;
			int column = index % _columns;
			UIElement cell = _cells[index];
			UIStack.SetChildBounds(cell, left + column * (cellSize + horizontalGap),
				top + row * (cellSize + verticalGap), cellSize, cellSize);
			cell.Recalculate();
		}
	}
}
