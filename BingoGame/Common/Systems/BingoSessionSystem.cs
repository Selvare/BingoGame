using System;
using System.Collections.Generic;
using System.IO;
using BingoGame.Common.Configs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BingoGame.Common.Systems;

public enum BingoGamePhase : byte
{
	NotStarted,
	InProgress,
	Finished
}

public enum BingoValidationError : byte
{
	None,
	NotHost,
	InvalidPhase,
	InvalidSize,
	InvalidRule,
	InvalidCellCount,
	InvalidItem,
	DuplicateItem,
	NotEnoughItems,
	InvalidWhitelist,
	NotEnoughWhitelistItems
}

public readonly record struct BingoValidationFailure(BingoValidationError Error, int CellIndex);

public sealed class BingoWorldSystem : ModSystem
{
	private const int SaveVersion = 2;
	private static readonly int[] EmptyInts = Array.Empty<int>();
	private static readonly byte[] EmptyBytes = Array.Empty<byte>();

	public static BingoGamePhase Phase { get; private set; }
	public static int HostPlayerId { get; private set; } = -1;
	public static int BoardSize { get; private set; }
	public static BingoWinRule WinRule { get; private set; }
	public static int[] ItemTypes { get; private set; } = EmptyInts;
	public static byte[] Owners { get; private set; } = EmptyBytes;
	public static byte WinningTeam { get; private set; }
	public static bool IsDraw { get; private set; }
	public static int StateRevision { get; private set; }

	private static Dictionary<int, int> _cellByItemType = new();
	private static long[] _connectionOrder = Array.Empty<long>();
	private static long _nextConnectionOrder;

	public static bool HasBoard => BoardSize is >= 2 and <= 10
		&& ItemTypes.Length == BoardSize * BoardSize
		&& Owners.Length == ItemTypes.Length;
	public static bool IsFinished => Phase == BingoGamePhase.Finished;
	public static bool IsLocalPlayerHost => Main.netMode == NetmodeID.SinglePlayer
		|| Main.myPlayer >= 0 && Main.myPlayer == HostPlayerId;

	public override void ClearWorld()
	{
		Phase = BingoGamePhase.NotStarted;
		HostPlayerId = -1;
		BoardSize = 0;
		WinRule = BingoWinRule.Line;
		ItemTypes = EmptyInts;
		Owners = EmptyBytes;
		WinningTeam = 0;
		IsDraw = false;
		_cellByItemType = new Dictionary<int, int>();
		_connectionOrder = new long[Main.maxPlayers];
		_nextConnectionOrder = 0;
		StateRevision++;
	}

	public override void OnWorldLoad()
	{
		InitializeDraft(5, BingoWinRule.Line);
	}

	public override void OnWorldUnload() => ClearWorld();

	public override void PreUpdateWorld()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			UpdateHostPlayer();
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (!HasBoard)
			return;

		tag["Version"] = SaveVersion;
		tag["Phase"] = (int)Phase;
		tag["BoardSize"] = BoardSize;
		tag["WinRule"] = (int)WinRule;
		List<ItemDefinition> definitions = new(ItemTypes.Length);
		foreach (int itemType in ItemTypes)
			definitions.Add(new ItemDefinition(itemType));
		tag["ItemDefinitions"] = definitions;
		tag["Owners"] = Array.ConvertAll(Owners, owner => (int)owner);
		tag["WinningTeam"] = (int)WinningTeam;
		tag["IsDraw"] = IsDraw;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		int size = tag.GetInt("BoardSize");
		if (size is < 2 or > 10)
			return;

		int count = size * size;
		int version = tag.GetInt("Version");
		int[] loadedTypes;
		if (version >= SaveVersion)
		{
			IList<ItemDefinition> definitions = tag.GetList<ItemDefinition>("ItemDefinitions");
			if (definitions.Count != count)
				return;
			loadedTypes = new int[count];
			for (int i = 0; i < count; i++)
				loadedTypes[i] = definitions[i].IsUnloaded ? -1 : definitions[i].Type;
		}
		else
		{
			IList<int> oldTypes = tag.GetList<int>("ItemTypes");
			if (oldTypes.Count != count)
				return;
			loadedTypes = new int[count];
			oldTypes.CopyTo(loadedTypes, 0);
		}

