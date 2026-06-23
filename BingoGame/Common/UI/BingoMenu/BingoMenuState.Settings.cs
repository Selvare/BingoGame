using System;
using System.Globalization;
using System.Linq;
using BingoGame.Common.Configs;
using BingoGame.Common.Tools;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
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
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Settings, 560f, 665f, 620f, 705f);
		UIVerticalStack root = CreateRootStack(10f, 8f);
		BingoGameConfig gameConfig = ModContent.GetInstance<BingoGameConfig>();
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

		UIPanel lineRuleOptions = CreateSubOptionPanel();
		if (_draftRule == BingoWinRule.Line)
		{
			UIVerticalStack lineRuleOptionList = new(6f);
			lineRuleOptionList.Width.Set(0f, 1f);
			lineRuleOptionList.Height.Set(0f, 1f);
			lineRuleOptionList.AddFixed(CreateSubToggleRow(panel, Text("UI.LineProgressTiebreak"),
				Text("UI.AdvancedTooltips.LineProgressTiebreak"), gameConfig.LineProgressTiebreakEnabled,
				ToggleLineProgressTiebreak), 50f);
			lineRuleOptionList.AddFixed(CreateSubToggleRow(panel, Text("UI.LineAutoDegrade"),
				Text("UI.AdvancedTooltips.LineAutoDegrade"), gameConfig.LineAutoDegradeEnabled,
				ToggleLineAutoDegrade), 50f);
			lineRuleOptions.Append(lineRuleOptionList);
		}

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
		BingoButton advanced = CreateButton(panel, Text("UI.AdvancedOptions"), OpenAdvancedSettings);
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
		if (_draftRule == BingoWinRule.Line)
			root.AddFixed(lineRuleOptions, 122f);
		root.AddFixed(timeLimitRow, 46f);
		root.AddFixed(whitelistRow, 46f);
		root.AddFixed(configure, 46f);
		root.AddFixed(inventoryActions, 46f);
		root.AddFixed(advanced, 46f);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(hint, 22f);
		if (failureValue.Length > 0)
			root.AddFixed(failure, 22f);
		root.AddFixed(footer, 48f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildAdvancedSettings()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.AdvancedSettings, 560f, 650f, 620f, 690f);
		UIVerticalStack root = CreateRootStack(10f, 8f);
		BingoGameConfig config = GetGameConfig();
		BingoAdaptiveText title = CreateText(panel, Text("UI.AdvancedOptions"), 0.5f, 0.5f, 1.2f,
			BingoTextRole.Title);

		UIVerticalStack killStealGroup = CreateOptionGroup(panel, Text("UI.KillSteal"),
			Text("UI.AdvancedTooltips.KillSteal"), config.KillStealEnabled, ToggleKillSteal);
		if (config.KillStealEnabled)
			killStealGroup.AddFixed(CreateChanceSubOption(panel, config), 74f);

		UIVerticalStack randomStartGroup = CreateOptionGroup(panel, Text("UI.RandomStart"),
			Text("UI.AdvancedTooltips.RandomStart"), config.RandomStartEnabled, ToggleRandomStart);
		if (config.RandomStartEnabled)
			randomStartGroup.AddFixed(CreateSubToggle(panel, Text("UI.RandomStartTeamTogether"),
				Text("UI.AdvancedTooltips.RandomStartTeamTogether"), config.RandomStartTeamTogether,
				ToggleRandomStartTeamTogether), 58f);

		UIVerticalStack forcePvpGroup = CreateOptionGroup(panel, Text("UI.ForcePvp"),
			Text("UI.AdvancedTooltips.ForcePvp"), config.ForcePvpEnabled, ToggleForcePvp);
		if (config.ForcePvpEnabled)
			forcePvpGroup.AddFixed(CreateSubToggle(panel, Text("UI.NoRetreat"),
				Text("UI.AdvancedTooltips.NoRetreat"), config.NoRetreatEnabled,
				ToggleNoRetreat), 58f);
		UIHorizontalStack fogOfWarRow = CreateToggleRow(panel, Text("UI.FogOfWar"), config.FogOfWarEnabled,
			ToggleFogOfWar, Text("UI.AdvancedTooltips.FogOfWar"));
		UIVerticalStack preparationGroup = CreateOptionGroup(panel, Text("UI.Preparation"),
			Text("UI.AdvancedTooltips.Preparation"), config.PreparationEnabled, TogglePreparation);
		if (config.PreparationEnabled)
			preparationGroup.AddFixed(CreatePreparationSubOption(panel, config), 74f);

		UIHorizontalStack footer = new(12f);
		BingoButton back = CreateButton(panel, Text("UI.Back"), CloseAdvancedSettings);
		footer.AddWeighted(back);

		root.AddFixed(title, 36f);
		root.AddFixed(killStealGroup, config.KillStealEnabled ? 132f : 52f);
		root.AddFixed(randomStartGroup, config.RandomStartEnabled ? 116f : 52f);
		root.AddFixed(forcePvpGroup, config.ForcePvpEnabled ? 116f : 52f);
		root.AddFixed(fogOfWarRow, 46f);
		root.AddFixed(preparationGroup, config.PreparationEnabled ? 132f : 52f);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(footer, 46f);
		panel.Append(root);
		panel.Recalculate();
	}

	private UIVerticalStack CreateOptionGroup(BingoResponsivePanel panel, string label, string tooltip, bool enabled,
		Action toggle)
	{
		UIVerticalStack group = new(8f);
		group.AddFixed(CreateToggleRow(panel, label, enabled, toggle, tooltip), 46f);
		return group;
	}

	private UIHorizontalStack CreateToggleRow(BingoResponsivePanel panel, string label, bool enabled, Action toggle,
		string tooltip)
	{
		UIHorizontalStack row = new(12f);
		UIHorizontalStack labelGroup = CreateLabelWithHelp(panel, label, tooltip);
		BingoButton button = CreateButton(panel, Text(enabled ? "UI.Enabled" : "UI.Disabled"), toggle, enabled,
			backgroundColor: enabled ? BingoUITheme.SuccessBackground : null);
		row.AddWeighted(labelGroup, 4f, 150f);
		row.AddWeighted(button, 6f, 180f);
		return row;
	}

	private UIPanel CreateChanceSubOption(BingoResponsivePanel panel, BingoGameConfig config)
	{
		UIPanel subPanel = CreateSubOptionPanel();
		UIVerticalStack stack = new(6f);
		stack.Width.Set(0f, 1f);
		stack.Height.Set(0f, 1f);
		UIHorizontalStack controls = new(8f);
		BingoTextInput input = null;
		BingoFloatSlider slider = new(config.KillStealChance, value =>
		{
			SetKillStealChance(value);
			input?.SetText(FormatChance(value));
		});
		input = CreateChanceInput(panel, config.KillStealChance, value =>
		{
			SetKillStealChance(value);
			slider.SetValue(value);
		});
		controls.AddWeighted(slider, 7f, 180f);
		controls.AddWeighted(input, 3f, 70f);
		stack.AddFixed(CreateLabelWithHelp(panel, Text("UI.KillStealChance"),
			Text("UI.AdvancedTooltips.KillStealChance")), 24f);
		stack.AddFixed(controls, 34f);
		subPanel.Append(stack);
		return subPanel;
	}

	private UIPanel CreatePreparationSubOption(BingoResponsivePanel panel, BingoGameConfig config)
	{
		UIPanel subPanel = CreateSubOptionPanel();
		UIVerticalStack stack = new(6f);
		stack.Width.Set(0f, 1f);
		stack.Height.Set(0f, 1f);
		UIHorizontalStack controls = new(8f);
		BingoTextInput input = null;
		BingoFloatSlider slider = new(config.PreparationSeconds, value =>
		{
			int seconds = Math.Clamp((int)MathF.Round(value), 5, 60);
			SetPreparationSeconds(seconds);
			input?.SetText(seconds.ToString(CultureInfo.InvariantCulture));
		}, 5f, 60f);
		input = CreatePreparationSecondsInput(panel, config.PreparationSeconds, value =>
		{
			SetPreparationSeconds(value);
			slider.SetValue(value);
		});
		controls.AddWeighted(slider, 7f, 180f);
		controls.AddWeighted(input, 3f, 70f);
		stack.AddFixed(CreateLabelWithHelp(panel, Text("UI.PreparationSeconds"),
			Text("UI.AdvancedTooltips.PreparationSeconds")), 24f);
		stack.AddFixed(controls, 34f);
		subPanel.Append(stack);
		return subPanel;
	}

	private UIPanel CreateSubToggle(BingoResponsivePanel panel, string label, string tooltip, bool enabled,
		Action toggle)
	{
		UIPanel subPanel = CreateSubOptionPanel();
		subPanel.Append(CreateSubToggleRow(panel, label, tooltip, enabled, toggle));
		return subPanel;
	}

	private UIHorizontalStack CreateSubToggleRow(BingoResponsivePanel panel, string label, string tooltip,
		bool enabled, Action toggle)
	{
		UIHorizontalStack row = new(10f);
		row.Width.Set(0f, 1f);
		row.Height.Set(0f, 1f);
		row.AddWeighted(CreateLabelWithHelp(panel, label, tooltip), 5f, 160f);
		row.AddWeighted(CreateButton(panel, Text(enabled ? "UI.Enabled" : "UI.Disabled"), toggle, enabled,
			backgroundColor: enabled ? BingoUITheme.SuccessBackground : null), 4f, 110f);
		return row;
	}

	private static UIPanel CreateSubOptionPanel()
	{
		UIPanel panel = new();
		panel.SetPadding(8f);
		panel.BackgroundColor = BingoUITheme.WithOpacity(new Color(38, 52, 96));
		panel.BorderColor = BingoUITheme.WithFullOpacity(new Color(116, 150, 230));
		return panel;
	}

	private UIHorizontalStack CreateLabelWithHelp(BingoResponsivePanel panel, string label, string tooltip)
	{
		UIHorizontalStack group = new(6f);
		group.AddWeighted(CreateText(panel, label, 0f, 0.5f, 1f), 1f, 90f);
		group.AddFixed(new BingoHelpBox(tooltip, () => panel.LayoutScale), 28f);
		return group;
	}

	private static BingoTextInput CreateChanceInput(BingoResponsivePanel panel, float value, Action<float> changed)
	{
		string text = FormatChance(value);
		return new BingoTextInput(Text("UI.ChanceHint"), text, () => panel.LayoutScale,
			input =>
			{
				if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
					changed(Math.Clamp(parsed, 0f, 1f));
			}, null, 5, character => char.IsDigit(character) || character == '.');
	}

	private static BingoTextInput CreatePreparationSecondsInput(BingoResponsivePanel panel, int value,
		Action<int> changed)
	{
		string text = Math.Clamp(value, 5, 60).ToString(CultureInfo.InvariantCulture);
		return new BingoTextInput(Text("UI.Seconds"), text, () => panel.LayoutScale,
			input =>
			{
				if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
					changed(Math.Clamp(parsed, 5, 60));
			}, null, 2, char.IsDigit);
	}

	private static string FormatChance(float value) =>
		Math.Clamp(value, 0f, 1f).ToString("0.##", CultureInfo.InvariantCulture);

	private sealed class BingoHelpBox : UIPanel
	{
		private readonly string _tooltip;
		private readonly Func<float> _layoutScale;
		private readonly Item _hoverItem = new();

		public BingoHelpBox(string tooltip, Func<float> layoutScale)
		{
			_tooltip = tooltip ?? string.Empty;
			_layoutScale = layoutScale;
			_hoverItem.SetDefaults(ItemID.Sign);
			_hoverItem.stack = 1;
			SetPadding(0f);
			OverflowHidden = true;
			BackgroundColor = BingoUITheme.WithOpacity(BingoUITheme.InputBackground);
			BorderColor = BingoUITheme.WithFullOpacity(BingoUITheme.InputBorder);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (!IsMouseHovering)
				return;
			Main.LocalPlayer.mouseInterface = true;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			BackgroundColor = BingoUITheme.WithOpacity(IsMouseHovering
				? Color.Lerp(BingoUITheme.InputBackground, Color.White, 0.16f)
				: BingoUITheme.InputBackground);
			BorderColor = BingoUITheme.WithFullOpacity(IsMouseHovering
				? BingoUITheme.InputFocusedBorder
				: BingoUITheme.InputBorder);
			base.DrawSelf(spriteBatch);
			Rectangle bounds = GetDimensions().ToRectangle();
			float scale = BingoAdaptiveText.CalculateScale("?", bounds.Width - 4f, bounds.Height - 4f, 0.9f,
				BingoTextRole.Compact, _layoutScale?.Invoke() ?? 1f);
			Utils.DrawBorderString(spriteBatch, "?", bounds.Center.ToVector2(), Color.White, scale, 0.5f, 0.5f);
			if (bounds.Contains(Main.MouseScreen.ToPoint()) && _tooltip.Length > 0)
				BingoHoverTooltipGlobalItem.Show(_hoverItem, Text("UI.AdvancedTooltipTitle"), _tooltip,
					Color.LightGray);
		}
	}

	private sealed class BingoFloatSlider : UIPanel
	{
		private readonly Action<float> _changed;
		private readonly float _min;
		private readonly float _max;
		private bool _dragging;
		private float _value;

		public BingoFloatSlider(float value, Action<float> changed, float min = 0f, float max = 1f)
		{
			_changed = changed;
			_min = min;
			_max = Math.Max(min, max);
			_value = Math.Clamp(value, _min, _max);
			SetPadding(0f);
			OverflowHidden = true;
			OnLeftMouseDown += (_, _) => BeginDrag();
			OnLeftMouseUp += (_, _) => _dragging = false;
		}

		public void SetValue(float value)
		{
			_value = Math.Clamp(value, _min, _max);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (IsMouseHovering)
				Main.LocalPlayer.mouseInterface = true;
			if (_dragging)
			{
				if (!Main.mouseLeft)
				{
					_dragging = false;
					return;
				}
				SetFromMouse();
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			BingoUITheme.ApplyInput(this, false, _dragging || IsMouseHovering);
			base.DrawSelf(spriteBatch);
			Rectangle bounds = GetDimensions().ToRectangle();
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Rectangle track = new(bounds.X + 8, bounds.Center.Y - 2, Math.Max(1, bounds.Width - 16), 4);
			spriteBatch.Draw(pixel, track, new Color(30, 35, 55, 220));
			float ratio = (_value - _min) / Math.Max(0.001f, _max - _min);
			Rectangle fill = new(track.X, track.Y, (int)MathF.Round(track.Width * ratio), track.Height);
			spriteBatch.Draw(pixel, fill, new Color(130, 210, 255, 240));
			int knobX = track.X + (int)MathF.Round(track.Width * ratio);
			Rectangle knob = new(knobX - 4, bounds.Center.Y - 9, 8, 18);
			spriteBatch.Draw(pixel, knob, Color.White);
		}

		private void BeginDrag()
		{
			BingoTextInput.ClearFocus();
			BingoNumericInput.ClearFocus();
			_dragging = true;
			SetFromMouse();
		}

		private void SetFromMouse()
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			float ratio = Math.Clamp((Main.mouseX - (bounds.X + 8f)) / Math.Max(1f, bounds.Width - 16f), 0f, 1f);
			float next = _min + ratio * (_max - _min);
			if (Math.Abs(next - _value) < 0.001f)
				return;
			_value = next;
			_changed(_value);
		}
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

	private void OpenAdvancedSettings()
	{
		BingoTextInput.ClearFocus();
		_editingAdvancedSettings = true;
		Rebuild();
	}

	private void CloseAdvancedSettings()
	{
		BingoTextInput.ClearFocus();
		GetGameConfig().SaveChanges();
		_editingAdvancedSettings = false;
		Rebuild();
	}

	private void ToggleKillSteal() => ToggleAdvancedFlag(config => config.KillStealEnabled = !config.KillStealEnabled);

	private void ToggleRandomStart() => ToggleAdvancedFlag(config => config.RandomStartEnabled = !config.RandomStartEnabled);

	private void ToggleRandomStartTeamTogether()
		=> ToggleAdvancedFlag(config => config.RandomStartTeamTogether = !config.RandomStartTeamTogether);

	private void ToggleForcePvp() => ToggleAdvancedFlag(config => config.ForcePvpEnabled = !config.ForcePvpEnabled);

	private void ToggleNoRetreat() => ToggleAdvancedFlag(config => config.NoRetreatEnabled = !config.NoRetreatEnabled);

	private void ToggleFogOfWar() => ToggleAdvancedFlag(config => config.FogOfWarEnabled = !config.FogOfWarEnabled);

	private void TogglePreparation()
		=> ToggleAdvancedFlag(config => config.PreparationEnabled = !config.PreparationEnabled);

	private void ToggleLineProgressTiebreak()
		=> ToggleAdvancedFlag(config => config.LineProgressTiebreakEnabled = !config.LineProgressTiebreakEnabled);

	private void ToggleLineAutoDegrade()
		=> ToggleAdvancedFlag(config => config.LineAutoDegradeEnabled = !config.LineAutoDegradeEnabled);

	private void ToggleAdvancedFlag(Action<BingoGameConfig> toggle)
	{
		BingoTextInput.ClearFocus();
		BingoGameConfig config = GetGameConfig();
		toggle(config);
		config.SaveChanges();
		Rebuild();
	}

	private void SetKillStealChance(float value)
	{
		GetGameConfig().KillStealChance = Math.Clamp(value, 0f, 1f);
	}

	private void SetPreparationSeconds(int value)
	{
		GetGameConfig().PreparationSeconds = Math.Clamp(value, 5, 60);
		if (_failure.Error == BingoValidationError.InvalidAdvancedOptions)
			_failure = default;
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
		if (config.PreparationEnabled && config.PreparationSeconds is (< 5 or > 60))
		{
			SetValidationFailure(new BingoValidationFailure(BingoValidationError.InvalidAdvancedOptions, -1));
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
			config.TimeLimitSeconds, config.LineProgressTiebreakEnabled, config.LineAutoDegradeEnabled,
			config.KillStealEnabled, config.KillStealChance, config.RandomStartEnabled,
			config.RandomStartTeamTogether, config.ForcePvpEnabled, config.NoRetreatEnabled,
			config.FogOfWarEnabled, config.PreparationEnabled, config.PreparationSeconds);
	}
}
