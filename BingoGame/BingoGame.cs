using System.IO;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BingoGame;

public sealed class BingoGame : Mod
{
	internal enum PacketType : byte
	{
		RequestStart,
		RequestStop,
		StartRejected
	}

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
			case PacketType.RequestStop when Main.netMode == NetmodeID.Server:
				BingoWorldSystem.TryStopGame(whoAmI);
				break;
			case PacketType.StartRejected when Main.netMode == NetmodeID.MultiplayerClient:
				BingoUISystem.SetValidationFailure((BingoValidationError)reader.ReadByte(), reader.ReadInt16());
				break;
			default:
				Logger.Warn($"Ignored invalid BINGO packet {type} from {whoAmI}.");
				break;
		}
	}

	internal static void RequestStart(int size, BingoWinRule rule, int[] itemTypes)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			if (!BingoWorldSystem.TryStartGame(Main.myPlayer, size, rule, itemTypes, out BingoValidationFailure failure))
				BingoUISystem.SetValidationFailure(failure.Error, failure.CellIndex);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestStart);
		packet.Write((byte)size);
		packet.Write((byte)rule);
		packet.Write((ushort)itemTypes.Length);
		foreach (int itemType in itemTypes)
			packet.Write(itemType);
		packet.Send();
	}

	internal static void RequestStop()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			BingoWorldSystem.TryStopGame(Main.myPlayer);
			return;
		}

		ModPacket packet = ModContent.GetInstance<BingoGame>().GetPacket();
		packet.Write((byte)PacketType.RequestStop);
		packet.Send();
	}

	private static void ReceiveStartRequest(BinaryReader reader, int whoAmI)
	{
		int size = reader.ReadByte();
		BingoWinRule rule = (BingoWinRule)reader.ReadByte();
		int count = reader.ReadUInt16();
		if (count > 100)
		{
			SendStartRejected(whoAmI, new BingoValidationFailure(BingoValidationError.InvalidCellCount, -1));
			return;
		}

		int[] itemTypes = new int[count];
		for (int i = 0; i < count; i++)
			itemTypes[i] = reader.ReadInt32();

		if (!BingoWorldSystem.TryStartGame(whoAmI, size, rule, itemTypes, out BingoValidationFailure failure))
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
}
