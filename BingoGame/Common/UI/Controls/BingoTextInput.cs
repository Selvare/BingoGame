using System;
using System.Linq;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed class BingoTextInput : UIPanel
{
	private static BingoTextInput _focused;
	private readonly string _hint;
	private readonly Func<float> _layoutScale;
	private readonly Action<string> _changed;
	private readonly Action<string> _committed;
	private readonly int _maxLength;
	private readonly Func<char, bool> _allowedCharacter;
	private string _text;

	public static bool AnyFocused => _focused != null;
	public bool IsInvalid { get; set; }

	public BingoTextInput(string hint, string value, Func<float> layoutScale, Action<string> changed,
		Action<string> committed, int maxLength, Func<char, bool> allowedCharacter = null)
	{
		_hint = hint ?? string.Empty;
		_text = value ?? string.Empty;
		_layoutScale = layoutScale;
		_changed = changed;
		_committed = committed;
		_maxLength = Math.Max(1, maxLength);
		_allowedCharacter = allowedCharacter;
		SetPadding(0f);
		OverflowHidden = true;
		OnLeftClick += (_, _) => Focus();
	}

	public static void ClearFocus(bool commit = true)
	{
		BingoTextInput focused = _focused;
		if (Main.CurrentInputTextTakerOverride == focused)
			Main.CurrentInputTextTakerOverride = null;
		_focused = null;
		if (commit)
			focused?._committed?.Invoke(focused._text);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
		if (_focused == this && Main.mouseLeft && !ContainsPoint(Main.MouseScreen))
			ClearFocus();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle bounds = GetDimensions().ToRectangle();
		BingoUITheme.ApplyInput(this, IsInvalid, _focused == this);
		base.DrawSelf(spriteBatch);

		if (_focused == this)
		{
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			Main.CurrentInputTextTakerOverride = this;
			string input = Main.GetInputText(_text, false);
			bool submit = Main.inputTextEnter;
			bool cancel = Main.inputTextEscape;
			Main.inputTextEnter = false;
			Main.inputTextEscape = false;
			string filtered = new(input.Where(character => !char.IsControl(character)
				&& (_allowedCharacter?.Invoke(character) ?? true)).Take(_maxLength).ToArray());
			if (filtered != _text)
			{
				_text = filtered;
				IsInvalid = false;
				_changed?.Invoke(_text);
			}
			if (submit || cancel)
				ClearFocus();
		}

		string display = _text.Length == 0 ? _hint : _text;
		if (display.Length > 80)
			display = "…" + display[^79..];
		Color color = _text.Length == 0 ? Color.Gray : Color.White;
		float scale = BingoAdaptiveText.CalculateScale(display, bounds.Width - 8f, bounds.Height - 4f, 0.82f,
			BingoTextRole.Compact, _layoutScale?.Invoke() ?? 1f);
		Utils.DrawBorderString(spriteBatch, display, bounds.Center.ToVector2(), color, scale, 0.5f, 0.5f);
	}

	private void Focus()
	{
		BingoNumericInput.ClearFocus();
		if (_focused != null && _focused != this)
			ClearFocus();
		_focused = this;
		Main.CurrentInputTextTakerOverride = this;
		Main.clrInput();
	}

}
