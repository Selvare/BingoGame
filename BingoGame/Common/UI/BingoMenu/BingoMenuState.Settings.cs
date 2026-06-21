using System;
using System.Linq;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private void BuildWaitingMessage()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Waiting, 320f, 140f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText message = CreateText(panel, Text("UI.GameNotStarted"), 0.5f, 0.5f, 1.15f,
			BingoTextRole.Title);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(message, 32f);
		root.AddWeighted(new UIElement(), 1f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildSettings()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Settings, 560f, 557f);
		UIVerticalStack root = CreateRootStack(10f, 8f);
		BingoAdaptiveText title = CreateText(panel, Text("UI.Title"), 0.5f, 0.5f, 1.25f, BingoTextRole.Title);
		UIHorizontalStack sizeRow = new(12f);
		BingoAdaptiveText sizeLabel = CreateText(panel, Text("UI.BoardSize"), 0f, 0.5f, 1f);
		UIHorizontalStack sizeControls = new(8f);
		BingoButton decrease = CreateButton(panel, "−", () => ResizeDraft(_draftSize - 1));
		BingoAdaptiveText sizeValue = CreateText(panel, $"{_draftSize} × {_draftSize}", 0.5f, 0.5f, 1f);
		BingoButton increase = CreateButton(panel, "+", () => ResizeDraft(_draftSize + 1));
		sizeControls.AddFixed(decrease, 48f);
		sizeControls.AddWeighted(sizeValue, 1f, 70f);
		sizeControls.AddFixed(increase, 48f);
		sizeRow.AddWeighted(sizeLabel, 4f, 120f);
		sizeRow.AddWeighted(sizeControls, 6f, 180f);

		UIHorizontalStack ruleRow = new(12f);
		BingoAdaptiveText ruleLabel = CreateText(panel, Text("UI.WinRule"), 0f, 0.5f, 1f);
		UIHorizontalStack ruleControls = new(8f);
		BingoButton line = CreateButton(panel, Text("UI.RuleLine"),
			() => SetRule(BingoWinRule.Line), _draftRule == BingoWinRule.Line);
		BingoButton majority = CreateButton(panel, Text("UI.RuleMajority"),
			() => SetRule(BingoWinRule.Majority), _draftRule == BingoWinRule.Majority);
		ruleControls.AddWeighted(line);
		ruleControls.AddWeighted(majority);
		ruleRow.AddWeighted(ruleLabel, 4f, 120f);
		ruleRow.AddWeighted(ruleControls, 6f, 180f);

		BingoGameConfig gameConfig = ModContent.GetInstance<BingoGameConfig>();
		UIHorizontalStack timeLimitRow = new(12f);
		BingoAdaptiveText timeLimitLabel = CreateText(panel, Text("UI.UseTimeLimit"), 0f, 0.5f, 1f);
		UIHorizontalStack timeLimitControls = new(8f);
		BingoButton timeLimitToggle = CreateButton(panel,
			Text(gameConfig.TimeLimitEnabled ? "UI.Enabled" : "UI.Disabled"), ToggleTimeLimit,
			gameConfig.TimeLimitEnabled,
			backgroundColor: gameConfig.TimeLimitEnabled
				? BingoUITheme.SuccessBackground
				: null);
		UIElement timeLimitDetails;
		if (gameConfig.TimeLimitEnabled)
		{
			UIHorizontalStack timeLimitInputs = new(8f);
			BingoTextInput minutes = CreateDigitsInput(panel, Text("UI.Minutes"), gameConfig.TimeLimitMinutes, 10,
				SetTimeLimitMinutes);
			BingoTextInput seconds = CreateDigitsInput(panel, Text("UI.Seconds"), gameConfig.TimeLimitSeconds, 2,
				SetTimeLimitSeconds);
			timeLimitInputs.AddWeighted(minutes);
			timeLimitInputs.AddWeighted(seconds);
			timeLimitDetails = timeLimitInputs;
		}
		else timeLimitDetails = new UIElement();
		timeLimitControls.AddWeighted(timeLimitDetails);
		timeLimitControls.AddWeighted(timeLimitToggle);
		timeLimitRow.AddWeighted(timeLimitLabel, 4f, 120f);
		timeLimitRow.AddWeighted(timeLimitControls, 6f, 180f);

		UIHorizontalStack whitelistRow = new(12f);
		BingoAdaptiveText whitelistLabel = CreateText(panel, Text("UI.UseWhitelist"), 0f, 0.5f, 1f);
		UIHorizontalStack whitelistControls = new(8f);
		BingoButton whitelistToggle = CreateButton(panel,
			Text(gameConfig.WhitelistEnabled ? "UI.Enabled" : "UI.Disabled"), ToggleWhitelist,
			gameConfig.WhitelistEnabled,
			backgroundColor: gameConfig.WhitelistEnabled
				? BingoUITheme.SuccessBackground
				: null);
		BingoButton editWhitelists = CreateButton(panel, Text("UI.EditWhitelists"), OpenWhitelistList);
		whitelistControls.AddWeighted(editWhitelists);
		whitelistControls.AddWeighted(whitelistToggle);
		whitelistRow.AddWeighted(whitelistLabel, 4f, 120f);
		whitelistRow.AddWeighted(whitelistControls, 6f, 180f);

		BingoButton configure = CreateButton(panel, Text("UI.ConfigureBoard"), OpenEditor);
		UIHorizontalStack inventoryActions = new(12f);
		BingoButton giveInitialItems = CreateButton(panel, Text("UI.GiveInitialItems"), GiveInitialItems,
			textRole: BingoTextRole.Compact);
		BingoButton editInitialItems = CreateButton(panel, Text("UI.EditInitialItems"), OpenInitialItemList,
			textRole: BingoTextRole.Compact);
		BingoButton clearPlayerItems = CreateButton(panel, Text("UI.ClearPlayerItems"),
			ClearPlayerItems, textRole: BingoTextRole.Compact,
			backgroundColor: BingoUITheme.DangerBackground);
		inventoryActions.AddWeighted(giveInitialItems);
		inventoryActions.AddWeighted(editInitialItems);
		inventoryActions.AddWeighted(clearPlayerItems);
		BingoAdaptiveText hint = CreateText(panel, Text("UI.EmptyCellsRandom"), 0.5f, 0.5f, 0.8f,
			BingoTextRole.Compact, Color.Silver);
		string failureValue = FailureText();
		if (failureValue.Length == 0)
			failureValue = _inventoryActionError;
		BingoAdaptiveText failure = CreateText(panel, failureValue, 0.5f, 0.5f, 0.78f,
			BingoTextRole.Compact, BingoUITheme.InputInvalidBorder);
		UIHorizontalStack footer = new(24f);
		BingoButton close = CreateButton(panel, Text("UI.Close"), BingoUISystem.Toggle);
		BingoButton start = CreateButton(panel, Text("UI.StartGame"), StartGame, emphasized: true);
		footer.AddWeighted(close);
		footer.AddWeighted(start);

		root.AddFixed(title, 36f);
		root.AddFixed(sizeRow, 46f);
		root.AddFixed(ruleRow, 46f);
		root.AddFixed(timeLimitRow, 46f);
		root.AddFixed(whitelistRow, 46f);
		root.AddFixed(configure, 46f);
		root.AddFixed(inventoryActions, 46f);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(hint, 22f);
		if (failureValue.Length > 0)
			root.AddFixed(failure, 22f);
		root.AddFixed(footer, 48f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void ResizeDraft(int newSize)
	{
		newSize = Math.Clamp(newSize, 2, 10);
		if (newSize == _draftSize)
			return;
		int[] resized = new int[newSize * newSize];
		int overlap = Math.Min(_draftSize, newSize);
		for (int row = 0; row < overlap; row++)
			Array.Copy(_draftItems, row * _draftSize, resized, row * newSize, overlap);
		_draftSize = newSize;
		_draftItems = resized;
		_failure = default;
		Rebuild();
	}

	private void SetRule(BingoWinRule rule)
	{
		_draftRule = rule;
		_failure = default;
		Rebuild();
	}

	private void ToggleWhitelist()
	{
		BingoGameConfig config = GetGameConfig();
		config.WhitelistEnabled = !config.WhitelistEnabled;
		config.SaveChanges();
		_failure = default;
		Rebuild();
	}

	private void ToggleTimeLimit()
	{
		BingoGameConfig config = GetGameConfig();
		config.TimeLimitEnabled = !config.TimeLimitEnabled;
		_failure = default;
		config.SaveChanges();
		Rebuild();
	}

	private void SetTimeLimitMinutes(int value)
	{
		GetGameConfig().TimeLimitMinutes = value;
		if (_failure.Error == BingoValidationError.InvalidTimeLimit)
			_failure = default;
	}

	private void SetTimeLimitSeconds(int value)
	{
		GetGameConfig().TimeLimitSeconds = value;
		if (_failure.Error == BingoValidationError.InvalidTimeLimit)
			_failure = default;
	}

	private void GiveInitialItems()
	{
		BingoGameConfig config = GetGameConfig();
		BingoInitialItemListEntry enabled = config.InitialItemLists.FirstOrDefault(entry => entry.Enabled);
		if (enabled == null)
		{
			_inventoryActionError = Text("UI.InitialItemErrors.NoEnabledList");
			Rebuild();
			return;
		}
		_inventoryActionError = string.Empty;
		Rebuild();
		BingoGame.RequestGiveInitialItems(enabled.Items);
	}

	private void ClearPlayerItems()
	{
		_inventoryActionError = string.Empty;
		Rebuild();
		BingoGame.RequestClearPlayerItems();
	}

	private void StartGame()
	{
		if (!TrySavePersistentState(true))
			return;
		if (!ValidateDraft(out BingoValidationFailure failure))
		{
			SetValidationFailure(failure);
			return;
		}
		BingoGameConfig config = GetGameConfig();
		if (config.TimeLimitEnabled
			&& (config.TimeLimitMinutes < 0 || config.TimeLimitSeconds is < 0 or > 59
				|| config.TimeLimitMinutes == 0 && config.TimeLimitSeconds == 0))
		{
			SetValidationFailure(new BingoValidationFailure(BingoValidationError.InvalidTimeLimit, -1));
			return;
		}
		if (!TryBuildWhitelistPool(config, out int[] whitelistTypes, out BingoValidationFailure whitelistFailure))
		{
			SetValidationFailure(whitelistFailure);
			return;
		}
		_failure = default;
		BingoNumericInput.ClearFocus();
		BingoInitialItemListEntry enabledInitialItems = config.InitialItemLists.FirstOrDefault(entry => entry.Enabled);
		int[] initialItemTypes = enabledInitialItems?.Items.Select(item => item.ItemType).ToArray()
			?? Array.Empty<int>();
		BingoGame.RequestStart(_draftSize, _draftRule, (int[])_draftItems.Clone(), config.WhitelistEnabled,
			whitelistTypes, initialItemTypes, config.TimeLimitEnabled, config.TimeLimitMinutes,
			config.TimeLimitSeconds);
	}
}
