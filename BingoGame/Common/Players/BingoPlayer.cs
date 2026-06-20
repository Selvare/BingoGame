using BingoGame.Common.Systems;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BingoGame.Common.Players;

public sealed class BingoPlayer : ModPlayer
{
	private int _inventoryCheckTimer;

	public override void PostUpdate()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || ++_inventoryCheckTimer < 15)
			return;

		_inventoryCheckTimer = 0;
		BingoWorldSystem.TryClaimInventory(Player);
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (BingoGame.ToggleBoardKeybind?.JustPressed == true && !BingoUISystem.IsEditingText)
			BingoUISystem.Toggle();
	}
}
