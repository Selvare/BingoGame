using System;
using System.Linq;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed class BingoNumericInput : UIElement
{
	private static BingoNumericInput _focused;
	private readonly string _hint;
	private readonly Func<float> _layoutScale;
	private readonly Action<int> _changed;
	private string _text;

	public static bool AnyFocused => _focused != null;
	public bool IsInvalid { get; set; }

	public BingoNumericInput(string hint, int value, Func<float> layoutScale, Action<int> changed)
	{
		_hint = hint;
		_layoutScale = layoutScale;
		_changed = changed;
		_text = value > 0 ? value.ToString() : string.Empty;
		OnLeftClick += (_, _) => Focus();
	}

	public static void ClearFocus()
	{
		if (Main.CurrentInputTextTakerOverride == _focused)
			Main.CurrentInputTextTakerOverride = null;
		_focused = null;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			if (int.TryParse(_text, out int itemType) && ContentSamples.ItemsByType.ContainsKey(itemType))
				Main.hoverItemName = $"{Lang.GetItemNameValue(itemType)} ({itemType})";
		}
		if (_focused == this && Main.mouseLeft && !ContainsPoint(Main.MouseScreen))
			ClearFocus();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle bounds = dimensions.ToRectangle();
		bool validItem = int.TryParse(_text, out int itemType) && BingoWorldSystem.IsUsableItemId(itemType);
		bool invalidItem = _text.Length > 0 && !validItem;
		Color border = IsInvalid || invalidItem
			? Color.OrangeRed
			: _focused == this ? new Color(130, 210, 255) : new Color(89, 116, 213);
		border = BingoUITheme.WithFullOpacity(border);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, BingoUITheme.CellBackground);
		DrawBorder(spriteBatch, bounds, border);

		if (_focused == this)
		{
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			Main.CurrentInputTextTakerOverride = this;
			string input = Main.GetInputText(_text, false);
			if (Main.inputTextEscape || Main.inputTextEnter)
			{
				Main.inputTextEscape = false;
				Main.inputTextEnter = false;
				ClearFocus();
			}
			string filtered = new(input.Where(char.IsDigit).Take(10).ToArray());
			if (filtered != _text)
			{
				_text = filtered;
				_changed(string.IsNullOrEmpty(_text) ? 0 : int.TryParse(_text, out int value) ? value : -1);
			}
		}
		else if (validItem)
		{
			BingoItemIconRenderer.Draw(spriteBatch, bounds, itemType, 8f);
			return;
		}

		string display = _text.Length == 0 ? _hint : _text;
		Color color = _text.Length == 0 ? Color.Gray : Color.White;
		float scale = BingoAdaptiveText.CalculateScale(display, bounds.Width - 4f, bounds.Height - 4f, 0.82f,
			BingoTextRole.Compact, _layoutScale?.Invoke() ?? 1f);
		Vector2 center = bounds.Center.ToVector2();
		Utils.DrawBorderString(spriteBatch, display, center, color, scale, 0.5f, 0.5f);
	}

	private void Focus()
	{
		BingoTextInput.ClearFocus();
		_focused = this;
		Main.CurrentInputTextTakerOverride = this;
		Main.clrInput();
	}

	private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
	{
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 2), color);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rectangle.X, rectangle.Bottom - 2, rectangle.Width, 2), color);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rectangle.X, rectangle.Y, 2, rectangle.Height), color);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rectangle.Right - 2, rectangle.Y, 2, rectangle.Height), color);
	}
}

