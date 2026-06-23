using System;
using System.Globalization;
using System.Linq;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using BingoGame.Common.UI;
using BingoGame.Common.UI.Components;
using BingoGame.Common.UI.Core;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Theme;
using BingoGame.Common.Tools;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.UI.Screens;

/// <summary>
/// Bingo 设置屏幕 - 初始配置界面
/// </summary>
internal sealed class BingoSettingsScreen : BingoBaseScreen
{
	protected override void BuildUI()
	{
		BingoResponsivePanel panel = CreateMainWindow("UI.Title", 560f, 665f, 620f, 705f);
		if (panel == null) return;

		UIVerticalStack root = CreateRootStack(10f, 8f);
		BingoGameConfig gameConfig = ModContent.GetInstance<BingoGameConfig>();
		BingoUiViewState state = Context.ViewState;

		// 标题
		BingoAdaptiveText title = CreateText(panel, GetText("UI.Title"), 0.5f, 0.5f, 1.25f, BingoTextRole.Title);

		// 棋盘大小行
		UIHorizontalStack sizeRow = new(12f);
		BingoAdaptiveText sizeLabel = CreateText(panel, GetText("UI.BoardSize"), 0f, 0.5f, 1f);
		UIHorizontalStack sizeControls = new(8f);
#pragma warning disable CS0618
		BingoButton decrease = new(() => ResizeDraft(state.DraftSize - 1), false, false);
		AddTextToButton(panel, decrease, "−");
		BingoAdaptiveText sizeValue = CreateText(panel, $"{state.DraftSize} × {state.DraftSize}", 0.5f, 0.5f, 1f);
		BingoButton increase = new(() => ResizeDraft(state.DraftSize + 1), false, false);
		AddTextToButton(panel, increase, "+");
#pragma warning restore CS0618
		sizeControls.AddFixed(decrease, 48f);
		sizeControls.AddWeighted(sizeValue, 1f, 70f);
		sizeControls.AddFixed(increase, 48f);
		sizeRow.AddWeighted(sizeLabel, 4f, 120f);
		sizeRow.AddWeighted(sizeControls, 6f, 180f);

		// 赢法则行
		UIHorizontalStack ruleRow = new(12f);
		BingoAdaptiveText ruleLabel = CreateText(panel, GetText("UI.WinRule"), 0f, 0.5f, 1f);
		UIHorizontalStack ruleControls = new(8f);
		BingoButton line = CreateButton(panel, GetText("UI.RuleLine"),
			() => SetRule(BingoWinRule.Line), state.DraftRule == BingoWinRule.Line);
		BingoButton majority = CreateButton(panel, GetText("UI.RuleMajority"),
			() => SetRule(BingoWinRule.Majority), state.DraftRule == BingoWinRule.Majority);
		ruleControls.AddWeighted(line);
		ruleControls.AddWeighted(majority);
		ruleRow.AddWeighted(ruleLabel, 4f, 120f);
		ruleRow.AddWeighted(ruleControls, 6f, 180f);

		// 页脚
		UIHorizontalStack footer = new(24f);
		BingoButton close = CreateButton(panel, GetText("UI.Close"), () => { /* Close action */ });
		BingoButton start = CreateButton(panel, GetText("UI.StartGame"), StartGame, true);
		footer.AddWeighted(close);
		footer.AddWeighted(start);

		root.AddFixed(title, 36f);
		root.AddFixed(sizeRow, 46f);
		root.AddFixed(ruleRow, 46f);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(footer, 48f);
		panel.Append(root);
		panel.Recalculate();
	}

	private BingoResponsivePanel CreateMainWindow(string titleKey, float minWidth, float minHeight, float defaultWidth = 0f, float defaultHeight = 0f)
	{
		Vector2 size = GetSavedSize(BingoWindowPage.Settings, minWidth, minHeight, defaultWidth, defaultHeight);
		Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight / 2f);
		
		BingoResponsivePanel panel = new(size.X, size.Y, minWidth, minHeight, defaultWidth > 0 ? defaultWidth : minWidth,
			defaultHeight > 0 ? defaultHeight : minHeight, 16f, center,
			pos => { }, (w, h) => SaveSize(BingoWindowPage.Settings, w, h));
		
