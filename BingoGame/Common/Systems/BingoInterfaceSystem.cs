using System;
using System.Collections.Generic;
using System.Linq;
using BingoGame.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class BingoUISystem : ModSystem
{
	private UserInterface _interface;
	private BingoMenuState _menu;
	private GameTime _lastUpdateTime;

	public static bool IsEditingText => BingoNumericInput.AnyFocused;

	public override void PostSetupContent()
	{
		_interface = new UserInterface();
		_menu = new BingoMenuState();
		_menu.Activate();
	}

	public override void OnWorldLoad() => Hide();

	public override void OnWorldUnload() => Hide();

	public override void Unload()
	{
		BingoNumericInput.ClearFocus();
		_interface = null;
		_menu = null;
		_lastUpdateTime = null;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastUpdateTime = gameTime;
		if (Main.gameMenu)
		{
			Hide();
			return;
		}

		if (_interface?.CurrentState != null)
		{
			_menu.RefreshForWorldChanges();
			_interface.Update(gameTime);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
			return;

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"BingoGame: Menu",
			() =>
			{
				if (_lastUpdateTime != null && _interface?.CurrentState != null)
					_interface.Draw(Main.spriteBatch, _lastUpdateTime);
				return true;
			}, InterfaceScaleType.UI));
	}

	public static void Toggle()
	{
		BingoUISystem system = ModContent.GetInstance<BingoUISystem>();
		if (system._interface?.CurrentState == null)
			system.Show();
		else
			system.Hide();
	}

	public static void SetValidationFailure(BingoValidationError error, int cellIndex)
	{
		BingoUISystem system = ModContent.GetInstance<BingoUISystem>();
		system._menu?.SetValidationFailure(new BingoValidationFailure(error, cellIndex));
	}

	private void Show()
	{
		if (_interface == null || _menu == null || Main.gameMenu)
			return;
		_menu.Open();
		_interface.SetState(_menu);
	}

	private void Hide()
	{
		BingoNumericInput.ClearFocus();
		_interface?.SetState(null);
	}
}

internal sealed class BingoMenuState : UIState
{
	private const float ScreenMargin = 16f;

	private int _draftSize;
	private BingoWinRule _draftRule;
	private int[] _draftItems = Array.Empty<int>();
	private bool _editingBoard;
	private BingoValidationFailure _failure;
	private BingoGamePhase _shownPhase;
	private int _shownHost;
	private int _shownRevision;
	private int _shownWidth;
	private int _shownHeight;
	private Vector2 _windowCenterOffset;
	private bool _gameWindowLocked;

	public void Open()
	{
		LoadDraftFromWorld();
		_editingBoard = false;
		_failure = default;
		Rebuild();
	}

	public void RefreshForWorldChanges()
	{
		bool layoutChanged = _shownWidth != Main.screenWidth || _shownHeight != Main.screenHeight;
		bool stateChanged = _shownPhase != BingoWorldSystem.Phase || _shownHost != BingoWorldSystem.HostPlayerId;
		if (!layoutChanged && !stateChanged && _shownRevision == BingoWorldSystem.StateRevision)
			return;

		if (stateChanged)
		{
			BingoNumericInput.ClearFocus();
			_editingBoard = false;
			_failure = default;
			if (BingoWorldSystem.Phase == BingoGamePhase.NotStarted)
				LoadDraftFromWorld();
		}
		Rebuild();
	}

	public void SetValidationFailure(BingoValidationFailure failure)
	{
		_failure = failure;
		if (failure.CellIndex >= 0)
			_editingBoard = true;
		Rebuild();
	}

	private void LoadDraftFromWorld()
	{
		_draftSize = Math.Clamp(BingoWorldSystem.BoardSize, 2, 10);
		_draftRule = BingoWorldSystem.WinRule;
		_draftItems = BingoWorldSystem.ItemTypes.Length == _draftSize * _draftSize
			? (int[])BingoWorldSystem.ItemTypes.Clone()
			: new int[_draftSize * _draftSize];
	}

