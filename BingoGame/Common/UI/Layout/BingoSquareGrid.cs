using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace BingoGame.Common.UI.Layout;

/// <summary>
/// 正方形网格布局，用于显示棋盘等正方形排列的内容
/// </summary>
internal sealed class BingoSquareGrid : UIElement
{
	private readonly List<UIElement> _cells = new();
	private readonly int _columns;

	/// <summary>
	/// 格子之间的间距
	/// </summary>
	public float Spacing { get; set; }

	/// <summary>
	/// 创建正方形网格
	/// </summary>
	/// <param name="columns">列数</param>
	/// <param name="spacing">格子间的间距</param>
	public BingoSquareGrid(int columns, float spacing = 4f)
	{
		_columns = Math.Max(1, columns);
		Spacing = Math.Max(0f, spacing);
		OverflowHidden = true;
	}

	/// <summary>
	/// 添加格子
	/// </summary>
	public T AddCell<T>(T child) where T : UIElement
	{
		ArgumentNullException.ThrowIfNull(child);
		_cells.Add(child);
		Append(child);
		return child;
	}

	/// <summary>
	/// 清空所有格子
	/// </summary>
	public void ClearCells()
	{
		_cells.Clear();
		RemoveAllChildren();
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
			BingoStackPanel.SetChildBounds(cell, left + column * (cellSize + horizontalGap),
				top + row * (cellSize + verticalGap), cellSize, cellSize);
			cell.Recalculate();
		}
	}
}