		panel.BackgroundColor = BingoColorTokens.WindowBackground;
		panel.BorderColor = BingoColorTokens.WindowBorder;
		panel.OverflowHidden = true;
		UiState.Append(panel);
		return panel;
	}

	private Vector2 GetSavedSize(BingoWindowPage page, float minWidth, float minHeight, float defaultWidth, float defaultHeight)
	{
		if (defaultWidth <= 0) defaultWidth = minWidth;
		if (defaultHeight <= 0) defaultHeight = minHeight;
		
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		Vector2 saved = page switch
		{
			BingoWindowPage.Settings => new Vector2(config.SettingsWidth, config.SettingsHeight),
			_ => Vector2.Zero
		};
		
		if (saved == Vector2.Zero) return new Vector2(defaultWidth, defaultHeight);
		return saved;
	}

	private void SaveSize(BingoWindowPage page, float width, float height)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		int w = (int)MathF.Round(width);
		int h = (int)MathF.Round(height);
		
		if (page == BingoWindowPage.Settings)
		{
			config.SettingsWidth = w;
			config.SettingsHeight = h;
		}
		config.SaveChanges();
	}

	private UIVerticalStack CreateRootStack(float spacing, float padding)
	{
		UIVerticalStack root = new(spacing);
		root.Width.Set(0f, 1f);
		root.Height.Set(0f, 1f);
		root.SetPadding(padding);
		return root;
	}

	private BingoAdaptiveText CreateText(BingoResponsivePanel panel, string value, float hAlign, float vAlign, float scale, BingoTextRole role = BingoTextRole.Normal, Color? color = null)
	{
		BingoAdaptiveText text = new(value, scale, hAlign, vAlign, role, () => panel.LayoutScale);
		if (color.HasValue)
			text.TextColor = color.Value;
		return text;
	}

	private BingoButton CreateButton(BingoResponsivePanel panel, string label, Action action, bool selected = false)
	{
		// 使用旧的兼容构造函数
#pragma warning disable CS0618
		BingoButton button = new(action, selected, false);
#pragma warning restore CS0618
		BingoAdaptiveText text = CreateText(panel, label, 0.5f, 0.5f, 0.86f, BingoTextRole.Normal);
		text.Width.Set(0f, 1f);
		text.Height.Set(0f, 1f);
		button.Append(text);
		return button;
	}

	private void AddTextToButton(BingoResponsivePanel panel, BingoButton button, string label)
	{
		BingoAdaptiveText text = CreateText(panel, label, 0.5f, 0.5f, 0.86f, BingoTextRole.Normal);
		text.Width.Set(0f, 1f);
		text.Height.Set(0f, 1f);
		button.Append(text);
	}

	private void ResizeDraft(int newSize)
	{
		BingoUiViewState state = Context.ViewState;
		newSize = Math.Clamp(newSize, 2, 10);
		if (state.DraftSize == newSize) return;

		int oldSize = state.DraftSize;
		int oldCount = oldSize * oldSize;
		int newCount = newSize * newSize;

		int[] oldItems = state.DraftItems;
		int[] newItems = new int[newCount];
		
		// 保留可以转移的物品
		int transfer = Math.Min(oldCount, newCount);
		Array.Copy(oldItems, 0, newItems, 0, transfer);
		
		state.DraftSize = newSize;
		state.DraftItems = newItems;
		Rebuild();
	}

	private void SetRule(BingoWinRule rule)
	{
		Context.ViewState.DraftRule = rule;
		Rebuild();
	}

	private void StartGame()
	{
		// 验证草稿
		BingoUiViewState state = Context.ViewState;
		if (!ValidateDraft(state.DraftItems, out BingoValidationFailure failure))
		{
			Context.ViewState.ValidationFailure = failure;
			Rebuild();
			return;
		}

		// 调用启动游戏的操作
		Context.Actions.StartGame();
	}

	private bool ValidateDraft(int[] items, out BingoValidationFailure failure)
	{
		var seen = new System.Collections.Generic.HashSet<int>();
		for (int i = 0; i < items.Length; i++)
		{
			int itemType = items[i];
			if (itemType == 0) continue;

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

	private string GetText(string key, params object[] args) =>
		Terraria.Localization.Language.GetTextValue($"Mods.BingoGame.{key}", args);

	private void Rebuild()
	{
		// 重新构建屏幕
		BuildUI();
	}
}
