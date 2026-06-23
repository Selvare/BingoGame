using System;
using BingoGame.Common.UI.Layout;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace BingoGame.Common.UI.Components;

/// <summary>
/// 自适应文本控件，根据可用区域自动缩放文本大小
/// </summary>
internal sealed class BingoAdaptiveText : UIElement
{
	private string _text;
	private readonly float _baseScale;
	private readonly float _horizontalOrigin;
	private readonly float _verticalOrigin;
	private readonly BingoTextRole _role;
	private readonly Func<float> _layoutScale;

	public Color TextColor { get; set; } = Color.White;

	/// <summary>
	/// 创建自适应文本控件
	/// </summary>
	/// <param name="text">显示的文本</param>
	/// <param name="baseScale">基础缩放比例</param>
	/// <param name="horizontalOrigin">水平原点位置（0-1）</param>
	/// <param name="verticalOrigin">垂直原点位置（0-1）</param>
	/// <param name="role">文本角色</param>
	/// <param name="layoutScale">布局缩放回调</param>
	public BingoAdaptiveText(string text, float baseScale, float horizontalOrigin, float verticalOrigin,
		BingoTextRole role, Func<float> layoutScale)
	{
		_text = text ?? string.Empty;
		_baseScale = baseScale;
		_horizontalOrigin = Math.Clamp(horizontalOrigin, 0f, 1f);
		_verticalOrigin = Math.Clamp(verticalOrigin, 0f, 1f);
		_role = role;
		_layoutScale = layoutScale;
	}

	/// <summary>
	/// 更新文本内容
	/// </summary>
	public void SetText(string text) => _text = text ?? string.Empty;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetInnerDimensions();
		if (dimensions.Width <= 0f || dimensions.Height <= 0f || _text.Length == 0)
			return;

		float scale = CalculateScale(_text, dimensions.Width, dimensions.Height, _baseScale, _role,
			_layoutScale?.Invoke() ?? 1f);
		Vector2 position = new(dimensions.X + dimensions.Width * _horizontalOrigin,
			dimensions.Y + dimensions.Height * _verticalOrigin);
		Utils.DrawBorderString(spriteBatch, _text, position, TextColor, scale, _horizontalOrigin, _verticalOrigin);
	}

	/// <summary>
	/// 计算适应文本框的缩放比例
	/// </summary>
	internal static float CalculateScale(string text, float width, float height, float baseScale,
		BingoTextRole role, float layoutScale)
	{
		(float minimum, float maximum) = role switch
		{
			BingoTextRole.Title => (0.8f, 1.35f),
			BingoTextRole.Compact => (0.55f, 0.9f),
			_ => (0.65f, 1.1f)
		};

		float desired = Math.Clamp(baseScale * Math.Clamp(layoutScale, 0.75f, 1.35f), minimum, maximum);
		Vector2 measured = FontAssets.MouseText.Value.MeasureString(text);
		float fitWidth = Math.Max(1f, width - 6f) / Math.Max(1f, measured.X);
		float fitHeight = Math.Max(1f, height - 2f) / Math.Max(1f, measured.Y);
		float fitted = Math.Min(desired, Math.Min(fitWidth, fitHeight));
		return Math.Clamp(fitted, 0.4f, maximum);
	}
}
