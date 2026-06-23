using BingoGame.Common.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BingoGame.Common.Players;

public sealed class BingoPlayer : ModPlayer
{
	private int _inventoryCheckTimer;
	private int _lastPvpAttacker = -1;
	private ulong _lastPvpHitUpdate;

	public override void PostUpdate()
	{
		ForcePvpIfNeeded();
		if (Main.netMode == NetmodeID.MultiplayerClient || ++_inventoryCheckTimer < 15)
			return;

		_inventoryCheckTimer = 0;
		BingoWorldSystem.TryClaimInventory(Player);
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (!info.PvP)
			return;
		int attacker = ResolveAttackingPlayer(info.DamageSource);
		if (attacker < 0 || attacker == Player.whoAmI)
			return;
		_lastPvpAttacker = attacker;
		_lastPvpHitUpdate = Main.GameUpdateCount;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer || !BingoWorldSystem.KillStealEnabled)
			return;

		int attacker = ResolveAttackingPlayer(damageSource);
		if (attacker < 0 && pvp && Main.GameUpdateCount - _lastPvpHitUpdate <= 300)
			attacker = _lastPvpAttacker;
		if (attacker >= 0 && attacker != Player.whoAmI)
			BingoGame.RequestKillStealDrop(attacker);
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (BingoGame.ToggleBoardKeybind?.JustPressed == true && !BingoUISystem.IsEditingText)
			BingoUISystem.Toggle();
	}

	private void ForcePvpIfNeeded()
	{
		if (BingoWorldSystem.Phase != BingoGamePhase.InProgress || !BingoWorldSystem.ForcePvpEnabled
			|| Player.hostile)
			return;

		Player.hostile = true;
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			NetMessage.SendData(MessageID.TogglePVP, number: Player.whoAmI);
		else if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.TogglePVP, number: Player.whoAmI);
	}

	private static int ResolveAttackingPlayer(PlayerDeathReason damageSource)
	{
		if (!damageSource.TryGetCausingEntity(out Entity entity))
			return -1;
		if (entity is Player player && player.whoAmI is >= 0 and < Main.maxPlayers)
			return player.whoAmI;
		if (entity is Projectile projectile && projectile.owner is >= 0 and < Main.maxPlayers)
			return projectile.owner;
		return -1;
	}
}
