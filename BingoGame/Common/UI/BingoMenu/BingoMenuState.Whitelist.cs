using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private static readonly Regex WhitelistTokenPattern = new(
		@"^(?<start>[1-9]\d*)(?:\s*~\s*(?<end>[1-9]\d*))?$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private void BuildWhitelistList()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.WhitelistList, 520f, 360f, 620f, 470f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText title = CreateText(panel, Text("UI.WhitelistListTitle"), 0.5f, 0.5f, 1.15f,
			BingoTextRole.Title);
		BingoScrollList list = new(6f);
		BingoGameConfig config = GetGameConfig();

		for (int index = 0; index < config.Whitelists.Count; index++)
		{
			int capturedIndex = index;
			BingoWhitelistEntry entry = config.Whitelists[index];
			UIHorizontalStack row = new(8f);
			BingoTextInput name = new(Text("UI.WhitelistNameHint"), entry.Name, () => panel.LayoutScale,
				null, value => CommitWhitelistName(capturedIndex, value), 64);
			BingoButton toggle = CreateButton(panel, Text(entry.Enabled ? "UI.Enabled" : "UI.Disabled"),
				() => ToggleWhitelistEntry(capturedIndex), entry.Enabled, textRole: BingoTextRole.Compact,
			backgroundColor: entry.Enabled
				? BingoUITheme.SuccessBackground
				: null);
			BingoButton edit = CreateButton(panel, Text("UI.Edit"), () => OpenWhitelistEditor(capturedIndex),
				textRole: BingoTextRole.Compact);
			BingoButton delete = CreateButton(panel, Text("UI.Delete"), () => DeleteWhitelist(capturedIndex),
				textRole: BingoTextRole.Compact, backgroundColor: BingoUITheme.DangerBackground);
			row.AddWeighted(name, 5f, 180f);
			row.AddWeighted(edit, 2f, 70f);
			row.AddWeighted(toggle, 2f, 80f);
			row.AddWeighted(delete, 2f, 70f);
			list.AddRow(row, 48f);
		}

		BingoButton add = CreateButton(panel, "+", AddWhitelist, emphasized: true);
		list.AddRow(add, 44f);
		UIHorizontalStack footer = new(12f);
		BingoButton back = CreateButton(panel, Text("UI.Back"), CloseWhitelistList);
		footer.AddWeighted(back);
		root.AddFixed(title, 34f);
		root.AddWeighted(list, 1f, 220f);
		root.AddFixed(footer, 44f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildWhitelistEditor()
	{
		BingoGameConfig config = GetGameConfig();
		if (_editingWhitelistIndex < 0 || _editingWhitelistIndex >= config.Whitelists.Count)
		{
			_editingWhitelistIndex = -1;
			_editingWhitelists = true;
			Rebuild();
			return;
		}

		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.WhitelistEditor, 460f, 230f, 560f, 260f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoWhitelistEntry entry = config.Whitelists[_editingWhitelistIndex];
		string titleValue = string.IsNullOrWhiteSpace(entry.Name)
			? Text("UI.WhitelistDefaultName", _editingWhitelistIndex + 1)
			: entry.Name;
		BingoAdaptiveText title = CreateText(panel, Text("UI.WhitelistEditorTitle", titleValue), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		BingoAdaptiveText status = CreateText(panel, _whitelistEditorResult.Message, 0.5f, 0.5f, 0.75f,
			BingoTextRole.Compact, EditorResultColor(_whitelistEditorResult.Status));
		BingoTextInput input = new(Text("UI.WhitelistItemsHint"), _whitelistEditorText, () => panel.LayoutScale,
			value =>
			{
				_whitelistEditorText = value;
				_whitelistEditorResult = default;
				status.SetText(string.Empty);
			}, null, ushort.MaxValue);
		input.IsInvalid = _whitelistEditorResult.Failed;
		UIHorizontalStack footer = new(12f);
		BingoButton back = CreateButton(panel, Text("UI.Back"), CloseWhitelistEditor);
		BingoButton save = CreateButton(panel, Text("UI.Save"), SaveWhitelistEditor, emphasized: true);
		footer.AddWeighted(back);
		footer.AddWeighted(save);
		root.AddFixed(title, 34f);
		root.AddWeighted(input, 1f, 70f);
		root.AddFixed(status, 26f);
		root.AddFixed(footer, 44f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void OpenWhitelistList()
	{
		_editingWhitelists = true;
		_editingWhitelistIndex = -1;
		_whitelistEditorResult = default;
		Rebuild();
	}

	private void CloseWhitelistList()
	{
		BingoTextInput.ClearFocus();
		GetGameConfig().SaveChanges();
		_editingWhitelists = false;
		Rebuild();
	}

	private void AddWhitelist()
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		config.Whitelists.Add(new BingoWhitelistEntry
		{
			Name = Text("UI.WhitelistDefaultName", config.Whitelists.Count + 1)
		});
		config.SaveChanges();
		Rebuild();
	}

	private void CommitWhitelistName(int index, string value)
	{
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.Whitelists.Count)
			return;
		string normalized = value?.Trim() ?? string.Empty;
		if (normalized.Length == 0)
			normalized = Text("UI.WhitelistDefaultName", index + 1);
		if (config.Whitelists[index].Name == normalized)
			return;
		config.Whitelists[index].Name = normalized;
		config.SaveChanges();
	}

	private void ToggleWhitelistEntry(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.Whitelists.Count)
			return;
		config.Whitelists[index].Enabled = !config.Whitelists[index].Enabled;
		config.SaveChanges();
		Rebuild();
	}

	private void DeleteWhitelist(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.Whitelists.Count)
			return;
		config.Whitelists.RemoveAt(index);
		config.SaveChanges();
		Rebuild();
	}

	private void OpenWhitelistEditor(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.Whitelists.Count)
			return;
		_editingWhitelistIndex = index;
		_whitelistEditorText = string.Join(",", config.Whitelists[index].ItemTypes);
		_whitelistEditorResult = default;
		Rebuild();
	}

	private void CloseWhitelistEditor()
	{
		BingoTextInput.ClearFocus(false);
		if (TryCommitWhitelistEditor().Failed)
		{
			Rebuild();
			return;
		}
		_editingWhitelistIndex = -1;
		_whitelistEditorResult = default;
		Rebuild();
	}

	private void SaveWhitelistEditor()
	{
		BingoTextInput.ClearFocus(false);
		TryCommitWhitelistEditor();
		Rebuild();
	}

	private BingoEditorSaveResult TryCommitWhitelistEditor()
	{
		BingoEditorSaveResult result = ParseWhitelistItems(_whitelistEditorText, out List<int> itemTypes);
		if (result.Failed)
			return _whitelistEditorResult = result;
		BingoGameConfig config = GetGameConfig();
		if (_editingWhitelistIndex < 0 || _editingWhitelistIndex >= config.Whitelists.Count)
			return _whitelistEditorResult = SaveFailure(Text("UI.SaveStatus.Unavailable"));
		config.Whitelists[_editingWhitelistIndex].ItemTypes = itemTypes;
		config.SaveChanges();
		return _whitelistEditorResult = result;
	}

	private static BingoEditorSaveResult ParseWhitelistItems(string value, out List<int> itemTypes)
	{
		itemTypes = new List<int>();
		if (string.IsNullOrWhiteSpace(value))
			return SaveSuccess();

		HashSet<int> seen = new();
		int filteredCount = 0;
		string[] tokens = value.Split(',');
		for (int i = 0; i < tokens.Length; i++)
		{
			string token = tokens[i].Trim();
			if (token.Length == 0)
			{
				return SaveFailure(Text("UI.WhitelistErrors.EmptyToken", i + 1));
			}
			Match match = WhitelistTokenPattern.Match(token);
			if (!match.Success || !int.TryParse(match.Groups["start"].Value, out int start))
			{
				return SaveFailure(Text("UI.WhitelistErrors.InvalidNumber", token));
			}

			int end = start;
			if (match.Groups["end"].Success && !int.TryParse(match.Groups["end"].Value, out end))
			{
				return SaveFailure(Text("UI.WhitelistErrors.InvalidNumber", token));
			}
			if (end < start)
			{
				return SaveFailure(Text("UI.WhitelistErrors.InvalidRange", token));
			}

			for (int itemType = start; ; itemType++)
			{
				if (!BingoWorldSystem.IsExistingItemId(itemType))
				{
					return SaveFailure(Text("UI.WhitelistErrors.InvalidItem", itemType));
				}
				if (!BingoWorldSystem.IsUsableItemId(itemType))
				{
					filteredCount++;
					if (itemType == end)
						break;
					continue;
				}
				if (!seen.Add(itemType))
				{
					return SaveFailure(Text("UI.WhitelistErrors.DuplicateItem", itemType));
				}
				itemTypes.Add(itemType);
				if (itemType == end)
					break;
			}
		}
		return filteredCount > 0
			? SaveWarning(Text("UI.WhitelistErrors.FilteredItems", filteredCount))
			: SaveSuccess();
	}

	private bool TryBuildWhitelistPool(BingoGameConfig config, out int[] itemTypes,
		out BingoValidationFailure failure)
	{
		if (!config.WhitelistEnabled)
		{
			itemTypes = Array.Empty<int>();
			failure = default;
			return true;
		}

		HashSet<int> unique = new();
		foreach (BingoWhitelistEntry whitelist in config.Whitelists)
		{
			if (!whitelist.Enabled)
				continue;
			foreach (int itemType in whitelist.ItemTypes)
			{
				if (!BingoWorldSystem.IsUsableItemId(itemType))
				{
					itemTypes = Array.Empty<int>();
					failure = new BingoValidationFailure(BingoValidationError.InvalidWhitelist, -1);
					return false;
				}
				unique.Add(itemType);
			}
		}
		if (unique.Count < _draftSize * _draftSize)
		{
			itemTypes = Array.Empty<int>();
			failure = new BingoValidationFailure(BingoValidationError.NotEnoughWhitelistItems, -1);
			return false;
		}
		itemTypes = unique.ToArray();
		failure = default;
		return true;
	}
}

