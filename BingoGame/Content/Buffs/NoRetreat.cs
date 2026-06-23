using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using BingoGame.Common.Systems;

namespace BingoGame.Content.Buffs;

public sealed class NoRetreat : ModBuff
{
	internal const int DurationTicks = 5 * 60;

	public override string Texture => $"Terraria/Images/Buff_{BuffID.Obstructed}";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.pvpBuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	internal static void ApplyToPvpVictim(Player attacker, Player target)
	{
		if (!BingoWorldSystem.CanApplyNoRetreat(attacker, target))
			return;

		target.AddBuff(ModContent.BuffType<NoRetreat>(), DurationTicks, quiet: false);
	}

	internal static bool IsActive(Player player)
	{
		return player != null && player.active && player.HasBuff(ModContent.BuffType<NoRetreat>());
	}
}

public sealed class NoRetreatTeleportSystem : ModSystem
{
	public override void Load()
	{
		Terraria.On_Player.Teleport += PreventNoRetreatTeleport;
	}

	public override void Unload()
	{
		Terraria.On_Player.Teleport -= PreventNoRetreatTeleport;
	}

	private static void PreventNoRetreatTeleport(Terraria.On_Player.orig_Teleport orig, Player self,
		Vector2 newPos, int Style, int extraInfo)
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.Preparing || NoRetreat.IsActive(self))
			return;

		orig(self, newPos, Style, extraInfo);
	}
}

public sealed class NoRetreatPlayer : ModPlayer
{
	public override bool CanUseItem(Item item)
	{
		return !NoRetreat.IsActive(Player) || !IsTeleportItem(item);
	}

	private static bool IsTeleportItem(Item item)
	{
		return item != null && item.type is
			ItemID.RecallPotion
			or ItemID.TeleportationPotion
			or ItemID.PotionOfReturn
			or ItemID.WormholePotion
			or ItemID.MagicMirror
			or ItemID.IceMirror
			or ItemID.CellPhone
			or ItemID.MagicConch
			or ItemID.DemonConch
			or ItemID.Shellphone
			or ItemID.ShellphoneSpawn
			or ItemID.ShellphoneOcean
			or ItemID.ShellphoneHell
			or ItemID.ShellphoneDummy
			or ItemID.RodofDiscord
			or ItemID.RodOfHarmony;
	}
}
