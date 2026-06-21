using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Terraria;
using Terraria.ID;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private static readonly Regex InitialItemTokenPattern = new(
		@"^(?<id>[1-9]\d*)(?:\s+(?<stack>[1-9]\d*))?$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private void BuildInitialItemList()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.InitialItemList, 520f, 360f, 620f, 470f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText title = CreateText(panel, Text("UI.InitialItemListTitle"), 0.5f, 0.5f, 1.15f,
			BingoTextRole.Title);
		BingoScrollList list = new(6f);
		BingoGameConfig config = GetGameConfig();

		for (int index = 0; index < config.InitialItemLists.Count; index++)
		{
			int capturedIndex = index;
			BingoInitialItemListEntry entry = config.InitialItemLists[index];
			UIHorizontalStack row = new(8f);
			BingoTextInput name = new(Text("UI.InitialItemNameHint"), entry.Name, () => panel.LayoutScale,
				null, value => CommitInitialItemListName(capturedIndex, value), 64);
			BingoButton toggle = CreateButton(panel, Text(entry.Enabled ? "UI.Enabled" : "UI.Disabled"),
				() => ToggleInitialItemListEntry(capturedIndex), entry.Enabled, textRole: BingoTextRole.Compact,
			backgroundColor: entry.Enabled
				? BingoUITheme.SuccessBackground
				: null);
			BingoButton edit = CreateButton(panel, Text("UI.Edit"), () => OpenInitialItemEditor(capturedIndex),
				textRole: BingoTextRole.Compact);
			BingoButton delete = CreateButton(panel, Text("UI.Delete"),
				() => DeleteInitialItemList(capturedIndex), textRole: BingoTextRole.Compact,
				backgroundColor: BingoUITheme.DangerBackground);
			row.AddWeighted(name, 5f, 180f);
			row.AddWeighted(edit, 2f, 70f);
			row.AddWeighted(toggle, 2f, 80f);
			row.AddWeighted(delete, 2f, 70f);
			list.AddRow(row, 48f);
		}

		BingoButton add = CreateButton(panel, "+", AddInitialItemList, emphasized: true);
		list.AddRow(add, 44f);
		UIHorizontalStack footer = new(12f);
		footer.AddWeighted(CreateButton(panel, Text("UI.Back"), CloseInitialItemList));
		root.AddFixed(title, 34f);
		root.AddWeighted(list, 1f, 220f);
		root.AddFixed(footer, 44f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildInitialItemEditor()
	{
		BingoGameConfig config = GetGameConfig();
		if (_editingInitialItemListIndex < 0 || _editingInitialItemListIndex >= config.InitialItemLists.Count)
		{
			_editingInitialItemListIndex = -1;
			_editingInitialItemLists = true;
			Rebuild();
			return;
		}

		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.InitialItemEditor, 460f, 230f, 560f, 260f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoInitialItemListEntry entry = config.InitialItemLists[_editingInitialItemListIndex];
		string titleValue = string.IsNullOrWhiteSpace(entry.Name)
			? Text("UI.InitialItemDefaultName", _editingInitialItemListIndex + 1)
			: entry.Name;
		BingoAdaptiveText title = CreateText(panel, Text("UI.InitialItemEditorTitle", titleValue),
			0.5f, 0.5f, 1.1f, BingoTextRole.Title);
		BingoAdaptiveText status = CreateText(panel, _initialItemEditorResult.Message, 0.5f, 0.5f, 0.75f,
			BingoTextRole.Compact, EditorResultColor(_initialItemEditorResult.Status));
		BingoTextInput input = new(Text("UI.InitialItemItemsHint"), _initialItemEditorText,
			() => panel.LayoutScale, value =>
			{
				_initialItemEditorText = value;
				_initialItemEditorResult = default;
				status.SetText(string.Empty);
			}, null, ushort.MaxValue);
		input.IsInvalid = _initialItemEditorResult.Failed;
		UIHorizontalStack footer = new(12f);
		footer.AddWeighted(CreateButton(panel, Text("UI.Back"), CloseInitialItemEditor));
		footer.AddWeighted(CreateButton(panel, Text("UI.Save"), SaveInitialItemEditor, emphasized: true));
		root.AddFixed(title, 34f);
		root.AddWeighted(input, 1f, 70f);
		root.AddFixed(status, 26f);
		root.AddFixed(footer, 44f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void OpenInitialItemList()
	{
		_editingInitialItemLists = true;
		_editingInitialItemListIndex = -1;
		_initialItemEditorResult = default;
		_inventoryActionError = string.Empty;
		Rebuild();
	}

	private void CloseInitialItemList()
	{
		BingoTextInput.ClearFocus();
		GetGameConfig().SaveChanges();
		_editingInitialItemLists = false;
		Rebuild();
	}

	private void AddInitialItemList()
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		config.InitialItemLists.Add(new BingoInitialItemListEntry
		{
			Name = Text("UI.InitialItemDefaultName", config.InitialItemLists.Count + 1)
		});
		config.SaveChanges();
		Rebuild();
	}

	private void CommitInitialItemListName(int index, string value)
	{
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.InitialItemLists.Count)
			return;
		string normalized = value?.Trim() ?? string.Empty;
		if (normalized.Length == 0)
			normalized = Text("UI.InitialItemDefaultName", index + 1);
		if (config.InitialItemLists[index].Name == normalized)
			return;
		config.InitialItemLists[index].Name = normalized;
		config.SaveChanges();
	}

	private void ToggleInitialItemListEntry(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.InitialItemLists.Count)
			return;
		bool enable = !config.InitialItemLists[index].Enabled;
		foreach (BingoInitialItemListEntry entry in config.InitialItemLists)
			entry.Enabled = false;
		config.InitialItemLists[index].Enabled = enable;
		config.SaveChanges();
		_inventoryActionError = string.Empty;
		Rebuild();
	}

	private void DeleteInitialItemList(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.InitialItemLists.Count)
			return;
		config.InitialItemLists.RemoveAt(index);
		config.SaveChanges();
		Rebuild();
	}

	private void OpenInitialItemEditor(int index)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		if (index < 0 || index >= config.InitialItemLists.Count)
			return;
		_editingInitialItemListIndex = index;
		_initialItemEditorText = string.Join(",", config.InitialItemLists[index].Items.Select(item =>
			item.Stack == 1 ? item.ItemType.ToString() : $"{item.ItemType} {item.Stack}"));
		_initialItemEditorResult = default;
		Rebuild();
	}

	private void CloseInitialItemEditor()
	{
		BingoTextInput.ClearFocus(false);
		if (TryCommitInitialItemEditor().Failed)
		{
			Rebuild();
			return;
		}
		_editingInitialItemListIndex = -1;
		_initialItemEditorResult = default;
		Rebuild();
	}

	private void SaveInitialItemEditor()
	{
		BingoTextInput.ClearFocus(false);
		TryCommitInitialItemEditor();
		Rebuild();
	}

	private BingoEditorSaveResult TryCommitInitialItemEditor()
	{
		BingoEditorSaveResult result = ParseInitialItems(_initialItemEditorText, out List<BingoInitialItemStack> items);
		if (result.Failed)
			return _initialItemEditorResult = result;
		BingoGameConfig config = GetGameConfig();
		if (_editingInitialItemListIndex < 0 || _editingInitialItemListIndex >= config.InitialItemLists.Count)
			return _initialItemEditorResult = SaveFailure(Text("UI.SaveStatus.Unavailable"));
		config.InitialItemLists[_editingInitialItemListIndex].Items = items;
		config.SaveChanges();
		return _initialItemEditorResult = result;
	}

	private static BingoEditorSaveResult ParseInitialItems(string value, out List<BingoInitialItemStack> items)
	{
		items = new List<BingoInitialItemStack>();
		if (string.IsNullOrWhiteSpace(value))
			return SaveSuccess();

		HashSet<int> seen = new();
		int oversizedStackCount = 0;
		string[] tokens = value.Split(',');
		if (tokens.Length > 512)
			return SaveFailure(Text("UI.InitialItemErrors.TooManyItems", 512));
		for (int i = 0; i < tokens.Length; i++)
		{
			string token = tokens[i].Trim();
			if (token.Length == 0)
			{
				return SaveFailure(Text("UI.InitialItemErrors.EmptyToken", i + 1));
			}
			Match match = InitialItemTokenPattern.Match(token);
			if (!match.Success || !int.TryParse(match.Groups["id"].Value, out int itemType))
			{
				return SaveFailure(Text("UI.InitialItemErrors.InvalidFormat", token));
			}
			int stack = 1;
			if (match.Groups["stack"].Success
				&& (!int.TryParse(match.Groups["stack"].Value, out stack) || stack is < 1 or > 9999))
			{
				return SaveFailure(Text("UI.InitialItemErrors.InvalidStack", token));
			}
			if (!BingoWorldSystem.IsUsableItemId(itemType))
			{
				return SaveFailure(Text("UI.InitialItemErrors.InvalidItem", itemType));
			}
			if (!seen.Add(itemType))
			{
				return SaveFailure(Text("UI.InitialItemErrors.DuplicateItem", itemType));
			}
			if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item sample)
				&& stack > Math.Max(1, sample.maxStack))
				oversizedStackCount++;
			items.Add(new BingoInitialItemStack(itemType, stack));
		}
		return oversizedStackCount > 0
			? SaveWarning(Text("UI.InitialItemErrors.ExceedsMaxStack", oversizedStackCount))
			: SaveSuccess();
	}
}

