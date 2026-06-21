using System.Collections.Generic;
using BingoGame.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class BingoUISystem : ModSystem
{
	private UserInterface _interface;
	private BingoMenuState _menu;
	private UserInterface _resultInterface;
	private BingoResultState _result;
	private GameTime _lastUpdateTime;
	private BingoGamePhase _lastObservedPhase;

	public static bool IsEditingText => BingoNumericInput.AnyFocused || BingoTextInput.AnyFocused;

	public override void PostSetupContent()
	{
		BingoClientConfig clientConfig = ModContent.GetInstance<BingoClientConfig>();
		clientConfig.MigrateLegacyDraft(ModContent.GetInstance<BingoGameConfig>());
		_interface = new UserInterface();
		_menu = new BingoMenuState();
		_menu.Activate();
		_resultInterface = new UserInterface();
		_result = new BingoResultState(HideResult);
		_result.Activate();
		_menu.EnsureConfigDefaults();
		_lastObservedPhase = BingoWorldSystem.Phase;
	}

	public override void OnWorldLoad()
	{
		HideAll(true);
		_lastObservedPhase = BingoWorldSystem.Phase;
	}

	public override void OnWorldUnload()
	{
		HideAll(true);
		_lastObservedPhase = BingoGamePhase.NotStarted;
	}

	public override void Unload()
	{
		BingoNumericInput.ClearFocus();
		BingoTextInput.ClearFocus(false);
		_interface = null;
		_menu = null;
		_resultInterface = null;
		_result = null;
		_lastUpdateTime = null;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastUpdateTime = gameTime;
		BingoUITheme.RefreshOpacity();
		if (Main.gameMenu)
		{
			HideAll(true);
			return;
		}
		if (_lastObservedPhase != BingoWorldSystem.Phase)
		{
			BingoGamePhase previousPhase = _lastObservedPhase;
			_lastObservedPhase = BingoWorldSystem.Phase;
			if (previousPhase == BingoGamePhase.InProgress && _lastObservedPhase == BingoGamePhase.Finished)
				ShowResult();
		}

		if (_interface?.CurrentState != null)
		{
			_menu.RefreshForWorldChanges();
			_interface.Update(gameTime);
		}
		if (_resultInterface?.CurrentState != null)
			_resultInterface.Update(gameTime);
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
				if (_lastUpdateTime != null && _resultInterface?.CurrentState != null)
					_resultInterface.Draw(Main.spriteBatch, _lastUpdateTime);
				return true;
			}, InterfaceScaleType.UI));
	}

	public static void Toggle()
	{
		BingoUISystem system = ModContent.GetInstance<BingoUISystem>();
		if (system._interface?.CurrentState == null)
			system.Show();
		else
			system.Hide(false);
	}

	public static void SetValidationFailure(BingoValidationError error, int cellIndex)
	{
		BingoUISystem system = ModContent.GetInstance<BingoUISystem>();
		system._menu?.SetValidationFailure(new BingoValidationFailure(error, cellIndex));
	}

	internal static void SetInventoryActionFailure(BingoGame.InventoryActionError error)
	{
		BingoUISystem system = ModContent.GetInstance<BingoUISystem>();
		system._menu?.SetInventoryActionFailure(error);
	}

	private void Show()
	{
		if (_interface == null || _menu == null || Main.gameMenu)
			return;
		_menu.Open();
		_interface.SetState(_menu);
	}

	private void ShowResult()
	{
		if (_resultInterface == null || _result == null || Main.gameMenu || !BingoWorldSystem.HasBoard)
			return;
		_result.Add(BingoResultSnapshot.Capture());
		if (_resultInterface.CurrentState == null)
			_resultInterface.SetState(_result);
	}

	private void HideResult()
	{
		_resultInterface?.SetState(null);
		_result?.Clear();
	}

	private void Hide(bool forced)
	{
		if (_interface?.CurrentState != null && _menu != null && !_menu.TrySavePersistentState(!forced))
			return;
		BingoNumericInput.ClearFocus();
		BingoTextInput.ClearFocus(false);
		_interface?.SetState(null);
	}

	private void HideAll(bool forced)
	{
		Hide(forced);
		HideResult();
	}
}
