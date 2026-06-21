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
		_menu.EnsureConfigDefaults();
		_lastObservedPhase = BingoWorldSystem.Phase;
	}

	public override void OnWorldLoad() => Hide(true);

	public override void OnWorldUnload() => Hide(true);

	public override void Unload()
	{
		BingoNumericInput.ClearFocus();
		BingoTextInput.ClearFocus(false);
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
			Hide(true);
			return;
		}
		if (_lastObservedPhase != BingoWorldSystem.Phase)
		{
			_lastObservedPhase = BingoWorldSystem.Phase;
			if (_lastObservedPhase == BingoGamePhase.Finished && _interface?.CurrentState == null)
				Show();
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

	private void Hide(bool forced)
	{
		if (_interface?.CurrentState != null && _menu != null && !_menu.TrySavePersistentState(!forced))
			return;
		BingoNumericInput.ClearFocus();
		BingoTextInput.ClearFocus(false);
		_interface?.SetState(null);
	}
}

