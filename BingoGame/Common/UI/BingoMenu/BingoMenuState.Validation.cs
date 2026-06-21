using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private bool ValidateDraft(out BingoValidationFailure failure)
	{
		HashSet<int> seen = new();
		for (int i = 0; i < _draftItems.Length; i++)
		{
			int itemType = _draftItems[i];
			if (itemType == 0)
				continue;
			if (!BingoWorldSystem.IsUsableItemId(itemType))
			{
				failure = new BingoValidationFailure(BingoValidationError.InvalidItem, i);
				return false;
			}
			if (!seen.Add(itemType))
			{
				failure = new BingoValidationFailure(BingoValidationError.DuplicateItem, i);
				return false;
			}
		}
		failure = default;
		return true;
	}

	private string FailureText()
	{
		if (_failure.Error == BingoValidationError.None)
			return string.Empty;
		string key = $"UI.Errors.{_failure.Error}";
		return _failure.CellIndex >= 0 ? Text(key, _failure.CellIndex + 1) : Text(key);
	}

	private static BingoEditorSaveResult SaveSuccess() => new(BingoEditorSaveStatus.Success,
		Text("UI.SaveStatus.Success"));

	private static BingoEditorSaveResult SaveWarning(string message) => new(BingoEditorSaveStatus.Warning,
		Text("UI.SaveStatus.Warning", message));

	private static BingoEditorSaveResult SaveFailure(string message) => new(BingoEditorSaveStatus.Failure,
		Text("UI.SaveStatus.Failure", message));

	private static Color EditorResultColor(BingoEditorSaveStatus status) => status switch
	{
		BingoEditorSaveStatus.Success => new Color(80, 210, 90),
		BingoEditorSaveStatus.Warning => new Color(235, 210, 65),
		BingoEditorSaveStatus.Failure => Color.OrangeRed,
		_ => Color.White
	};
}

