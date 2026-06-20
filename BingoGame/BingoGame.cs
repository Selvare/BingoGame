using System;
using System.Collections.Generic;
using System.IO;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BingoGame;

public sealed class BingoGame : Mod
{
	internal enum PacketType : byte
	{
		RequestStart,
		RequestEnd,
		StartRejected,
		RequestGiveInitialItems,
		ApplyGiveInitialItems,
		RequestClearPlayerItems,
		ApplyClearPlayerItems,
		InventoryActionRejected
	}

	internal enum InventoryActionError : byte
	{
		NotHost,
		InvalidItems
	}

	private const int MaxInitialItemEntries = 512;
	private const int MaxInitialItemStack = 9999;

	internal static ModKeybind ToggleBoardKeybind { get; private set; }

	public override void Load()
	{
		if (!Main.dedServ)
			ToggleBoardKeybind = KeybindLoader.RegisterKeybind(this, "ToggleBoard", "B");
	}

	public override void Unload()
	{
		ToggleBoardKeybind = null;
	}

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		PacketType type = (PacketType)reader.ReadByte();
		switch (type)
		{
			case PacketType.RequestStart when Main.netMode == NetmodeID.Server:
				ReceiveStartRequest(reader, whoAmI);
				break;
			case PacketType.RequestEnd when Main.netMode == NetmodeID.Server:
				BingoWorldSystem.TryEndGame(whoAmI, (BingoEndAction)reader.ReadByte());
				break;
			case PacketType.StartRejected when Main.netMode == NetmodeID.MultiplayerClient:
				BingoUISystem.SetValidationFailure((BingoValidationError)reader.ReadByte(), reader.ReadInt16());
				break;
			case PacketType.RequestGiveInitialItems when Main.netMode == NetmodeID.Server:
				ReceiveGiveInitialItemsRequest(reader, whoAmI);
				break;
			case PacketType.ApplyGiveInitialItems when Main.netMode == NetmodeID.MultiplayerClient:
				ApplyInitialItems(ReadInitialItems(reader));
				break;
			case PacketType.RequestClearPlayerItems when Main.netMode == NetmodeID.Server:
				ReceiveClearPlayerItemsRequest(whoAmI);
				break;
			case PacketType.ApplyClearPlayerItems when Main.netMode == NetmodeID.MultiplayerClient:
				ClearLocalPlayerItems();
				break;
			case PacketType.InventoryActionRejected when Main.netMode == NetmodeID.MultiplayerClient:
				BingoUISystem.SetInventoryActionFailure((InventoryActionError)reader.ReadByte());
				break;
			default:
				Logger.Warn($"Ignored invalid BINGO packet {type} from {whoAmI}.");
				break;
		}
	}

	internal static void RequestStart(int size, BingoWinRule rule, int[] itemTypes, bool whitelistEnabled,
		int[] whitelistTypes, int[] initialItemTypes, bool timeLimitEnabled, int timeLimitMinutes,
		int timeLimitSeconds)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			if (!BingoWorldSystem.TryStartGame(Main.myPlayer, size, rule, itemTypes, whitelistEnabled, whitelistTypes,
				initialItemTypes, timeLimitEnabled, timeLimitMinutes, timeLimitSeconds,
				out BingoValidationFailure failure))
				BingoUISystem.SetValidationFailure(failure.Error, failure.CellIndex);
			return;
		}
		if (whitelistTypes.Length > ushort.MaxValue)
		{
			BingoUISystem.SetValidationFailure(BingoValidationError.InvalidWhitelist, -1);
			return;
		}
		if (initialItemTypes.Length > MaxInitialItemEntries)
		{
			BingoUISystem.SetValidationFailure(BingoValidationError.InvalidInitialItems, -1);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestStart);
		packet.Write((byte)size);
		packet.Write((byte)rule);
		packet.Write(timeLimitEnabled);
		packet.Write(timeLimitMinutes);
		packet.Write(timeLimitSeconds);
		packet.Write((ushort)itemTypes.Length);
		foreach (int itemType in itemTypes)
			packet.Write(itemType);
		packet.Write(whitelistEnabled);
		packet.Write((ushort)whitelistTypes.Length);
		foreach (int itemType in whitelistTypes)
			packet.Write(itemType);
		packet.Write((ushort)initialItemTypes.Length);
		foreach (int itemType in initialItemTypes)
			packet.Write(itemType);
		packet.Send();
	}

	internal static void RequestEnd(BingoEndAction action)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			BingoWorldSystem.TryEndGame(Main.myPlayer, action);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestEnd);
		packet.Write((byte)action);
		packet.Send();
	}

	internal static void RequestGiveInitialItems(IReadOnlyList<BingoInitialItemStack> items)
	{
		if (!ValidateInitialItems(items))
		{
			BingoUISystem.SetInventoryActionFailure(InventoryActionError.InvalidItems);
			return;
		}

		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			if (ApplyInitialItems(items))
				BingoWorldSystem.SetPlayersHaveInitialItems(true);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestGiveInitialItems);
		WriteInitialItems(packet, items);
		packet.Send();
	}

	internal static void RequestClearPlayerItems()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			if (ClearLocalPlayerItems())
				BingoWorldSystem.SetPlayersHaveInitialItems(false);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestClearPlayerItems);
		packet.Send();
	}

	private static void ReceiveStartRequest(BinaryReader reader, int whoAmI)
	{
		int size = reader.ReadByte();
		BingoWinRule rule = (BingoWinRule)reader.ReadByte();
		bool timeLimitEnabled = reader.ReadBoolean();
		int timeLimitMinutes = reader.ReadInt32();
		int timeLimitSeconds = reader.ReadInt32();
		int count = reader.ReadUInt16();
		if (count > 100)
		{
			SendStartRejected(whoAmI, new BingoValidationFailure(BingoValidationError.InvalidCellCount, -1));
			return;
		}

		int[] itemTypes = new int[count];
		for (int i = 0; i < count; i++)
			itemTypes[i] = reader.ReadInt32();
		bool whitelistEnabled = reader.ReadBoolean();
		int whitelistCount = reader.ReadUInt16();
		if (whitelistCount > ContentSamples.ItemsByType.Count)
		{
			SendStartRejected(whoAmI, new BingoValidationFailure(BingoValidationError.InvalidWhitelist, -1));
			return;
		}

		int[] whitelistTypes = new int[whitelistCount];
		for (int i = 0; i < whitelistCount; i++)
			whitelistTypes[i] = reader.ReadInt32();
		int initialItemCount = reader.ReadUInt16();
		if (initialItemCount > MaxInitialItemEntries)
		{
			SendStartRejected(whoAmI, new BingoValidationFailure(BingoValidationError.InvalidInitialItems, -1));
			return;
		}
		int[] initialItemTypes = new int[initialItemCount];
		for (int i = 0; i < initialItemCount; i++)
			initialItemTypes[i] = reader.ReadInt32();

		if (!BingoWorldSystem.TryStartGame(whoAmI, size, rule, itemTypes, whitelistEnabled, whitelistTypes,
			initialItemTypes, timeLimitEnabled, timeLimitMinutes, timeLimitSeconds,
			out BingoValidationFailure failure))
			SendStartRejected(whoAmI, failure);
	}

	private static void SendStartRejected(int toWho, BingoValidationFailure failure)
	{
		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.StartRejected);
		packet.Write((byte)failure.Error);
		packet.Write((short)failure.CellIndex);
		packet.Send(toWho);
	}

	private static void ReceiveGiveInitialItemsRequest(BinaryReader reader, int whoAmI)
	{
		IReadOnlyList<BingoInitialItemStack> items = ReadInitialItems(reader);
		if (!BingoWorldSystem.IsPlayerHost(whoAmI))
		{
			SendInventoryActionRejected(whoAmI, InventoryActionError.NotHost);
			return;
		}
		if (!ValidateInitialItems(items))
		{
			SendInventoryActionRejected(whoAmI, InventoryActionError.InvalidItems);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.ApplyGiveInitialItems);
		WriteInitialItems(packet, items);
		packet.Send();
		BingoWorldSystem.SetPlayersHaveInitialItems(true);
	}

	private static void ReceiveClearPlayerItemsRequest(int whoAmI)
	{
		if (!BingoWorldSystem.IsPlayerHost(whoAmI))
		{
			SendInventoryActionRejected(whoAmI, InventoryActionError.NotHost);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.ApplyClearPlayerItems);
		packet.Send();
		BingoWorldSystem.SetPlayersHaveInitialItems(false);
	}

	private static List<BingoInitialItemStack> ReadInitialItems(BinaryReader reader)
	{
		try
		{
			int count = reader.ReadUInt16();
			if (count > MaxInitialItemEntries)
				return new List<BingoInitialItemStack> { default };
			List<BingoInitialItemStack> items = new(count);
			for (int i = 0; i < count; i++)
				items.Add(new BingoInitialItemStack(reader.ReadInt32(), reader.ReadInt32()));
			return items;
		}
		catch (EndOfStreamException)
		{
			return new List<BingoInitialItemStack> { default };
		}
	}

	private static void WriteInitialItems(BinaryWriter writer, IReadOnlyList<BingoInitialItemStack> items)
	{
		writer.Write((ushort)items.Count);
		foreach (BingoInitialItemStack item in items)
		{
			writer.Write(item.ItemType);
			writer.Write(item.Stack);
		}
	}

	private static bool ValidateInitialItems(IReadOnlyList<BingoInitialItemStack> items)
	{
		if (items == null || items.Count > MaxInitialItemEntries)
			return false;
		HashSet<int> seen = new();
		foreach (BingoInitialItemStack item in items)
		{
			if (item == null)
				return false;
			if (!BingoWorldSystem.IsUsableItemId(item.ItemType)
				|| item.Stack is < 1 or > MaxInitialItemStack
				|| !seen.Add(item.ItemType))
				return false;
		}
		return true;
	}

	private static bool ApplyInitialItems(IReadOnlyList<BingoInitialItemStack> items)
	{
		if (!ValidateInitialItems(items) || Main.myPlayer is < 0 or >= Main.maxPlayers)
			return false;

		Player player = Main.LocalPlayer;
		IEntitySource source = player.GetSource_FromThis("BingoInitialItems");
		foreach (BingoInitialItemStack requested in items)
		{
			int remaining = requested.Stack;
			Item sample = ContentSamples.ItemsByType[requested.ItemType];
			while (remaining > 0)
			{
				Item item = new();
				item.SetDefaults(requested.ItemType);
				item.stack = Math.Min(remaining, Math.Max(1, sample.maxStack));
				remaining -= item.stack;
				Item overflow = player.GetItem(player.whoAmI, item,
					GetItemSettings.InventoryEntityToPlayerInventorySettings);
				if (!overflow.IsAir)
					player.QuickSpawnItem(source, overflow, overflow.stack);
			}
		}
		SyncVanillaSlots(player, player.inventory, 0);
		return true;
	}

	private static bool ClearLocalPlayerItems()
	{
		if (Main.myPlayer is < 0 or >= Main.maxPlayers)
			return false;

		Player player = Main.LocalPlayer;
		ClearItems(player.inventory);
		ClearItems(player.armor);
		ClearItems(player.dye);
		ClearItems(player.miscEquips);
		ClearItems(player.miscDyes);

		using (new Main.CurrentPlayerOverride(player))
		{
			foreach (ModAccessorySlot slot in ModContent.GetContent<ModAccessorySlot>())
			{
				slot.FunctionalItem.TurnToAir();
				slot.FunctionalItem.NetStateChanged();
				slot.VanityItem.TurnToAir();
				slot.VanityItem.NetStateChanged();
				slot.DyeItem.TurnToAir();
				slot.DyeItem.NetStateChanged();
			}
		}

		SyncVanillaSlots(player, player.inventory, 0);
		SyncVanillaSlots(player, player.armor, 59);
		SyncVanillaSlots(player, player.dye, 79);
		SyncVanillaSlots(player, player.miscEquips, 89);
		SyncVanillaSlots(player, player.miscDyes, 94);
		return true;
	}

	private static void ClearItems(Item[] items)
	{
		foreach (Item item in items)
		{
			item.TurnToAir();
			item.NetStateChanged();
		}
	}

	private static void SyncVanillaSlots(Player player, Item[] items, int offset)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		for (int slot = 0; slot < items.Length; slot++)
			NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: offset + slot);
	}

	private static void SendInventoryActionRejected(int toWho, InventoryActionError error)
	{
		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.InventoryActionRejected);
		packet.Write((byte)error);
		packet.Send(toWho);
	}
}