	private void Rebuild()
	{
		RemoveAllChildren();
		_shownPhase = BingoWorldSystem.Phase;
		_shownHost = BingoWorldSystem.HostPlayerId;
		_shownRevision = BingoWorldSystem.StateRevision;
		_shownWidth = Main.screenWidth;
		_shownHeight = Main.screenHeight;

		if (BingoWorldSystem.Phase != BingoGamePhase.NotStarted)
			BuildGameBoard();
		else if (!BingoWorldSystem.IsLocalPlayerHost)
			BuildWaitingMessage();
		else if (_editingBoard)
			BuildBoardEditor();
		else
			BuildSettings();
	}

	private void BuildWaitingMessage()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Waiting, 320f, 140f);
		UIText message = CreateText(Text("UI.GameNotStarted"), 0.5f, 0.5f, 1.15f);
		panel.Append(message);
		panel.SetLayout(_ => { });
	}

	private void BuildSettings()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Settings, 560f, 400f);
		UIText title = CreateText(Text("UI.Title"), 0.5f, 0f, 1.25f);
		UIText sizeLabel = CreateText(Text("UI.BoardSize"), 0.18f, 0f, 1f);
		BingoButton decrease = CreateButton("−", 0f, 0f, 54f, 42f, () => ResizeDraft(_draftSize - 1));
		UIText sizeValue = CreateText($"{_draftSize} × {_draftSize}", 0.5f, 0f, 1f);
		BingoButton increase = CreateButton("+", 0f, 0f, 54f, 42f, () => ResizeDraft(_draftSize + 1));
		UIText ruleLabel = CreateText(Text("UI.WinRule"), 0.18f, 0f, 1f);
		BingoButton line = CreateButton(Text("UI.RuleLine"), 0f, 0f, 130f, 44f,
			() => SetRule(BingoWinRule.Line), _draftRule == BingoWinRule.Line);
		BingoButton majority = CreateButton(Text("UI.RuleMajority"), 0f, 0f, 150f, 44f,
			() => SetRule(BingoWinRule.Majority), _draftRule == BingoWinRule.Majority);
		BingoButton configure = CreateButton(Text("UI.ConfigureBoard"), 0f, 0f, 260f, 48f, OpenEditor);
		UIText hint = CreateText(Text("UI.EmptyCellsRandom"), 0.5f, 0f, 0.8f, color: Color.Silver);
		string failureValue = FailureText();
		UIText failure = CreateText(failureValue, 0.5f, 0f, 0.78f, color: Color.OrangeRed);
		BingoButton close = CreateButton(Text("UI.Close"), 0f, 0f, 180f, 48f, BingoUISystem.Toggle);
		BingoButton start = CreateButton(Text("UI.StartGame"), 0f, 0f, 180f, 48f, StartGame, emphasized: true);

		panel.Append(title);
		panel.Append(sizeLabel);
		panel.Append(decrease);
		panel.Append(sizeValue);
		panel.Append(increase);
		panel.Append(ruleLabel);
		panel.Append(line);
		panel.Append(majority);
		panel.Append(configure);
		panel.Append(hint);
		if (failureValue.Length > 0)
			panel.Append(failure);
		panel.Append(close);
		panel.Append(start);

		panel.SetLayout(p =>
		{
			float width = p.PanelWidth;
			float height = p.PanelHeight;
			float firstRow = Math.Max(70f, height * 0.19f);
			float secondRow = Math.Max(138f, height * 0.35f);
			float configureTop = Math.Max(205f, height * 0.51f);
			float footerTop = height - 70f;
			float footerWidth = Math.Min(200f, (width - 80f) / 2f);
			float ruleWidth = Math.Min(160f, (width - 100f) / 2f);
			SetTop(title, 18f);
			SetTop(sizeLabel, firstRow + 12f);
			SetBounds(decrease, width * 0.5f - 115f, firstRow, 54f, 42f);
			SetTop(sizeValue, firstRow + 10f);
			SetBounds(increase, width * 0.5f + 61f, firstRow, 54f, 42f);
			SetTop(ruleLabel, secondRow + 12f);
			SetBounds(line, width * 0.5f - ruleWidth - 10f, secondRow, ruleWidth, 44f);
			SetBounds(majority, width * 0.5f + 10f, secondRow, ruleWidth, 44f);
			SetBounds(configure, (width - Math.Min(300f, width - 80f)) / 2f, configureTop,
				Math.Min(300f, width - 80f), 48f);
			SetTop(hint, configureTop + 58f);
			SetTop(failure, Math.Min(footerTop - 30f, configureTop + 92f));
			SetBounds(close, width * 0.5f - footerWidth - 20f, footerTop, footerWidth, 48f);
			SetBounds(start, width * 0.5f + 20f, footerTop, footerWidth, 48f);
		});
	}

	private void BuildBoardEditor()
	{
		float defaultCellSize = Math.Clamp(Math.Min(Main.screenWidth - 100f, Main.screenHeight - 180f) / _draftSize, 38f, 62f);
		float minimumWidth = Math.Max(360f, _draftSize * 38f + 40f);
		float minimumHeight = _draftSize * 38f + 125f;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Editor, minimumWidth, minimumHeight,
			defaultCellSize * _draftSize + 40f, defaultCellSize * _draftSize + 125f);
		UIText title = CreateText(Text("UI.BoardEditorTitle"), 0.5f, 0f, 1.1f, 10f);
		panel.Append(title);
		BingoNumericInput[] inputs = new BingoNumericInput[_draftItems.Length];

		for (int index = 0; index < _draftItems.Length; index++)
		{
			int capturedIndex = index;
			BingoNumericInput input = new(Text("UI.RandomHint"), _draftItems[index], value =>
			{
				_draftItems[capturedIndex] = value;
				if (_failure.CellIndex == capturedIndex)
					_failure = default;
			});
			input.IsInvalid = _failure.CellIndex == index;
			inputs[index] = input;
			panel.Append(input);
		}

		BingoButton back = CreateButton(Text("UI.Back"), 0f, 0f, 145f, 42f, CloseEditor);
		UIText failure = CreateText(FailureText(), 0.5f, 0f, 0.72f, color: Color.OrangeRed);
		BingoButton clear = CreateButton(Text("UI.Clear"), 0f, 0f, 145f, 42f, ClearDraftItems);
		panel.Append(back);
		panel.Append(failure);
		panel.Append(clear);
		panel.SetLayout(p =>
		{
			float cellSize = Math.Min((p.PanelWidth - 40f) / _draftSize, (p.PanelHeight - 125f) / _draftSize);
			float boardPixels = cellSize * _draftSize;
			float boardLeft = (p.PanelWidth - boardPixels) / 2f;
			float boardTop = 46f + Math.Max(0f, (p.PanelHeight - 125f - boardPixels) / 2f);
			for (int index = 0; index < inputs.Length; index++)
			{
				int row = index / _draftSize;
				int column = index % _draftSize;
				SetBounds(inputs[index], boardLeft + column * cellSize, boardTop + row * cellSize,
					Math.Max(24f, cellSize - 4f), Math.Max(24f, cellSize - 4f));
			}
			float buttonTop = p.PanelHeight - 57f;
			SetBounds(back, 20f, buttonTop, Math.Min(145f, p.PanelWidth * 0.28f), 42f);
			SetTop(failure, buttonTop + 8f);
			SetBounds(clear, p.PanelWidth - Math.Min(145f, p.PanelWidth * 0.28f) - 20f, buttonTop,
				Math.Min(145f, p.PanelWidth * 0.28f), 42f);
		});
	}

	private void BuildGameBoard()
	{
		int size = BingoWorldSystem.BoardSize;
		bool host = BingoWorldSystem.IsLocalPlayerHost;
		float footerHeight = host ? 150f : 95f;
		float available = Math.Min(Main.screenWidth - 100f, Main.screenHeight - 190f);
		float defaultCellSize = Math.Clamp(available / size, 36f, 64f);
		float minimumWidth = Math.Max(300f, size * 36f + 40f);
		float minimumHeight = size * 36f + footerHeight;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Game, minimumWidth, minimumHeight,
			defaultCellSize * size + 40f, defaultCellSize * size + footerHeight);
		panel.Locked = _gameWindowLocked && BingoWorldSystem.Phase == BingoGamePhase.InProgress;
		string title = BingoWorldSystem.WinRule == BingoWinRule.Line ? Text("UI.RuleLine") : Text("UI.RuleMajority");
		UIText titleText = CreateText(Text("UI.BoardTitle", title), 0.5f, 0f, 1.1f, 10f);
		panel.Append(titleText);

		BingoBoardElement board = new(size);
		panel.Append(board);

		string scores = string.Join("   ", Enumerable.Range(1, 5)
			.Where(team => BingoWorldSystem.GetTeamScore(team) > 0)
			.Select(team => Text("UI.TeamScore", team, BingoWorldSystem.GetTeamScore(team))));
		if (scores.Length == 0)
			scores = Text("UI.NoClaims");
		UIText scoreText = CreateText(scores, 0.5f, 0f, 0.72f);
		UIText resultText = CreateText(ResultText(), 0.5f, 0f, 0.82f, color: ResultColor());
		panel.Append(scoreText);
		panel.Append(resultText);

		BingoButton stop = null;
		if (host)
		{
			stop = CreateButton(Text("UI.StopGame"), 0f, 0f, 180f, 42f, BingoGame.RequestStop, emphasized: true);
			panel.Append(stop);
		}

		BingoButton lockButton = null;
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
		{
			lockButton = CreateButton(Text(_gameWindowLocked ? "UI.Unlock" : "UI.Lock"), 0f, 8f, 64f, 28f, ToggleGameWindowLock,
				selected: _gameWindowLocked);
			panel.Append(lockButton);
			panel.DragExclusion = lockButton;
		}

		panel.SetLayout(p =>
		{
			float cellSize = Math.Min((p.PanelWidth - 40f) / size, (p.PanelHeight - footerHeight) / size);
			float boardPixels = cellSize * size;
			float boardLeft = (p.PanelWidth - boardPixels) / 2f;
			float boardTop = 46f + Math.Max(0f, (p.PanelHeight - footerHeight - boardPixels) / 2f);
			SetBounds(board, boardLeft, boardTop, boardPixels, boardPixels);
			board.CellSize = cellSize;
			SetTop(scoreText, boardTop + boardPixels + 8f);
			SetTop(resultText, boardTop + boardPixels + 32f);
			if (stop != null)
				SetBounds(stop, (p.PanelWidth - 180f) / 2f, boardTop + boardPixels + 57f, 180f, 42f);
			if (lockButton != null)
				SetBounds(lockButton, p.PanelWidth - 82f, 8f, 64f, 28f);
		});
	}

	private void ToggleGameWindowLock()
	{
		_gameWindowLocked = !_gameWindowLocked;
		Rebuild();
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

	private void OpenEditor()
	{
		_editingBoard = true;
		Rebuild();
	}

	private void CloseEditor()
	{
		BingoNumericInput.ClearFocus();
		_editingBoard = false;
		Rebuild();
	}

	private void ClearDraftItems()
	{
		Array.Clear(_draftItems, 0, _draftItems.Length);
		_failure = default;
		BingoNumericInput.ClearFocus();
		Rebuild();
	}

	private void StartGame()
	{
		if (!ValidateDraft(out BingoValidationFailure failure))
		{
			SetValidationFailure(failure);
			return;
		}
		_failure = default;
		BingoNumericInput.ClearFocus();
		BingoGame.RequestStart(_draftSize, _draftRule, (int[])_draftItems.Clone());
	}

	private bool ValidateDraft(out BingoValidationFailure failure)
	{
		HashSet<int> seen = new();
		for (int i = 0; i < _draftItems.Length; i++)
		{
			int itemType = _draftItems[i];
			if (itemType == 0)
				continue;
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
		BingoResponsivePanel panel = new(savedSize.X, savedSize.Y, minimumWidth, minimumHeight, ScreenMargin, center,
			newCenter => _windowCenterOffset = newCenter - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f),
			(width, height) => SaveSize(page, width, height));
		panel.BackgroundColor = new Color(28, 36, 50, 242);
		panel.BorderColor = new Color(80, 105, 135, 255);
		Append(panel);
		return panel;
	}

	private static Vector2 GetSavedSize(BingoWindowPage page)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		return page switch
		{
			BingoWindowPage.Settings => new Vector2(config.SettingsWidth, config.SettingsHeight),
			BingoWindowPage.Waiting => new Vector2(config.WaitingWidth, config.WaitingHeight),
			BingoWindowPage.Editor => new Vector2(config.EditorWidth, config.EditorHeight),
			BingoWindowPage.Game => new Vector2(config.GameWidth, config.GameHeight),
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
		}
		config.SaveChanges();
	}

	private static UIText CreateText(string value, float hAlign, float vAlign, float scale, float top = 0f, Color? color = null)
	{
		UIText text = new(value, scale);
		text.HAlign = hAlign;
		text.VAlign = vAlign;
		text.Top.Set(top, 0f);
		if (color.HasValue)
			text.TextColor = color.Value;
		return text;
	}

	private static BingoButton CreateButton(string label, float left, float top, float width, float height,
		Action action, bool selected = false, bool emphasized = false)
	{
		BingoButton button = new(action);
		button.Left.Set(left, 0f);
		button.Top.Set(top, 0f);
		button.Width.Set(width, 0f);
		button.Height.Set(height, 0f);
		button.BackgroundColor = selected || emphasized ? new Color(46, 113, 77) : new Color(48, 60, 78);
		button.BorderColor = selected ? new Color(120, 230, 155) : new Color(88, 110, 138);
		button.Append(CreateText(label, 0.5f, 0.5f, 0.86f));
		return button;
	}

	private static void SetTop(UIElement element, float top)
	{
		element.Top.Set(top, 0f);
	}

	private static void SetBounds(UIElement element, float left, float top, float width, float height)
	{
		element.Left.Set(left, 0f);
		element.Top.Set(top, 0f);
		element.Width.Set(width, 0f);
		element.Height.Set(height, 0f);
	}

	private string FailureText()
	{
		if (_failure.Error == BingoValidationError.None)
			return string.Empty;
		string key = $"UI.Errors.{_failure.Error}";
		return _failure.CellIndex >= 0 ? Text(key, _failure.CellIndex + 1) : Text(key);
	}

	private static string ResultText()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
			return Text("UI.InProgress");
		if (BingoWorldSystem.IsDraw)
			return Text("UI.Draw");
		return Text("UI.TeamWon", BingoWorldSystem.WinningTeam);
	}

	private static Color ResultColor()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress || BingoWorldSystem.IsDraw)
			return Color.White;
		return BingoBoardElement.GetTeamColor(BingoWorldSystem.WinningTeam);
	}

	private static string Text(string key, params object[] args) =>
		Language.GetTextValue($"Mods.BingoGame.{key}", args);
}

