using System;
using System.Collections.Generic;
using System.Linq;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
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
		BingoUITheme.RefreshOpacity();
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
		if (_interface?.CurrentState != null)
			_menu?.SaveDraftToConfig();
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

	public void SaveDraftToConfig()
	{
		if (_draftSize is < 2 or > 10 || _draftItems.Length != _draftSize * _draftSize)
			return;

		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		config.DraftBoardSize = _draftSize;
		config.DraftWinRule = _draftRule;
		config.DraftItemTypes = _draftItems.ToList();
		config.SaveChanges();
	}

	private void LoadDraftFromWorld()
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		if (config.DraftBoardSize is >= 2 and <= 10
			&& config.DraftItemTypes?.Count == config.DraftBoardSize * config.DraftBoardSize)
		{
			_draftSize = config.DraftBoardSize;
			_draftRule = Enum.IsDefined(typeof(BingoWinRule), config.DraftWinRule)
				? config.DraftWinRule
				: BingoWinRule.Line;
			_draftItems = config.DraftItemTypes.ToArray();
			return;
		}

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
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Settings, 560f, 400f);
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
		sizeRow.AddWeighted(sizeLabel, 3f, 90f);
		sizeRow.AddWeighted(sizeControls, 7f, 210f);

		UIHorizontalStack ruleRow = new(12f);
		BingoAdaptiveText ruleLabel = CreateText(panel, Text("UI.WinRule"), 0f, 0.5f, 1f);
		UIHorizontalStack ruleControls = new(8f);
		BingoButton line = CreateButton(panel, Text("UI.RuleLine"),
			() => SetRule(BingoWinRule.Line), _draftRule == BingoWinRule.Line);
		BingoButton majority = CreateButton(panel, Text("UI.RuleMajority"),
			() => SetRule(BingoWinRule.Majority), _draftRule == BingoWinRule.Majority);
		ruleControls.AddWeighted(line);
		ruleControls.AddWeighted(majority);
		ruleRow.AddWeighted(ruleLabel, 3f, 90f);
		ruleRow.AddWeighted(ruleControls, 7f, 210f);

		BingoButton configure = CreateButton(panel, Text("UI.ConfigureBoard"), OpenEditor);
		BingoAdaptiveText hint = CreateText(panel, Text("UI.EmptyCellsRandom"), 0.5f, 0.5f, 0.8f,
			BingoTextRole.Compact, Color.Silver);
		string failureValue = FailureText();
		BingoAdaptiveText failure = CreateText(panel, failureValue, 0.5f, 0.5f, 0.78f,
			BingoTextRole.Compact, Color.OrangeRed);
		UIHorizontalStack footer = new(24f);
		BingoButton close = CreateButton(panel, Text("UI.Close"), BingoUISystem.Toggle);
		BingoButton start = CreateButton(panel, Text("UI.StartGame"), StartGame, emphasized: true);
		footer.AddWeighted(close);
		footer.AddWeighted(start);

		root.AddFixed(title, 36f);
		root.AddFixed(sizeRow, 46f);
		root.AddFixed(ruleRow, 46f);
		root.AddFixed(configure, 48f);
		root.AddWeighted(new UIElement(), 1f);
		root.AddFixed(hint, 22f);
		if (failureValue.Length > 0)
			root.AddFixed(failure, 22f);
		root.AddFixed(footer, 48f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildBoardEditor()
	{
		float defaultCellSize = Math.Clamp(Math.Min(Main.screenWidth - 100f, Main.screenHeight - 180f) / _draftSize, 38f, 62f);
		float minimumWidth = Math.Max(360f, _draftSize * 38f + 40f);
		float minimumHeight = _draftSize * 38f + 125f;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Editor, minimumWidth, minimumHeight,
			defaultCellSize * _draftSize + 40f, defaultCellSize * _draftSize + 125f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText title = CreateText(panel, Text("UI.BoardEditorTitle"), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		UIBingoBoardGrid grid = new(_draftSize, 4f);

		for (int index = 0; index < _draftItems.Length; index++)
		{
			int capturedIndex = index;
			BingoNumericInput input = new(Text("UI.RandomHint"), _draftItems[index], () => panel.LayoutScale, value =>
			{
				_draftItems[capturedIndex] = value;
				if (_failure.CellIndex == capturedIndex)
					_failure = default;
			});
			input.IsInvalid = _failure.CellIndex == index;
			grid.AddCell(input);
		}

		UIHorizontalStack footer = new(12f);
		BingoButton back = CreateButton(panel, Text("UI.Back"), CloseEditor);
		BingoAdaptiveText failure = CreateText(panel, FailureText(), 0.5f, 0.5f, 0.72f,
			BingoTextRole.Compact, Color.OrangeRed);
		BingoButton clear = CreateButton(panel, Text("UI.Clear"), ClearDraftItems);
		footer.AddWeighted(back, 1f, 90f);
		footer.AddWeighted(failure, 2f, 100f);
		footer.AddWeighted(clear, 1f, 90f);
		root.AddFixed(title, 32f);
		root.AddWeighted(grid, 1f, _draftSize * 24f);
		root.AddFixed(footer, 42f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void BuildGameBoard()
	{
		int size = BingoWorldSystem.BoardSize;
		bool host = BingoWorldSystem.IsLocalPlayerHost;
		float footerHeight = host ? 140f : 95f;
		float available = Math.Min(Main.screenWidth - 100f, Main.screenHeight - 190f);
		float defaultCellSize = Math.Clamp(available / size, 36f, 64f);
		float minimumWidth = Math.Max(360f, size * 36f + 40f);
		float minimumHeight = size * 36f + footerHeight;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Game, minimumWidth, minimumHeight,
			defaultCellSize * size + 40f, defaultCellSize * size + footerHeight);
		panel.Locked = _gameWindowLocked && BingoWorldSystem.Phase == BingoGamePhase.InProgress;
		UIVerticalStack root = CreateRootStack(6f, 8f);
		UIElement header = new();
		string title = BingoWorldSystem.WinRule == BingoWinRule.Line ? Text("UI.RuleLine") : Text("UI.RuleMajority");
		BingoAdaptiveText titleText = CreateText(panel, Text("UI.BoardTitle", title), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		titleText.Width.Set(0f, 1f);
		titleText.Height.Set(0f, 1f);
		header.Append(titleText);

		BingoBoardElement board = new(size, () => panel.BorderColor);

		UIHorizontalStack scoreRow = new(6f);
		int visibleTeams = 0;
		for (int team = 1; team <= 5; team++)
		{
			int score = BingoWorldSystem.GetTeamScore(team);
			if (score <= 0)
				continue;
			BingoAdaptiveText teamScore = CreateText(panel,
				Text("UI.TeamScore", BingoTeamDisplay.GetName(team), score), 0.5f, 0.5f, 0.72f,
				BingoTextRole.Compact, BingoBoardElement.GetTeamColor(team));
			scoreRow.AddWeighted(teamScore);
			visibleTeams++;
		}
		if (visibleTeams == 0)
			scoreRow.AddWeighted(CreateText(panel, Text("UI.NoClaims"), 0.5f, 0.5f, 0.72f,
				BingoTextRole.Compact));
		BingoAdaptiveText resultText = CreateText(panel, ResultText(), 0.5f, 0.5f, 0.82f,
			BingoTextRole.Compact, ResultColor());

		UIElement stopRow = null;
		if (host)
		{
			BingoButton stop = CreateButton(panel, Text("UI.StopGame"), BingoGame.RequestStop, emphasized: true);
			stop.Width.Set(120f, 0f);
			stop.Height.Set(0f, 1f);
			stop.HAlign = 0.5f;
			stopRow = new UIElement();
			stopRow.Append(stop);
		}

		BingoButton lockButton = null;
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
		{
			lockButton = CreateButton(panel, Text(_gameWindowLocked ? "UI.Unlock" : "UI.Lock"),
				ToggleGameWindowLock, selected: _gameWindowLocked, textRole: BingoTextRole.Compact);
			lockButton.Width.Set(70f, 0f);
			lockButton.Height.Set(0f, 1f);
			lockButton.HAlign = 1f;
			header.Append(lockButton);
			panel.DragExclusion = lockButton;
		}
		root.AddFixed(header, 32f);
		root.AddWeighted(board, 1f, size * 24f);
		root.AddFixed(scoreRow, 22f);
		root.AddFixed(resultText, 22f);
		if (stopRow != null)
			root.AddFixed(stopRow, 32f);
		panel.Append(root);
		panel.Recalculate();
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
		SaveDraftToConfig();
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

	private static BingoAdaptiveText CreateText(BingoResponsivePanel panel, string value, float hAlign, float vAlign,
		float scale, BingoTextRole role = BingoTextRole.Normal, Color? color = null)
	{
		BingoAdaptiveText text = new(value, scale, hAlign, vAlign, role, () => panel.LayoutScale);
		if (color.HasValue)
			text.TextColor = color.Value;
		return text;
	}

	private static BingoButton CreateButton(BingoResponsivePanel panel, string label, Action action,
		bool selected = false, bool emphasized = false, BingoTextRole textRole = BingoTextRole.Normal)
	{
		BingoButton button = new(action, selected, emphasized);
		BingoAdaptiveText text = CreateText(panel, label, 0.5f, 0.5f, 0.86f, textRole);
		text.Width.Set(0f, 1f);
		text.Height.Set(0f, 1f);
		button.Append(text);
		return button;
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
		return Text("UI.TeamWon", BingoTeamDisplay.GetName(BingoWorldSystem.WinningTeam));
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

internal static class BingoUITheme
{
	private static byte _backgroundAlpha = 204;
	private static byte BackgroundAlpha => _backgroundAlpha;
	public static Color CellBackground => WithOpacity(new Color(63, 82, 151));

	public static void RefreshOpacity()
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		float value = BingoWorldSystem.Phase == BingoGamePhase.InProgress
			? config.InProgressUIOpacity
			: config.InactiveUIOpacity;
		_backgroundAlpha = (byte)MathF.Round(255f * Math.Clamp(value, 0f, 1f));
	}

	public static void Apply(UIPanel panel, bool selected = false, bool emphasized = false)
	{
		Color background = panel.BackgroundColor;
		Color border = panel.BorderColor;
		if (selected)
		{
			background = Color.Lerp(background, Color.White, 0.2f);
			border = Color.Lerp(border, Color.White, 0.35f);
		}
		else if (emphasized)
		{
			background = Color.Lerp(background, new Color(120, 170, 255), 0.18f);
			border = Color.Lerp(border, Color.White, 0.18f);
		}
		panel.BackgroundColor = WithOpacity(background);
		panel.BorderColor = WithOpacity(border);
	}

	public static Color WithOpacity(Color color)
	{
		color.A = BackgroundAlpha;
		return color;
	}
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
	private readonly float _referenceWidth;
	private readonly float _referenceHeight;
	private readonly float _screenMargin;
	private readonly Action<Vector2> _positionChanged;
	private readonly Action<float, float> _resizeCompleted;
	private Vector2 _interactionStart;
	private Rectangle _startBounds;
	private BingoResizeEdge _resizeEdge;
	private bool _dragging;

	public bool Locked { get; set; }
	public UIElement DragExclusion { get; set; }
	public float PanelWidth => Width.Pixels;
	public float PanelHeight => Height.Pixels;
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
		BackgroundColor = BingoUITheme.WithOpacity(BackgroundColor);
		BorderColor = BingoUITheme.WithOpacity(BorderColor);
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

internal sealed class BingoButton : UIPanel
{
	private readonly Action _action;
	private readonly Color _normalColor;

	public BingoButton(Action action, bool selected, bool emphasized)
	{
		_action = action;
		OverflowHidden = true;
		SetPadding(4f);
		BingoUITheme.Apply(this, selected, emphasized);
		_normalColor = BackgroundColor;
		OnLeftClick += (_, _) => _action();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Color normalColor = BingoUITheme.WithOpacity(_normalColor);
		BackgroundColor = IsMouseHovering
			? BingoUITheme.WithOpacity(Color.Lerp(normalColor, Color.White, 0.16f))
			: normalColor;
		BorderColor = BingoUITheme.WithOpacity(BorderColor);
		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}

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
		border = BingoUITheme.WithOpacity(border);
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

internal static class BingoItemIconRenderer
{
	public static void Draw(SpriteBatch spriteBatch, Rectangle bounds, int itemType, float padding)
	{
		if (!BingoWorldSystem.IsUsableItemId(itemType))
			return;
		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = texture.Frame();
		float availableWidth = Math.Max(1f, bounds.Width - padding * 2f);
		float availableHeight = Math.Max(1f, bounds.Height - padding * 2f);
		float scale = Math.Min(1f, Math.Min(availableWidth / frame.Width, availableHeight / frame.Height));
		spriteBatch.Draw(texture, bounds.Center.ToVector2(), frame, Color.White, 0f, frame.Size() / 2f,
			scale, SpriteEffects.None, 0f);
	}
}

internal sealed class BingoBoardElement : UIElement
{
	private const float CellSpacing = 4f;
	private readonly int _size;
	private readonly List<BingoBoardCell> _cells = new();

	public BingoBoardElement(int size, Func<Color> borderColor)
	{
		_size = size;
		for (int index = 0; index < BingoWorldSystem.ItemTypes.Length; index++)
		{
			BingoBoardCell cell = new(BingoWorldSystem.ItemTypes[index], BingoWorldSystem.Owners[index], borderColor);
			_cells.Add(cell);
			Append(cell);
		}
	}

	public override void RecalculateChildren()
	{
		CalculatedStyle dimensions = GetDimensions();
		float availableWidth = Math.Max(0f, dimensions.Width - CellSpacing * (_size - 1));
		float availableHeight = Math.Max(0f, dimensions.Height - CellSpacing * (_size - 1));
		float cellSize = Math.Max(0f, Math.Min(availableWidth, availableHeight) / _size);
		float boardPixels = cellSize * _size + CellSpacing * (_size - 1);
		float boardLeft = Math.Max(0f, (dimensions.Width - boardPixels) * 0.5f);
		float boardTop = Math.Max(0f, (dimensions.Height - boardPixels) * 0.5f);
		for (int index = 0; index < _cells.Count; index++)
		{
			int row = index / _size;
			int column = index % _size;
			BingoBoardCell cell = _cells[index];
			UIStack.SetChildBounds(cell, boardLeft + column * (cellSize + CellSpacing),
				boardTop + row * (cellSize + CellSpacing), cellSize, cellSize);
			cell.Recalculate();
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

internal sealed class BingoBoardCell : UIPanel
{
	private readonly int _itemType;
	private readonly byte _owner;
	private readonly Func<Color> _borderColor;

	public BingoBoardCell(int itemType, byte owner, Func<Color> borderColor)
	{
		_itemType = itemType;
		_owner = owner;
		_borderColor = borderColor;
		BackgroundColor = Color.Transparent;
		SetPadding(0f);
		OverflowHidden = true;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		BackgroundColor = _owner is >= 1 and <= 5
			? BingoUITheme.WithOpacity(BingoBoardElement.GetTeamColor(_owner))
			: Color.Transparent;
		Color borderColor = _borderColor?.Invoke() ?? Color.White;
		borderColor.A = byte.MaxValue;
		BorderColor = borderColor;
		base.DrawSelf(spriteBatch);

		Rectangle bounds = GetDimensions().ToRectangle();
		if (BingoWorldSystem.IsUsableItemId(_itemType))
			BingoItemIconRenderer.Draw(spriteBatch, bounds, _itemType, 7f);

		if (bounds.Contains(Main.MouseScreen.ToPoint()) && BingoWorldSystem.IsUsableItemId(_itemType))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = $"{Lang.GetItemNameValue(_itemType)} ({_itemType})";
		}
	}
}