		BoardSize = size;
		int savedRule = tag.GetInt("WinRule");
		WinRule = Enum.IsDefined(typeof(BingoWinRule), savedRule) ? (BingoWinRule)savedRule : BingoWinRule.Line;
		int savedPhase = tag.GetInt("Phase");
		Phase = version >= SaveVersion
			&& savedPhase is >= byte.MinValue and <= byte.MaxValue
			&& Enum.IsDefined(typeof(BingoGamePhase), (byte)savedPhase)
			? (BingoGamePhase)savedPhase
			: tag.GetBool("IsFinished") ? BingoGamePhase.Finished : BingoGamePhase.InProgress;

		bool invalidContent = SanitizeLoadedTypes(loadedTypes);
		ItemTypes = loadedTypes;
		Owners = new byte[count];
		IList<int> loadedOwners = tag.GetList<int>("Owners");
		if (loadedOwners.Count == count)
		{
			for (int i = 0; i < count; i++)
				Owners[i] = (byte)Math.Clamp(loadedOwners[i], 0, 5);
		}
		WinningTeam = (byte)Math.Clamp(tag.GetInt("WinningTeam"), 0, 5);
		IsDraw = tag.GetBool("IsDraw");

		if (invalidContent && Phase != BingoGamePhase.NotStarted)
			StopGameCore();
		else if (Phase == BingoGamePhase.NotStarted)
			ClearCompletionState();

