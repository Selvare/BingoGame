using System;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private BingoResponsivePanel CreateWindow(BingoWindowPage page, float minimumWidth, float minimumHeight,
		float defaultWidth = 0f, float defaultHeight = 0f)
	{
		Vector2 savedSize = GetSavedSize(page);
		if (savedSize.X <= 0f)
			savedSize.X = defaultWidth > 0f ? defaultWidth : minimumWidth;
		if (savedSize.Y <= 0f)
			savedSize.Y = defaultHeight > 0f ? defaultHeight : minimumHeight;
		Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight / 2f);
		center += _windowCenterOffset;
		float referenceWidth = defaultWidth > 0f ? defaultWidth : minimumWidth;
		float referenceHeight = defaultHeight > 0f ? defaultHeight : minimumHeight;
		BingoResponsivePanel panel = new(savedSize.X, savedSize.Y, minimumWidth, minimumHeight,
			referenceWidth, referenceHeight, ScreenMargin, center,
			newCenter => _windowCenterOffset = newCenter - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f),
			(width, height) => SaveSize(page, width, height));
		BingoUITheme.Apply(panel);
		panel.OverflowHidden = true;
		Append(panel);
		return panel;
	}

	private static UIVerticalStack CreateRootStack(float spacing, float padding)
	{
		UIVerticalStack root = new(spacing);
		root.Width.Set(0f, 1f);
		root.Height.Set(0f, 1f);
		root.SetPadding(padding);
		return root;
	}

	private static Vector2 GetSavedSize(BingoWindowPage page)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		return page switch
		{
			BingoWindowPage.Settings => new Vector2(config.SettingsWidth, config.SettingsHeight),
			BingoWindowPage.AdvancedSettings => new Vector2(config.AdvancedSettingsWidth,
				config.AdvancedSettingsHeight),
			BingoWindowPage.Waiting => new Vector2(config.WaitingWidth, config.WaitingHeight),
			BingoWindowPage.Editor => new Vector2(config.EditorWidth, config.EditorHeight),
			BingoWindowPage.Game => new Vector2(config.GameWidth, config.GameHeight),
			BingoWindowPage.WhitelistList => new Vector2(config.WhitelistListWidth, config.WhitelistListHeight),
			BingoWindowPage.WhitelistEditor => new Vector2(config.WhitelistEditorWidth, config.WhitelistEditorHeight),
			BingoWindowPage.InitialItemList => new Vector2(config.InitialItemListWidth, config.InitialItemListHeight),
			BingoWindowPage.InitialItemEditor => new Vector2(config.InitialItemEditorWidth, config.InitialItemEditorHeight),
			_ => Vector2.Zero
		};
	}

	private static void SaveSize(BingoWindowPage page, float width, float height)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		int roundedWidth = (int)MathF.Round(width);
		int roundedHeight = (int)MathF.Round(height);
		switch (page)
		{
			case BingoWindowPage.Settings:
				config.SettingsWidth = roundedWidth;
				config.SettingsHeight = roundedHeight;
				break;
			case BingoWindowPage.AdvancedSettings:
				config.AdvancedSettingsWidth = roundedWidth;
				config.AdvancedSettingsHeight = roundedHeight;
				break;
			case BingoWindowPage.Waiting:
				config.WaitingWidth = roundedWidth;
				config.WaitingHeight = roundedHeight;
				break;
			case BingoWindowPage.Editor:
				config.EditorWidth = roundedWidth;
				config.EditorHeight = roundedHeight;
				break;
			case BingoWindowPage.Game:
				config.GameWidth = roundedWidth;
				config.GameHeight = roundedHeight;
				break;
			case BingoWindowPage.WhitelistList:
				config.WhitelistListWidth = roundedWidth;
				config.WhitelistListHeight = roundedHeight;
				break;
			case BingoWindowPage.WhitelistEditor:
				config.WhitelistEditorWidth = roundedWidth;
				config.WhitelistEditorHeight = roundedHeight;
				break;
			case BingoWindowPage.InitialItemList:
				config.InitialItemListWidth = roundedWidth;
				config.InitialItemListHeight = roundedHeight;
				break;
			case BingoWindowPage.InitialItemEditor:
				config.InitialItemEditorWidth = roundedWidth;
				config.InitialItemEditorHeight = roundedHeight;
				break;
		}
		config.SaveChanges();
	}

	private static BingoAdaptiveText CreateText(BingoResponsivePanel panel, string value, float hAlign, float vAlign,
		float scale, BingoTextRole role = BingoTextRole.Normal, Color? color = null)
	{
		BingoAdaptiveText text = new(value, scale, hAlign, vAlign, role, () => panel.LayoutScale);
		if (color.HasValue)
			text.TextColor = color.Value;
		return text;
	}

	private static BingoTextInput CreateDigitsInput(BingoResponsivePanel panel, string hint, int value,
		int maxLength, Action<int> changed)
	{
		return new BingoTextInput(hint, value > 0 ? value.ToString() : string.Empty, () => panel.LayoutScale,
			text => changed(string.IsNullOrEmpty(text) ? 0 : int.TryParse(text, out int parsed) ? parsed : -1),
			null, maxLength, char.IsDigit);
	}

	private static BingoButton CreateButton(BingoResponsivePanel panel, string label, Action action,
		bool selected = false, bool emphasized = false, BingoTextRole textRole = BingoTextRole.Normal,
		Color? backgroundColor = null, bool enabled = true)
	{
		BingoButton button = new(action, selected, emphasized, backgroundColor, enabled);
		BingoAdaptiveText text = CreateText(panel, label, 0.5f, 0.5f, 0.86f, textRole);
		text.Width.Set(0f, 1f);
		text.Height.Set(0f, 1f);
		button.Append(text);
		return button;
	}

	private static string Text(string key, params object[] args) =>
		Language.GetTextValue($"Mods.BingoGame.{key}", args);
}
