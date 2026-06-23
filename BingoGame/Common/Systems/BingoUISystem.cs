using System.Collections.Generic;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class BingoUISystem : ModSystem
{
	private BingoUIManager _uiManager;
	private GameTime _lastUpdateTime;
	private BingoGamePhase _lastObservedPhase;

	public static bool IsEditingText => BingoNumericInput.AnyFocused || BingoTextInput.AnyFocused;

	public override void PostSetupContent()
	{
		BingoClientConfig clientConfig = ModContent.GetInstance<BingoClientConfig>();
		clientConfig.MigrateLegacyDraft(ModContent.GetInstance<BingoGameConfig>());
		
		// 初始化新的 UI 系统
		BingoUIManager.CreateInstance();
		_uiManager = BingoUIManager.Instance;
		
		_lastObservedPhase = BingoWorldSystem.Phase;
	}

	public override void OnWorldLoad()
	{
		_uiManager?.Hide();
		_lastObservedPhase = BingoWorldSystem.Phase;
	}

	public override void OnWorldUnload()
	{
		_uiManager?.Hide();
		_lastObservedPhase = BingoGamePhase.NotStarted;
	}

	public override void Unload()
	{
		BingoNumericInput.ClearFocus();
		BingoTextInput.ClearFocus(false);
		_uiManager?.Dispose();
		_uiManager = null;
		_lastUpdateTime = null;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastUpdateTime = gameTime;
		
		if (Main.gameMenu)
		{
			_uiManager?.Hide();
			return;
		}
		
		if (_lastObservedPhase != BingoWorldSystem.Phase)
		{
			_lastObservedPhase = BingoWorldSystem.Phase;
			if (_lastObservedPhase != BingoGamePhase.NotStarted)
			{
				_uiManager?.Show();
			}
			else
			{
				_uiManager?.Hide();
			}
		}

		_uiManager?.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
			return;

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"BingoGame: UI",
			() =>
			{
				if (_lastUpdateTime != null && _uiManager != null)
					_uiManager.Draw(_lastUpdateTime);
				return true;
			}, InterfaceScaleType.UI));
	}

	public static void Toggle()
	{
		BingoUIManager.Instance?.Toggle();
	}

	public static void SetValidationFailure(BingoValidationError error, int cellIndex)
	{
		if (BingoUIManager.Instance?.Context?.ViewState != null)
		{
			BingoUIManager.Instance.Context.ViewState.ValidationFailure = new BingoValidationFailure(error, cellIndex);
		}
	}

	internal static void SetInventoryActionFailure(BingoGame.InventoryActionError error)
	{
		if (BingoUIManager.Instance?.Context?.ViewState != null)
		{
			BingoUIManager.Instance.Context.ViewState.InventoryActionError = error.ToString();
		}
	}
}
