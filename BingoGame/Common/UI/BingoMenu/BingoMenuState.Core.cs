using System;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState : UIState
{
	private const float ScreenMargin = 16f;

	private int _draftSize;

	private BingoWinRule _draftRule;

	private int[] _draftItems = Array.Empty<int>();

	private bool _editingBoard;

	private bool _editingWhitelists;

	private int _editingWhitelistIndex = -1;

	private string _whitelistEditorText = string.Empty;

	private BingoEditorSaveResult _whitelistEditorResult;

	private bool _editingInitialItemLists;

	private bool _editingAdvancedSettings;

	private int _editingInitialItemListIndex = -1;

	private string _initialItemEditorText = string.Empty;

	private BingoEditorSaveResult _initialItemEditorResult;

	private string _inventoryActionError = string.Empty;

	private BingoValidationFailure _failure;

	private BingoGamePhase _shownPhase;

	private int _shownHost;

	private int _shownRevision;

	private int _shownWidth;

	private int _shownHeight;

	private Vector2 _windowCenterOffset;

	private bool _gameWindowLocked;

	private bool _confirmingStop;

	private BingoAdaptiveText _timerText;

	public void Open()
	{
		LoadDraftFromWorld();
		_editingBoard = false;
		_editingWhitelists = false;
		_editingWhitelistIndex = -1;
		_editingInitialItemLists = false;
		_editingAdvancedSettings = false;
		_editingInitialItemListIndex = -1;
		_inventoryActionError = string.Empty;
		_failure = default;
		_confirmingStop = false;
		Rebuild();
	}

	public void EnsureConfigDefaults() => GetGameConfig();

	public void RefreshForWorldChanges()
	{
		bool layoutChanged = _shownWidth != Main.screenWidth || _shownHeight != Main.screenHeight;
		bool stateChanged = _shownPhase != BingoWorldSystem.Phase || _shownHost != BingoWorldSystem.HostPlayerId;
		if (!layoutChanged && !stateChanged && _shownRevision == BingoWorldSystem.StateRevision)
			return;

		if (stateChanged)
		{
			TrySavePersistentState(false);
			BingoNumericInput.ClearFocus();
			BingoTextInput.ClearFocus();
			_editingBoard = false;
			_editingWhitelists = false;
			_editingWhitelistIndex = -1;
			_editingInitialItemLists = false;
			_editingAdvancedSettings = false;
			_editingInitialItemListIndex = -1;
			_failure = default;
			_confirmingStop = false;
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

	public void SetInventoryActionFailure(BingoGame.InventoryActionError error)
	{
		_inventoryActionError = Text($"UI.InventoryActionErrors.{error}");
		Rebuild();
	}

	public bool TrySavePersistentState(bool blockOnInvalid)
	{
		BingoTextInput.ClearFocus();
		bool valid = true;
		if (_editingWhitelistIndex >= 0)
			valid &= !TryCommitWhitelistEditor().Failed;
		if (_editingInitialItemListIndex >= 0)
			valid &= !TryCommitInitialItemEditor().Failed;
		SaveDraftToConfig();
		GetGameConfig().SaveChanges();
		if (!valid && blockOnInvalid)
		{
			Rebuild();
			return false;
		}
		return true;
	}

	private void Rebuild()
	{
		RemoveAllChildren();
		_timerText = null;
		_shownPhase = BingoWorldSystem.Phase;
		_shownHost = BingoWorldSystem.HostPlayerId;
		_shownRevision = BingoWorldSystem.StateRevision;
		_shownWidth = Main.screenWidth;
		_shownHeight = Main.screenHeight;

		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
			BuildGameBoard();
		else if (!BingoWorldSystem.IsLocalPlayerHost)
			BuildWaitingMessage();
		else if (_editingInitialItemListIndex >= 0)
			BuildInitialItemEditor();
		else if (_editingInitialItemLists)
			BuildInitialItemList();
		else if (_editingWhitelistIndex >= 0)
			BuildWhitelistEditor();
		else if (_editingWhitelists)
			BuildWhitelistList();
		else if (_editingBoard)
			BuildBoardEditor();
		else if (_editingAdvancedSettings)
			BuildAdvancedSettings();
		else
			BuildSettings();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		_timerText?.SetText(BingoWorldSystem.FormatElapsed(BingoWorldSystem.GetDisplayElapsedTicks()));
	}
}