internal enum BingoWindowPage
{
	Settings,
	Waiting,
	Editor,
	Game
}

[Flags]
internal enum BingoResizeEdge
{
	None = 0,
	Left = 1,
	Right = 2,
	Top = 4,
	Bottom = 8
}

internal sealed class BingoResponsivePanel : UIPanel
{
	private const float HeaderHeight = 46f;
	private const float ResizeBorder = 6f;
	private const float ResizeCorner = 12f;

	private readonly float _minimumWidth;
	private readonly float _minimumHeight;
	private readonly float _screenMargin;
	private readonly Action<Vector2> _positionChanged;
	private readonly Action<float, float> _resizeCompleted;
	private Action<BingoResponsivePanel> _layout;
	private Vector2 _interactionStart;
	private Rectangle _startBounds;
	private BingoResizeEdge _resizeEdge;
	private bool _dragging;

	public bool Locked { get; set; }
	public UIElement DragExclusion { get; set; }
	public float PanelWidth => Width.Pixels;
	public float PanelHeight => Height.Pixels;

	public BingoResponsivePanel(float width, float height, float minimumWidth, float minimumHeight, float screenMargin,
		Vector2 center, Action<Vector2> positionChanged, Action<float, float> resizeCompleted)
	{
		_minimumWidth = minimumWidth;
		_minimumHeight = minimumHeight;
		_screenMargin = screenMargin;
		_positionChanged = positionChanged;
		_resizeCompleted = resizeCompleted;
		OnLeftMouseDown += BeginInteraction;
		OnLeftMouseUp += (_, _) => EndInteraction();
		ApplyBounds(center.X - width / 2f, center.Y - height / 2f, width, height, false);
	}

