using System;
using System.Linq;
using BingoGame.Common.Systems;
using BingoGame.Common.Tools;
using BingoGame.Common.UI.Components;
using BingoGame.Common.UI.Layout;
using BingoGame.Common.UI.Theme;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace BingoGame.Common.UI.Inputs;

/// <summary>
/// 数字输入框，只接受数字输入
/// </summary>
internal sealed class BingoNumericInput : UIPanel
{
	private static BingoNumericInput _focused;
	private readonly string _hint;
	private readonly Func<float> _layoutScale;
	private readonly Action<int> _changed;
	private string _text;

	public static bool AnyFocused => _focused != null;
	public bool IsInvalid { get; set; }

	/// <summary>
	/// 创建数字输入框
	/// </summary>
	public BingoNumericInput(string hint, int value, Func<float> layoutScale, Action<int> changed)
	{
		_hint = hint;
		_layoutScale = layoutScale;
		_changed = changed;
		_text = value > 0 ? value.ToString() : string.Empty;
		SetPadding(0f);
		OverflowHidden = true;
		OnLeftClick += (_, _) => Focus();
	}

	/// <summary>
	/// 清除焦点
	/// </summary>
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
		BingoTheme.ApplyInput(this, IsInvalid || invalidItem, _focused == this);
		base.DrawSelf(spriteBatch);

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

			if (bounds.Contains(Main.MouseScreen.ToPoint()))
			{
				string title = $"{Lang.GetItemNameValue(itemType)} ({itemType})";
				string body = "";
				BingoHoverTooltipGlobalItem.Show(new Item(itemType), title, body);
			}
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
		// 清除文本输入框焦点
		if (System.Type.GetType("BingoGame.Common.UI.Inputs.BingoTextInput", false) is System.Type textInputType)
		{
			var clearFocusMethod = textInputType.GetMethod("ClearFocus", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			clearFocusMethod?.Invoke(null, new object[] { false });
		}

		_focused = this;
		Main.CurrentInputTextTakerOverride = this;
		Main.clrInput();
	}
}
