using System;
using BingoGame.Common.UI.Theme;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BingoGame.Common.UI.Components;

internal enum BingoResizeEdge
{
	None = 0,
	Left = 1,
	Right = 2,
	Top = 4,
	Bottom = 8
}

/// <summary>
/// 响应式面板，支持拖拽和缩放，自适应屏幕大小
/// </summary>
internal sealed class BingoResponsivePanel : UIPanel
{
	private const float HeaderHeight = 46f;
	private const float ResizeBorder = 6f;
	private const float ResizeCorner = 12f;

	private readonly float _minimumWidth;
	private readonly float _minimumHeight;
	private readonly float _referenceWidth;
	private readonly float _referenceHeight;
	private readonly float _screenMargin;
	private readonly Action<Vector2> _positionChanged;
	private readonly Action<float, float> _resizeCompleted;
	private Vector2 _interactionStart;
	private Rectangle _startBounds;
	private BingoResizeEdge _resizeEdge;
	private bool _dragging;

	/// <summary>
	/// 窗口是否被锁定（禁止拖拽和缩放）
	/// </summary>
	public bool Locked { get; set; }

	/// <summary>
	/// 在header中排除拖拽的UI元素
	/// </summary>
	public UIElement DragExclusion { get; set; }

	public float PanelWidth => Width.Pixels;
	public float PanelHeight => Height.Pixels;

	/// <summary>
	/// 布局缩放因子，用于响应式UI缩放
	/// </summary>
	public float LayoutScale => Math.Clamp(Math.Min(PanelWidth / _referenceWidth, PanelHeight / _referenceHeight),
		0.75f, 1.35f);

	public BingoResponsivePanel(float width, float height, float minimumWidth, float minimumHeight,
		float referenceWidth, float referenceHeight, float screenMargin, Vector2 center,
		Action<Vector2> positionChanged, Action<float, float> resizeCompleted)
	{
		_minimumWidth = minimumWidth;
		_minimumHeight = minimumHeight;
		_referenceWidth = Math.Max(1f, referenceWidth);
		_referenceHeight = Math.Max(1f, referenceHeight);
		_screenMargin = screenMargin;
		_positionChanged = positionChanged;
		_resizeCompleted = resizeCompleted;
		OnLeftMouseDown += BeginInteraction;
		OnLeftMouseUp += (_, _) => EndInteraction();
		ApplyBounds(center.X - width / 2f, center.Y - height / 2f, width, height, false);
	}

	public override void Update(GameTime gameTime)
	{
		BackgroundColor = BingoTheme.WithOpacity(BackgroundColor);
		BorderColor = BingoTheme.WithFullOpacity(BorderColor);
		base.Update(gameTime);
		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		if (!_dragging && _resizeEdge == BingoResizeEdge.None)
			return;
		if (!Main.mouseLeft || Locked)
		{
			EndInteraction();
			return;
		}

		Vector2 delta = Main.MouseScreen - _interactionStart;
		if (_dragging)
		{
			ApplyBounds(_startBounds.X + delta.X, _startBounds.Y + delta.Y,
				_startBounds.Width, _startBounds.Height, true);
			return;
		}

		float left = _startBounds.Left;
		float top = _startBounds.Top;
		float right = _startBounds.Right;
		float bottom = _startBounds.Bottom;
		if ((_resizeEdge & BingoResizeEdge.Left) != 0)
			left += delta.X;
		if ((_resizeEdge & BingoResizeEdge.Right) != 0)
			right += delta.X;
		if ((_resizeEdge & BingoResizeEdge.Top) != 0)
			top += delta.Y;
		if ((_resizeEdge & BingoResizeEdge.Bottom) != 0)
			bottom += delta.Y;

		float minimumWidth = Math.Min(_minimumWidth, Main.screenWidth - _screenMargin * 2f);
		float minimumHeight = Math.Min(_minimumHeight, Main.screenHeight - _screenMargin * 2f);
		if ((_resizeEdge & BingoResizeEdge.Left) != 0)
			left = Math.Min(left, right - minimumWidth);
		else
			right = Math.Max(right, left + minimumWidth);
		if ((_resizeEdge & BingoResizeEdge.Top) != 0)
			top = Math.Min(top, bottom - minimumHeight);
		else
			bottom = Math.Max(bottom, top + minimumHeight);
		ApplyBounds(left, top, right - left, bottom - top, true);
	}

	private void BeginInteraction(UIMouseEvent evt, UIElement listeningElement)
	{
		if (Locked)
			return;
		CalculatedStyle dimensions = GetDimensions();
		Vector2 mouse = evt.MousePosition;
		_resizeEdge = GetResizeEdge(mouse, dimensions);
		if (_resizeEdge == BingoResizeEdge.None)
		{
			bool inHeader = mouse.Y <= dimensions.Y + HeaderHeight;
			bool excluded = DragExclusion?.ContainsPoint(mouse) == true;
			if (!inHeader || excluded)
				return;
			_dragging = true;
		}
		_interactionStart = mouse;
		_startBounds = new Rectangle((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height);
	}

	private BingoResizeEdge GetResizeEdge(Vector2 mouse, CalculatedStyle dimensions)
	{
		BingoResizeEdge edge = BingoResizeEdge.None;
		bool nearHorizontalCorner = mouse.Y <= dimensions.Y + ResizeCorner || mouse.Y >= dimensions.Y + dimensions.Height - ResizeCorner;
		bool nearVerticalCorner = mouse.X <= dimensions.X + ResizeCorner || mouse.X >= dimensions.X + dimensions.Width - ResizeCorner;
		float horizontalRange = nearHorizontalCorner ? ResizeCorner : ResizeBorder;
		float verticalRange = nearVerticalCorner ? ResizeCorner : ResizeBorder;
		if (mouse.X <= dimensions.X + horizontalRange)
			edge |= BingoResizeEdge.Left;
		else if (mouse.X >= dimensions.X + dimensions.Width - horizontalRange)
			edge |= BingoResizeEdge.Right;
		if (mouse.Y <= dimensions.Y + verticalRange)
			edge |= BingoResizeEdge.Top;
		else if (mouse.Y >= dimensions.Y + dimensions.Height - verticalRange)
			edge |= BingoResizeEdge.Bottom;
		return edge;
	}

	private void ApplyBounds(float left, float top, float width, float height, bool notifyPosition)
	{
		float maximumWidth = Math.Max(1f, Main.screenWidth - _screenMargin * 2f);
		float maximumHeight = Math.Max(1f, Main.screenHeight - _screenMargin * 2f);
		float minimumWidth = Math.Min(_minimumWidth, maximumWidth);
		float minimumHeight = Math.Min(_minimumHeight, maximumHeight);
		width = Math.Clamp(width, minimumWidth, maximumWidth);
		height = Math.Clamp(height, minimumHeight, maximumHeight);
		left = Math.Clamp(left, _screenMargin, Main.screenWidth - _screenMargin - width);
		top = Math.Clamp(top, _screenMargin, Main.screenHeight - _screenMargin - height);
		Left.Set(left, 0f);
		Top.Set(top, 0f);
		Width.Set(width, 0f);
		Height.Set(height, 0f);
		Recalculate();
		if (notifyPosition)
			_positionChanged(new Vector2(left + width / 2f, top + height / 2f));
	}

	private void EndInteraction()
	{
		bool resized = _resizeEdge != BingoResizeEdge.None;
		_dragging = false;
		_resizeEdge = BingoResizeEdge.None;
		if (resized)
			_resizeCompleted(PanelWidth, PanelHeight);
	}
}
