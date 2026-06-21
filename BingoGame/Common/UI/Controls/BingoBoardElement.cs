using System;
using System.Collections.Generic;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed class BingoBoardElement : UIElement
{
	private const float CellSpacing = 4f;
	private readonly int _size;
	private readonly List<BingoBoardCell> _cells = new();

	public BingoBoardElement(int size, IReadOnlyList<int> itemTypes, IReadOnlyList<byte> owners,
		IReadOnlyList<BingoClaimRecord> claims, Func<Color> borderColor, bool singlePlayer)
	{
		_size = size;
		Dictionary<int, BingoClaimRecord> claimsByItemType = new(claims.Count);
		foreach (BingoClaimRecord claim in claims)
			claimsByItemType.TryAdd(claim.ItemType, claim);
		int count = Math.Min(itemTypes.Count, owners.Count);
		for (int index = 0; index < count; index++)
		{
			claimsByItemType.TryGetValue(itemTypes[index], out BingoClaimRecord claim);
			BingoBoardCell cell = new(itemTypes[index], owners[index],
				claim == default ? null : claim, borderColor, singlePlayer);
			_cells.Add(cell);
			Append(cell);
		}
	}

	public override void RecalculateChildren()
	{
		CalculatedStyle dimensions = GetDimensions();
		float availableWidth = Math.Max(0f, dimensions.Width - CellSpacing * (_size - 1));
		float availableHeight = Math.Max(0f, dimensions.Height - CellSpacing * (_size - 1));
		float cellSize = Math.Max(0f, Math.Min(availableWidth, availableHeight) / _size);
		float boardPixels = cellSize * _size + CellSpacing * (_size - 1);
		float boardLeft = Math.Max(0f, (dimensions.Width - boardPixels) * 0.5f);
		float boardTop = Math.Max(0f, (dimensions.Height - boardPixels) * 0.5f);
		for (int index = 0; index < _cells.Count; index++)
		{
			int row = index / _size;
			int column = index % _size;
			BingoBoardCell cell = _cells[index];
			UIStack.SetChildBounds(cell, boardLeft + column * (cellSize + CellSpacing),
				boardTop + row * (cellSize + CellSpacing), cellSize, cellSize);
			cell.Recalculate();
		}
	}

	internal static Color GetTeamColor(int team) => BingoTeamDisplay.GetColor(team);
}