		RebuildLookup();
		StateRevision++;
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)Phase);
		writer.Write((short)HostPlayerId);
		writer.Write((byte)BoardSize);
		writer.Write((byte)WinRule);
		writer.Write(WinningTeam);
		writer.Write(IsDraw);
		writer.Write((ushort)ItemTypes.Length);
		for (int i = 0; i < ItemTypes.Length; i++)
		{
			writer.Write(ItemTypes[i]);
			writer.Write(Owners[i]);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		BingoGamePhase phase = (BingoGamePhase)reader.ReadByte();
		int host = reader.ReadInt16();
		int size = reader.ReadByte();
		BingoWinRule rule = (BingoWinRule)reader.ReadByte();
		byte winner = reader.ReadByte();
		bool draw = reader.ReadBoolean();
		int count = reader.ReadUInt16();

		bool valid = Enum.IsDefined(typeof(BingoGamePhase), phase)
			&& Enum.IsDefined(typeof(BingoWinRule), rule)
			&& size is >= 2 and <= 10
			&& count == size * size;
		if (!valid)
		{
			for (int i = 0; i < count; i++)
			{
				reader.ReadInt32();
				reader.ReadByte();
			}
			ClearWorld();
			return;
		}

		Phase = phase;
		HostPlayerId = host is >= 0 and < Main.maxPlayers ? host : -1;
		BoardSize = size;
		WinRule = rule;
		WinningTeam = winner <= 5 ? winner : (byte)0;
		IsDraw = draw;
		ItemTypes = new int[count];
		Owners = new byte[count];
		for (int i = 0; i < count; i++)
		{
			ItemTypes[i] = reader.ReadInt32();
			byte owner = reader.ReadByte();
			Owners[i] = owner <= 5 ? owner : (byte)0;
		}
		RebuildLookup();
		StateRevision++;
	}

	internal static bool TryStartGame(int requester, int size, BingoWinRule rule, IReadOnlyList<int> requestedTypes,
		bool whitelistEnabled, IReadOnlyList<int> whitelistTypes, out BingoValidationFailure failure)
	{
		failure = default;
		if (!IsPlayerHost(requester))
			return Fail(BingoValidationError.NotHost, -1, out failure);
		if (Phase != BingoGamePhase.NotStarted)
			return Fail(BingoValidationError.InvalidPhase, -1, out failure);
		if (size is < 2 or > 10)
			return Fail(BingoValidationError.InvalidSize, -1, out failure);
		if (!Enum.IsDefined(typeof(BingoWinRule), rule))
			return Fail(BingoValidationError.InvalidRule, -1, out failure);
		if (requestedTypes.Count != size * size)
			return Fail(BingoValidationError.InvalidCellCount, -1, out failure);

		int[] prepared = new int[requestedTypes.Count];
		HashSet<int> selected = new();
		for (int i = 0; i < requestedTypes.Count; i++)
		{
			int itemType = requestedTypes[i];
			if (itemType == 0)
				continue;
			if (!IsUsableItemId(itemType))
				return Fail(BingoValidationError.InvalidItem, i, out failure);
			if (!selected.Add(itemType))
				return Fail(BingoValidationError.DuplicateItem, i, out failure);
			prepared[i] = itemType;
		}

		List<int> candidates;
		if (whitelistEnabled)
		{
			HashSet<int> whitelistSeen = new();
			candidates = new List<int>(whitelistTypes.Count);
			foreach (int itemType in whitelistTypes)
			{
				if (!IsUsableItemId(itemType) || !whitelistSeen.Add(itemType))
					return Fail(BingoValidationError.InvalidWhitelist, -1, out failure);
				if (!selected.Contains(itemType))
					candidates.Add(itemType);
			}
			if (whitelistSeen.Count < prepared.Length)
				return Fail(BingoValidationError.NotEnoughWhitelistItems, -1, out failure);
		}
		else
		{
			candidates = new List<int>(ContentSamples.ItemsByType.Count);
			foreach (KeyValuePair<int, Item> pair in ContentSamples.ItemsByType)
			{
				if (IsUsableItemId(pair.Key) && !selected.Contains(pair.Key))
					candidates.Add(pair.Key);
			}
		}
		for (int i = 0; i < prepared.Length; i++)
		{
			if (prepared[i] != 0)
				continue;
			if (candidates.Count == 0)
				return Fail(BingoValidationError.NotEnoughItems, i, out failure);
			int candidateIndex = Main.rand.Next(candidates.Count);
			prepared[i] = candidates[candidateIndex];
			candidates.RemoveAt(candidateIndex);
		}

		BoardSize = size;
		WinRule = rule;
		ItemTypes = prepared;
		Owners = new byte[prepared.Length];
		Phase = BingoGamePhase.InProgress;
		ClearCompletionState();
		RebuildLookup();
		StateChanged();
		return true;
	}

	internal static bool TryStopGame(int requester)
	{
		if (!IsPlayerHost(requester) || Phase == BingoGamePhase.NotStarted)
			return false;
		StopGameCore();
		StateChanged();
		return true;
	}

	public static void TryClaimInventory(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || Phase != BingoGamePhase.InProgress || player.team is < 1 or > 5)
			return;

		byte team = (byte)player.team;
		bool changed = false;
		for (int slot = 0; slot < player.inventory.Length; slot++)
		{
			Item item = player.inventory[slot];
			if (item.IsAir || !_cellByItemType.TryGetValue(item.type, out int cell) || Owners[cell] != 0)
				continue;
			Owners[cell] = team;
			changed = true;
			EvaluateWinner(team);
			if (Phase == BingoGamePhase.Finished)
				break;
		}
		if (changed)
			StateChanged();
	}

	public static int GetTeamScore(int team)
	{
		if (team is < 1 or > 5)
			return 0;
		int score = 0;
		foreach (byte owner in Owners)
			score += owner == team ? 1 : 0;
		return score;
	}

	private static void InitializeDraft(int size, BingoWinRule rule)
	{
		BoardSize = Math.Clamp(size, 2, 10);
		WinRule = Enum.IsDefined(typeof(BingoWinRule), rule) ? rule : BingoWinRule.Line;
		ItemTypes = new int[BoardSize * BoardSize];
		Owners = new byte[ItemTypes.Length];
		Phase = BingoGamePhase.NotStarted;
		ClearCompletionState();
		RebuildLookup();
		StateRevision++;
	}

	private static void UpdateHostPlayer()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			if (HostPlayerId != Main.myPlayer)
			{
				HostPlayerId = Main.myPlayer;
				StateRevision++;
			}
			return;
		}

		bool changed = false;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (!Main.player[i].active)
			{
				_connectionOrder[i] = 0;
				continue;
			}
			if (_connectionOrder[i] == 0 && IsEligibleLocalHost(i))
				_connectionOrder[i] = ++_nextConnectionOrder;
		}

		if (HostPlayerId >= 0 && !IsEligibleLocalHost(HostPlayerId))
		{
			if (Phase != BingoGamePhase.NotStarted)
				StopGameCore();
			HostPlayerId = -1;
			changed = true;
		}

		if (HostPlayerId < 0)
		{
			long earliest = long.MaxValue;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (_connectionOrder[i] > 0 && _connectionOrder[i] < earliest && IsEligibleLocalHost(i))
				{
					earliest = _connectionOrder[i];
					HostPlayerId = i;
				}
			}
			changed |= HostPlayerId >= 0;
		}

		if (changed)
			StateChanged();
	}

	private static bool IsEligibleLocalHost(int playerId) => playerId is >= 0 and < Main.maxPlayers
		&& Main.player[playerId].active
		&& Netplay.Clients[playerId].State == 10
		&& NetMessage.DoesPlayerSlotCountAsAHost(playerId);

	private static bool IsPlayerHost(int requester) => Main.netMode == NetmodeID.SinglePlayer
		|| Main.netMode == NetmodeID.Server && requester == HostPlayerId && IsEligibleLocalHost(requester);

	internal static bool IsUsableItemId(int id)
	{
		if (!ContentSamples.ItemsByType.TryGetValue(id, out Item sample) || sample.IsAir)
			return false;
		if (ItemID.Sets.Deprecated[id])
			return false;
		if (ItemID.Sets.ItemsThatShouldNotBeInInventory[id])
			return false;
		if (ItemID.Sets.IsAPickup[id])
			return false;
		if (sample.ResearchUnlockCount <= 0)
			return false;
		return true;
	}

	private static bool SanitizeLoadedTypes(int[] types)
	{
		bool invalid = false;
		HashSet<int> seen = new();
		for (int i = 0; i < types.Length; i++)
		{
			int itemType = types[i];
			if (itemType == 0)
				continue;
			if (!IsUsableItemId(itemType) || !seen.Add(itemType))
			{
				types[i] = 0;
				invalid = true;
			}
		}
		return invalid;
	}

	private static bool Fail(BingoValidationError error, int cell, out BingoValidationFailure failure)
	{
		failure = new BingoValidationFailure(error, cell);
		return false;
	}

	private static void EvaluateWinner(byte latestTeam)
	{
		if (WinRule == BingoWinRule.Line)
		{
			if (HasCompletedLine(latestTeam))
			{
				Phase = BingoGamePhase.Finished;
				WinningTeam = latestTeam;
				return;
			}

			if (AllCellsClaimed())
			{
				Phase = BingoGamePhase.Finished;
				IsDraw = true;
				WinningTeam = 0;
			}
			return;
		}

		int latestScore = GetTeamScore(latestTeam);
		if (latestScore <= Owners.Length / 2 && !AllCellsClaimed())
			return;

		int bestScore = 0;
		byte bestTeam = 0;
		bool tied = false;
		for (byte team = 1; team <= 5; team++)
		{
			int score = GetTeamScore(team);
			if (score > bestScore)
			{
				bestScore = score;
				bestTeam = team;
				tied = false;
			}
			else if (score == bestScore && score > 0)
				tied = true;
		}
		Phase = BingoGamePhase.Finished;
		IsDraw = tied || bestTeam == 0;
		WinningTeam = IsDraw ? (byte)0 : bestTeam;
	}

	private static bool AllCellsClaimed()
	{
		foreach (byte owner in Owners)
		{
			if (owner == 0)
				return false;
		}
		return Owners.Length > 0;
	}

	private static bool HasCompletedLine(byte team)
	{
		for (int row = 0; row < BoardSize; row++)
		{
			bool complete = true;
			for (int column = 0; column < BoardSize; column++)
				complete &= Owners[row * BoardSize + column] == team;
			if (complete)
				return true;
		}
		for (int column = 0; column < BoardSize; column++)
		{
			bool complete = true;
			for (int row = 0; row < BoardSize; row++)
				complete &= Owners[row * BoardSize + column] == team;
			if (complete)
				return true;
		}

		bool mainDiagonal = true;
		bool antiDiagonal = true;
		for (int i = 0; i < BoardSize; i++)
		{
			mainDiagonal &= Owners[i * BoardSize + i] == team;
			antiDiagonal &= Owners[i * BoardSize + BoardSize - 1 - i] == team;
		}
		return mainDiagonal || antiDiagonal;
	}

	private static void StopGameCore()
	{
		Phase = BingoGamePhase.NotStarted;
		Owners = new byte[ItemTypes.Length];
		ClearCompletionState();
		RebuildLookup();
	}

	private static void ClearCompletionState()
	{
		WinningTeam = 0;
		IsDraw = false;
	}

	private static void RebuildLookup()
	{
		_cellByItemType = new Dictionary<int, int>(ItemTypes.Length);
		for (int i = 0; i < ItemTypes.Length; i++)
		{
			if (ItemTypes[i] > 0)
				_cellByItemType.TryAdd(ItemTypes[i], i);
		}
	}

	private static void StateChanged()
	{
		StateRevision++;
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.WorldData);
	}
}