	public void SetLayout(Action<BingoResponsivePanel> layout)
	{
		_layout = layout;
		ApplyLayout();
	}

	public override void Update(GameTime gameTime)
	{
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
		ApplyLayout();
		Recalculate();
		if (notifyPosition)
			_positionChanged(new Vector2(left + width / 2f, top + height / 2f));
	}

	private void ApplyLayout()
	{
		_layout?.Invoke(this);
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

internal sealed class BingoButton : UIPanel
{
	private readonly Action _action;
	private Color _normalColor;

	public BingoButton(Action action)
	{
		_action = action;
		OnLeftClick += (_, _) => _action();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (_normalColor == default)
			_normalColor = BackgroundColor;
		BackgroundColor = IsMouseHovering ? Color.Lerp(_normalColor, Color.White, 0.16f) : _normalColor;
		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}

internal sealed class BingoNumericInput : UIElement
{
	private static BingoNumericInput _focused;
	private readonly string _hint;
	private readonly Action<int> _changed;
	private string _text;

	public static bool AnyFocused => _focused != null;
	public bool IsInvalid { get; set; }

	public BingoNumericInput(string hint, int value, Action<int> changed)
	{
		_hint = hint;
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
		Color border = IsInvalid ? Color.OrangeRed : _focused == this ? new Color(130, 210, 255) : new Color(82, 100, 125);
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(24, 30, 42, 245));
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

		string display = _text.Length == 0 ? _hint : _text;
		Color color = _text.Length == 0 ? Color.Gray : Color.White;
		float scale = Math.Clamp((bounds.Width - 8f) / Math.Max(1f, FontAssets.MouseText.Value.MeasureString(display).X), 0.55f, 0.82f);
		Vector2 center = bounds.Center.ToVector2();
		Utils.DrawBorderString(spriteBatch, display, center, color, scale, 0.5f, 0.5f);
	}

	private void Focus()
	{
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

internal sealed class BingoBoardElement : UIElement
{
	private readonly int _size;
	public float CellSize { get; set; }

	public BingoBoardElement(int size)
	{
		_size = size;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		for (int index = 0; index < BingoWorldSystem.ItemTypes.Length; index++)
		{
			int row = index / _size;
			int column = index % _size;
			Rectangle cell = new((int)(dimensions.X + column * CellSize), (int)(dimensions.Y + row * CellSize),
				(int)CellSize - 2, (int)CellSize - 2);
			DrawCell(spriteBatch, cell, BingoWorldSystem.ItemTypes[index], BingoWorldSystem.Owners[index]);
		}
	}

	private static void DrawCell(SpriteBatch spriteBatch, Rectangle cell, int itemType, byte owner)
	{
		Color fill = owner == 0 ? new Color(54, 61, 72, 235) : GetTeamColor(owner) * 0.78f;
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, cell, fill);
		if (itemType <= 0 || !ContentSamples.ItemsByType.ContainsKey(itemType))
			return;

		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = texture.Frame();
		float maxDimension = Math.Max(frame.Width, frame.Height);
		float scale = maxDimension > cell.Width - 10 ? (cell.Width - 10) / maxDimension : 1f;
		spriteBatch.Draw(texture, cell.Center.ToVector2(), frame, Color.White, 0f, frame.Size() / 2f,
			scale, SpriteEffects.None, 0f);

		if (cell.Contains(Main.MouseScreen.ToPoint()))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = $"{Lang.GetItemNameValue(itemType)} ({itemType})";
		}
	}

	internal static Color GetTeamColor(int team) => team switch
	{
		1 => new Color(230, 70, 70),
		2 => new Color(80, 210, 90),
		3 => new Color(75, 135, 235),
		4 => new Color(235, 210, 65),
		5 => new Color(225, 105, 205),
		_ => Color.Gray
	};
}
