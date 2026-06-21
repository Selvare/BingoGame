using System;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private void BuildBoardEditor()
	{
		float defaultCellSize = Math.Clamp(Math.Min(Main.screenWidth - 100f, Main.screenHeight - 180f) / _draftSize, 38f, 62f);
		float minimumWidth = Math.Max(360f, _draftSize * 38f + 40f);
		float minimumHeight = _draftSize * 38f + 125f;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Editor, minimumWidth, minimumHeight,
			defaultCellSize * _draftSize + 40f, defaultCellSize * _draftSize + 125f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText title = CreateText(panel, Text("UI.BoardEditorTitle"), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		UIBingoBoardGrid grid = new(_draftSize, 4f);

		for (int index = 0; index < _draftItems.Length; index++)
		{
			int capturedIndex = index;
			BingoNumericInput input = new(Text("UI.RandomHint"), _draftItems[index], () => panel.LayoutScale, value =>
			{
				_draftItems[capturedIndex] = value;
				if (_failure.CellIndex == capturedIndex)
					_failure = default;
			});
			input.IsInvalid = _failure.Error != BingoValidationError.None && _failure.CellIndex == index;
			grid.AddCell(input);
		}

		UIHorizontalStack footer = new(12f);
		BingoButton back = CreateButton(panel, Text("UI.Back"), CloseEditor);
		BingoAdaptiveText failure = CreateText(panel, FailureText(), 0.5f, 0.5f, 0.72f,
			BingoTextRole.Compact, BingoUITheme.InputInvalidBorder);
		BingoButton clear = CreateButton(panel, Text("UI.Clear"), ClearDraftItems,
			backgroundColor: BingoUITheme.DangerBackground);
		footer.AddWeighted(back, 1f, 90f);
		footer.AddWeighted(failure, 2f, 100f);
		footer.AddWeighted(clear, 1f, 90f);
		root.AddFixed(title, 32f);
		root.AddWeighted(grid, 1f, _draftSize * 24f);
		root.AddFixed(footer, 42f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void OpenEditor()
	{
		_editingBoard = true;
		Rebuild();
	}

	private void CloseEditor()
	{
		BingoNumericInput.ClearFocus();
		SaveDraftToConfig();
		_editingBoard = false;
		Rebuild();
	}

	private void ClearDraftItems()
	{
		Array.Clear(_draftItems, 0, _draftItems.Length);
		_failure = default;
		BingoNumericInput.ClearFocus();
		Rebuild();
	}
}
