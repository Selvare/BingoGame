using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BingoGame.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
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

public enum BingoFinishReason : byte
{
	None,
	Natural,
	TimeLimit,
	Manual
}

public enum BingoEndAction : byte
{
	Settle,
	Cancel,
	Reset
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
	NotEnoughWhitelistItems,
	InvalidInitialItems,
	InvalidTimeLimit,
	InvalidAdvancedOptions
}

public readonly record struct BingoValidationFailure(BingoValidationError Error, int CellIndex);

public readonly record struct BingoClaimRecord(byte Team, string PlayerName, int ItemType, long ElapsedTicks,
	int Order);

public readonly record struct BingoTeamStanding(int Rank, byte Team, int Score);

public sealed record BingoContributionStanding(int Rank, byte Team, string PlayerName, int FirstOrder,
	IReadOnlyList<int> ItemTypes);

public sealed class BingoWorldSystem : ModSystem
{
	public const byte SinglePlayerTeam = 2;
	private const int SaveVersion = 5;
	private const long TicksPerSecond = 60;
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
	public static bool PlayersHaveInitialItems { get; private set; }
	public static long ElapsedTicks { get; private set; }
	public static long TimeLimitTicks { get; private set; }
	public static BingoFinishReason FinishReason { get; private set; }
	public static bool LineProgressTiebreakEnabled { get; private set; } = true;
	public static bool LineAutoDegradeEnabled { get; private set; } = true;
	public static bool KillStealEnabled { get; private set; }
	public static float KillStealChance { get; private set; }
	public static bool RandomStartEnabled { get; private set; }
	public static bool RandomStartTeamTogether { get; private set; }
	public static bool ForcePvpEnabled { get; private set; }
	public static bool FogOfWarEnabled { get; private set; }
	public static int[] InitialItemTypes { get; private set; } = EmptyInts;
	public static IReadOnlyList<BingoClaimRecord> Claims => _claims;
	public static int StateRevision { get; private set; }

	private static Dictionary<int, int> _cellByItemType = new();
	private static long[] _connectionOrder = Array.Empty<long>();
	private static long _nextConnectionOrder;
	private static List<BingoClaimRecord> _claims = new();
	private static ulong _clientSyncUpdateCount;
	private static Dictionary<byte, int> _randomStartLeaders = new();
	private static int _randomStartFollowerDelay;

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
		PlayersHaveInitialItems = false;
		ElapsedTicks = 0;
		TimeLimitTicks = 0;
		FinishReason = BingoFinishReason.None;
		LineProgressTiebreakEnabled = true;
		LineAutoDegradeEnabled = true;
		KillStealEnabled = false;
		KillStealChance = 0f;
		RandomStartEnabled = false;
		RandomStartTeamTogether = false;
		ForcePvpEnabled = false;
		FogOfWarEnabled = false;
		InitialItemTypes = EmptyInts;
		_claims = new List<BingoClaimRecord>();
		_clientSyncUpdateCount = Main.GameUpdateCount;
		_cellByItemType = new Dictionary<int, int>();
		_connectionOrder = new long[Main.maxPlayers];
		_nextConnectionOrder = 0;
		_randomStartLeaders = new Dictionary<byte, int>();
		_randomStartFollowerDelay = 0;
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

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || Phase != BingoGamePhase.InProgress)
			return;

		if (ElapsedTicks < long.MaxValue)
			ElapsedTicks++;
		UpdateRandomStartFollowers();
		if (TimeLimitTicks > 0 && ElapsedTicks >= TimeLimitTicks)
		{
			FinishForced(BingoFinishReason.TimeLimit);
			StateChanged();
		}
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
		tag["PlayersHaveInitialItems"] = PlayersHaveInitialItems;
		tag["ElapsedTicks"] = ElapsedTicks;
		tag["TimeLimitTicks"] = TimeLimitTicks;
		tag["FinishReason"] = (int)FinishReason;
		tag["LineProgressTiebreakEnabled"] = LineProgressTiebreakEnabled;
		tag["LineAutoDegradeEnabled"] = LineAutoDegradeEnabled;
		tag["KillStealEnabled"] = KillStealEnabled;
		tag["KillStealChance"] = KillStealChance;
		tag["RandomStartEnabled"] = RandomStartEnabled;
		tag["RandomStartTeamTogether"] = RandomStartTeamTogether;
		tag["ForcePvpEnabled"] = ForcePvpEnabled;
		tag["FogOfWarEnabled"] = FogOfWarEnabled;
		List<ItemDefinition> initialDefinitions = new(InitialItemTypes.Length);
		foreach (int itemType in InitialItemTypes)
			initialDefinitions.Add(new ItemDefinition(itemType));
		tag["InitialItemDefinitions"] = initialDefinitions;
		List<TagCompound> claims = new(_claims.Count);
		foreach (BingoClaimRecord claim in _claims)
		{
			claims.Add(new TagCompound
			{
				["Team"] = (int)claim.Team,
				["PlayerName"] = claim.PlayerName,
				["ItemType"] = claim.ItemType,
				["ElapsedTicks"] = claim.ElapsedTicks,
				["Order"] = claim.Order
			});
		}
		tag["Claims"] = claims;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		int size = tag.GetInt("BoardSize");
		if (size is < 2 or > 10)
			return;

		int count = size * size;
		int version = tag.GetInt("Version");
		int[] loadedTypes;
		if (version >= 2)
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
		Phase = version >= 2
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
		PlayersHaveInitialItems = tag.GetBool("PlayersHaveInitialItems");
		ElapsedTicks = version >= 3 ? Math.Max(0L, tag.GetLong("ElapsedTicks")) : 0L;
		TimeLimitTicks = version >= 3 ? Math.Max(0L, tag.GetLong("TimeLimitTicks")) : 0L;
		int savedFinishReason = tag.GetInt("FinishReason");
		FinishReason = version >= 3
			&& savedFinishReason is >= byte.MinValue and <= byte.MaxValue
			&& Enum.IsDefined(typeof(BingoFinishReason), (byte)savedFinishReason)
			? (BingoFinishReason)savedFinishReason
			: Phase == BingoGamePhase.Finished ? BingoFinishReason.Natural : BingoFinishReason.None;
		LineProgressTiebreakEnabled = version < 5 || tag.GetBool("LineProgressTiebreakEnabled");
		LineAutoDegradeEnabled = version < 5 || tag.GetBool("LineAutoDegradeEnabled");
		KillStealEnabled = version >= 4 && tag.GetBool("KillStealEnabled");
		KillStealChance = version >= 4 ? Math.Clamp(tag.GetFloat("KillStealChance"), 0f, 1f) : 0f;
		RandomStartEnabled = version >= 4 && tag.GetBool("RandomStartEnabled");
		RandomStartTeamTogether = version >= 4 && tag.GetBool("RandomStartTeamTogether");
		ForcePvpEnabled = version >= 4 && tag.GetBool("ForcePvpEnabled");
		FogOfWarEnabled = version >= 4 && tag.GetBool("FogOfWarEnabled");
		if (version >= 4)
		{
			IList<ItemDefinition> initialDefinitions = tag.GetList<ItemDefinition>("InitialItemDefinitions");
			List<int> initialTypes = new(initialDefinitions.Count);
			foreach (ItemDefinition definition in initialDefinitions)
			{
				if (!definition.IsUnloaded && IsUsableItemId(definition.Type))
					initialTypes.Add(definition.Type);
			}
			InitialItemTypes = initialTypes.ToArray();
		}
		else InitialItemTypes = EmptyInts;
		_claims = new List<BingoClaimRecord>(count);
		HashSet<int> loadedClaimItems = new();
		if (version >= 3)
		{
			foreach (TagCompound claimTag in tag.GetList<TagCompound>("Claims"))
			{
				if (_claims.Count >= count)
					break;
				byte team = (byte)Math.Clamp(claimTag.GetInt("Team"), 0, 5);
				string playerName = NormalizePlayerName(claimTag.GetString("PlayerName"));
				int itemType = claimTag.GetInt("ItemType");
				long claimTicks = Math.Clamp(claimTag.GetLong("ElapsedTicks"), 0L, ElapsedTicks);
				int order = Math.Max(0, claimTag.GetInt("Order"));
				int cell = Array.IndexOf(ItemTypes, itemType);
				if (team > 0 && playerName.Length > 0 && cell >= 0 && Owners[cell] == team
					&& loadedClaimItems.Add(itemType))
					_claims.Add(new BingoClaimRecord(team, playerName, itemType, claimTicks, order));
			}
		}

		if (invalidContent && Phase != BingoGamePhase.NotStarted)
			ResetGameCore();
		else if (Phase == BingoGamePhase.NotStarted)
		{
			ClearCompletionState();
			ElapsedTicks = 0;
			TimeLimitTicks = 0;
			_claims.Clear();
		}

		RebuildLookup();
		_clientSyncUpdateCount = Main.GameUpdateCount;
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
		writer.Write(PlayersHaveInitialItems);
		writer.Write(ElapsedTicks);
		writer.Write(TimeLimitTicks);
		writer.Write((byte)FinishReason);
		writer.Write(LineProgressTiebreakEnabled);
		writer.Write(LineAutoDegradeEnabled);
		writer.Write(KillStealEnabled);
		writer.Write(KillStealChance);
		writer.Write(RandomStartEnabled);
		writer.Write(RandomStartTeamTogether);
		writer.Write(ForcePvpEnabled);
		writer.Write(FogOfWarEnabled);
		writer.Write((ushort)Math.Min(InitialItemTypes.Length, ushort.MaxValue));
		for (int i = 0; i < InitialItemTypes.Length && i < ushort.MaxValue; i++)
			writer.Write(InitialItemTypes[i]);
		writer.Write((ushort)ItemTypes.Length);
		for (int i = 0; i < ItemTypes.Length; i++)
		{
			writer.Write(ItemTypes[i]);
			writer.Write(Owners[i]);
		}
		writer.Write((byte)Math.Min(_claims.Count, byte.MaxValue));
		for (int i = 0; i < _claims.Count && i < byte.MaxValue; i++)
		{
			BingoClaimRecord claim = _claims[i];
			writer.Write(claim.Team);
			writer.Write(claim.PlayerName);
			writer.Write(claim.ItemType);
			writer.Write(claim.ElapsedTicks);
			writer.Write(claim.Order);
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
		bool playersHaveInitialItems = reader.ReadBoolean();
		long elapsedTicks = reader.ReadInt64();
		long timeLimitTicks = reader.ReadInt64();
		BingoFinishReason finishReason = (BingoFinishReason)reader.ReadByte();
		bool lineProgressTiebreakEnabled = reader.ReadBoolean();
		bool lineAutoDegradeEnabled = reader.ReadBoolean();
		bool killStealEnabled = reader.ReadBoolean();
		float killStealChance = reader.ReadSingle();
		bool randomStartEnabled = reader.ReadBoolean();
		bool randomStartTeamTogether = reader.ReadBoolean();
		bool forcePvpEnabled = reader.ReadBoolean();
		bool fogOfWarEnabled = reader.ReadBoolean();
		int initialItemCount = reader.ReadUInt16();
		int[] initialItemTypes = new int[initialItemCount];
		for (int i = 0; i < initialItemCount; i++)
			initialItemTypes[i] = reader.ReadInt32();
		int count = reader.ReadUInt16();

		bool valid = Enum.IsDefined(typeof(BingoGamePhase), phase)
			&& Enum.IsDefined(typeof(BingoWinRule), rule)
			&& size is >= 2 and <= 10
			&& Enum.IsDefined(typeof(BingoFinishReason), finishReason)
			&& elapsedTicks >= 0 && timeLimitTicks >= 0
			&& killStealChance is >= 0f and <= 1f
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
		PlayersHaveInitialItems = playersHaveInitialItems;
		ElapsedTicks = elapsedTicks;
		TimeLimitTicks = timeLimitTicks;
		FinishReason = finishReason;
		LineProgressTiebreakEnabled = lineProgressTiebreakEnabled;
		LineAutoDegradeEnabled = lineAutoDegradeEnabled;
		KillStealEnabled = killStealEnabled;
		KillStealChance = Math.Clamp(killStealChance, 0f, 1f);
		RandomStartEnabled = randomStartEnabled;
		RandomStartTeamTogether = randomStartTeamTogether;
		ForcePvpEnabled = forcePvpEnabled;
		FogOfWarEnabled = fogOfWarEnabled;
		HashSet<int> sanitizedInitialItemTypes = new();
		foreach (int itemType in initialItemTypes)
		{
			if (IsUsableItemId(itemType))
				sanitizedInitialItemTypes.Add(itemType);
		}
		InitialItemTypes = sanitizedInitialItemTypes.ToArray();
		ItemTypes = new int[count];
		Owners = new byte[count];
		for (int i = 0; i < count; i++)
		{
			ItemTypes[i] = reader.ReadInt32();
			byte owner = reader.ReadByte();
			Owners[i] = owner <= 5 ? owner : (byte)0;
		}
		int claimCount = reader.ReadByte();
		_claims = new List<BingoClaimRecord>(Math.Min(claimCount, count));
		HashSet<int> receivedClaimItems = new();
		for (int i = 0; i < claimCount; i++)
		{
			byte team = reader.ReadByte();
			string playerName = NormalizePlayerName(reader.ReadString());
			int itemType = reader.ReadInt32();
			long claimTicks = reader.ReadInt64();
			int order = reader.ReadInt32();
			int cell = Array.IndexOf(ItemTypes, itemType);
			if (_claims.Count < count && team is >= 1 and <= 5 && playerName.Length > 0
				&& claimTicks is >= 0 && claimTicks <= elapsedTicks && cell >= 0 && Owners[cell] == team
				&& receivedClaimItems.Add(itemType))
				_claims.Add(new BingoClaimRecord(team, playerName, itemType, claimTicks, Math.Max(0, order)));
		}
		RebuildLookup();
		_clientSyncUpdateCount = Main.GameUpdateCount;
		StateRevision++;
	}

	internal static bool TryStartGame(int requester, int size, BingoWinRule rule, IReadOnlyList<int> requestedTypes,
		bool whitelistEnabled, IReadOnlyList<int> whitelistTypes, IReadOnlyList<int> initialItemTypes,
		bool timeLimitEnabled, int timeLimitMinutes, int timeLimitSeconds, bool lineProgressTiebreakEnabled,
		bool lineAutoDegradeEnabled, bool killStealEnabled, float killStealChance, bool randomStartEnabled,
		bool randomStartTeamTogether, bool forcePvpEnabled, bool fogOfWarEnabled,
		out BingoValidationFailure failure)
	{
		failure = default;
		if (!IsPlayerHost(requester))
			return Fail(BingoValidationError.NotHost, -1, out failure);
		if (Phase is not (BingoGamePhase.NotStarted or BingoGamePhase.Finished))
			return Fail(BingoValidationError.InvalidPhase, -1, out failure);
		if (size is < 2 or > 10)
			return Fail(BingoValidationError.InvalidSize, -1, out failure);
		if (!Enum.IsDefined(typeof(BingoWinRule), rule))
			return Fail(BingoValidationError.InvalidRule, -1, out failure);
		if (requestedTypes.Count != size * size)
			return Fail(BingoValidationError.InvalidCellCount, -1, out failure);
		if (!TryCalculateTimeLimit(timeLimitEnabled, timeLimitMinutes, timeLimitSeconds, out long timeLimitTicks))
			return Fail(BingoValidationError.InvalidTimeLimit, -1, out failure);
		if (killStealChance is < 0f or > 1f || float.IsNaN(killStealChance))
			return Fail(BingoValidationError.InvalidAdvancedOptions, -1, out failure);

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

		HashSet<int> initialItemSnapshot = new();
		foreach (int itemType in initialItemTypes)
		{
			if (IsUsableItemId(itemType))
				initialItemSnapshot.Add(itemType);
		}

		HashSet<int> excludedInitialItems = new();
		if (PlayersHaveInitialItems)
		{
			foreach (int itemType in initialItemTypes)
			{
				if (!IsUsableItemId(itemType) || !excludedInitialItems.Add(itemType))
					return Fail(BingoValidationError.InvalidInitialItems, -1, out failure);
			}
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
				if (!selected.Contains(itemType) && !excludedInitialItems.Contains(itemType))
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
				if (IsUsableItemId(pair.Key) && !selected.Contains(pair.Key)
					&& !excludedInitialItems.Contains(pair.Key))
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
		ElapsedTicks = 0;
		TimeLimitTicks = timeLimitTicks;
		LineProgressTiebreakEnabled = rule == BingoWinRule.Line && lineProgressTiebreakEnabled;
		LineAutoDegradeEnabled = rule == BingoWinRule.Line && lineAutoDegradeEnabled;
		KillStealEnabled = killStealEnabled;
		KillStealChance = Math.Clamp(killStealChance, 0f, 1f);
		RandomStartEnabled = randomStartEnabled;
		RandomStartTeamTogether = randomStartTeamTogether;
		ForcePvpEnabled = forcePvpEnabled;
		FogOfWarEnabled = fogOfWarEnabled;
		InitialItemTypes = initialItemSnapshot.ToArray();
		_claims = new List<BingoClaimRecord>(prepared.Length);
		Phase = BingoGamePhase.InProgress;
		ClearCompletionState();
		RebuildLookup();
		ScheduleRandomStart();
		StateChanged();
		return true;
	}

	internal static bool TryEndGame(int requester, BingoEndAction action)
	{
		if (!IsPlayerHost(requester) || !Enum.IsDefined(typeof(BingoEndAction), action))
			return false;

		switch (action)
		{
			case BingoEndAction.Settle when Phase == BingoGamePhase.InProgress:
				FinishForced(BingoFinishReason.Manual);
				break;
			case BingoEndAction.Cancel when Phase == BingoGamePhase.InProgress:
				ResetGameCore();
				break;
			case BingoEndAction.Reset when Phase == BingoGamePhase.Finished:
				ResetGameCore();
				break;
			default:
				return false;
		}
		StateChanged();
		return true;
	}

	internal static void SetPlayersHaveInitialItems(bool value)
	{
		if (PlayersHaveInitialItems == value)
			return;
		PlayersHaveInitialItems = value;
		StateChanged();
	}

	public static void TryClaimInventory(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || Phase != BingoGamePhase.InProgress
			|| !TryGetEffectiveTeam(player, out byte team))
			return;

		bool changed = false;
		for (int slot = 0; slot < player.inventory.Length; slot++)
		{
			Item item = player.inventory[slot];
			if (item.IsAir || !_cellByItemType.TryGetValue(item.type, out int cell) || Owners[cell] != 0)
				continue;
			Owners[cell] = team;
			BingoClaimRecord claim = new(team, NormalizePlayerName(player.name), item.type, ElapsedTicks, _claims.Count);
			_claims.Add(claim);
			BroadcastClaim(claim);
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

	public static long GetDisplayElapsedTicks()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient || Phase != BingoGamePhase.InProgress)
			return ElapsedTicks;

		ulong delta = Main.GameUpdateCount >= _clientSyncUpdateCount
			? Main.GameUpdateCount - _clientSyncUpdateCount
			: 0;
		long added = delta > long.MaxValue ? long.MaxValue : (long)delta;
		long displayed = ElapsedTicks > long.MaxValue - added ? long.MaxValue : ElapsedTicks + added;
		return TimeLimitTicks > 0 ? Math.Min(displayed, TimeLimitTicks) : displayed;
	}

	public static string FormatElapsed(long ticks)
	{
		long totalSeconds = Math.Max(0L, ticks) / TicksPerSecond;
		return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
	}

	public static IReadOnlyList<BingoTeamStanding> GetTeamStandings()
	{
		List<(byte Team, int Score)> scores = new();
		for (byte team = 1; team <= 5; team++)
		{
			int score = GetTeamScore(team);
			if (score > 0)
				scores.Add((team, score));
		}
		scores.Sort((left, right) =>
		{
			if (WinRule == BingoWinRule.Line && !IsDraw)
			{
				bool leftWinner = left.Team == WinningTeam;
				bool rightWinner = right.Team == WinningTeam;
				if (leftWinner != rightWinner)
					return leftWinner ? -1 : 1;
			}
			int scoreOrder = right.Score.CompareTo(left.Score);
			return scoreOrder != 0 ? scoreOrder : left.Team.CompareTo(right.Team);
		});

		List<BingoTeamStanding> result = new(scores.Count);
		for (int i = 0; i < scores.Count; i++)
		{
			int rank;
			if (i == 0)
				rank = 1;
			else if (scores[i].Score == scores[i - 1].Score
				&& !(WinRule == BingoWinRule.Line && !IsDraw
					&& scores[i - 1].Team == WinningTeam))
				rank = result[i - 1].Rank;
			else
				rank = i + 1;
			result.Add(new BingoTeamStanding(rank, scores[i].Team, scores[i].Score));
		}
		return result;
	}

	public static IReadOnlyList<BingoContributionStanding> GetContributionStandings()
	{
		Dictionary<(byte Team, string PlayerName), ContributionBuilder> grouped = new();
		foreach (BingoClaimRecord claim in _claims)
		{
			var key = (claim.Team, claim.PlayerName);
			if (!grouped.TryGetValue(key, out ContributionBuilder builder))
			{
				builder = new ContributionBuilder(claim.Team, claim.PlayerName, claim.Order);
				grouped.Add(key, builder);
			}
			builder.ItemTypes.Add(claim.ItemType);
		}

		List<ContributionBuilder> builders = new(grouped.Values);
		builders.Sort((left, right) =>
		{
			int countOrder = right.ItemTypes.Count.CompareTo(left.ItemTypes.Count);
			if (countOrder != 0)
				return countOrder;
			int firstOrder = left.FirstOrder.CompareTo(right.FirstOrder);
			if (firstOrder != 0)
				return firstOrder;
			int teamOrder = left.Team.CompareTo(right.Team);
			return teamOrder != 0 ? teamOrder : string.CompareOrdinal(left.PlayerName, right.PlayerName);
		});

		List<BingoContributionStanding> result = new(builders.Count);
		for (int i = 0; i < builders.Count; i++)
		{
			int rank = i > 0 && builders[i].ItemTypes.Count == builders[i - 1].ItemTypes.Count
				? result[i - 1].Rank
				: i + 1;
			ContributionBuilder builder = builders[i];
			result.Add(new BingoContributionStanding(rank, builder.Team, builder.PlayerName, builder.FirstOrder,
				builder.ItemTypes.ToArray()));
		}
		return result;
	}

	private static void InitializeDraft(int size, BingoWinRule rule)
	{
		BoardSize = Math.Clamp(size, 2, 10);
		WinRule = Enum.IsDefined(typeof(BingoWinRule), rule) ? rule : BingoWinRule.Line;
		ItemTypes = new int[BoardSize * BoardSize];
		Owners = new byte[ItemTypes.Length];
		Phase = BingoGamePhase.NotStarted;
		ElapsedTicks = 0;
		TimeLimitTicks = 0;
		_claims = new List<BingoClaimRecord>();
		ClearAdvancedOptions();
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
				ResetGameCore();
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

	internal static bool IsPlayerHost(int requester) => Main.netMode == NetmodeID.SinglePlayer
		|| Main.netMode == NetmodeID.Server && requester == HostPlayerId && IsEligibleLocalHost(requester);

	internal static bool CanKillStealDrop(int victimPlayerId, int killerPlayerId)
	{
		if (Main.netMode != NetmodeID.Server || Phase != BingoGamePhase.InProgress || !KillStealEnabled
			|| KillStealChance <= 0f || victimPlayerId == killerPlayerId
			|| victimPlayerId is < 0 or >= Main.maxPlayers || killerPlayerId is < 0 or >= Main.maxPlayers)
			return false;

		Player victim = Main.player[victimPlayerId];
		Player killer = Main.player[killerPlayerId];
		return victim.active && killer.active
			&& TryGetEffectiveTeam(victim, out byte victimTeam)
			&& TryGetEffectiveTeam(killer, out byte killerTeam)
			&& victimTeam != killerTeam;
	}

	private static bool TryGetEffectiveTeam(Player player, out byte team)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			team = SinglePlayerTeam;
			return true;
		}

		if (player.team is >= 1 and <= 5)
		{
			team = (byte)player.team;
			return true;
		}

		team = 0;
		return false;
	}

	internal static bool IsUsableItemId(int id)
	{
		if (!IsExistingItemId(id))
			return false;
		Item sample = ContentSamples.ItemsByType[id];
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

	internal static bool IsExistingItemId(int id) => ContentSamples.ItemsByType.TryGetValue(id, out Item sample)
		&& !sample.IsAir;

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

	private static bool TryCalculateTimeLimit(bool enabled, int minutes, int seconds, out long ticks)
	{
		ticks = 0;
		if (!enabled)
			return true;
		if (minutes < 0 || seconds is < 0 or > 59 || minutes == 0 && seconds == 0)
			return false;

		try
		{
			ticks = checked(((long)minutes * 60L + seconds) * TicksPerSecond);
			return ticks > 0;
		}
		catch (OverflowException)
		{
			return false;
		}
	}

	private static void EvaluateWinner(byte latestTeam)
	{
		if (WinRule == BingoWinRule.Line)
		{
			if (HasCompletedLine(latestTeam))
			{
				FinishGame(BingoFinishReason.Natural, latestTeam, false);
				return;
			}

			if (AllCellsClaimed())
				FinishLineDrawBreakTie(BingoFinishReason.Natural);
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
		bool draw = tied || bestTeam == 0;
		FinishGame(BingoFinishReason.Natural, draw ? (byte)0 : bestTeam, draw);
	}

	private static void FinishForced(BingoFinishReason reason)
	{
		if (Phase != BingoGamePhase.InProgress)
			return;

		if (WinRule == BingoWinRule.Line)
		{
			byte completedTeam = 0;
			bool multiple = false;
			for (byte team = 1; team <= 5; team++)
			{
				if (!HasCompletedLine(team))
					continue;
				if (completedTeam != 0)
					multiple = true;
				else
					completedTeam = team;
			}
			if (!multiple && completedTeam != 0)
				FinishGame(reason, completedTeam, false);
			else
				FinishLineDrawBreakTie(reason);
			return;
		}

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
		bool draw = tied || bestTeam == 0;
		FinishGame(reason, draw ? (byte)0 : bestTeam, draw);
	}

	private static void FinishGame(BingoFinishReason reason, byte winningTeam, bool draw)
	{
		if (Phase != BingoGamePhase.InProgress)
			return;
		Phase = BingoGamePhase.Finished;
		FinishReason = reason;
		WinningTeam = draw ? (byte)0 : winningTeam;
		IsDraw = draw;
		BroadcastContributionRanking();
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

	private static void FinishLineDrawBreakTie(BingoFinishReason reason)
	{
		if (!LineProgressTiebreakEnabled && !LineAutoDegradeEnabled)
		{
			FinishGame(reason, 0, true);
			return;
		}

		int bestProgress = LineProgressTiebreakEnabled ? 0 : -1;
		int bestScore = LineAutoDegradeEnabled ? 0 : -1;
		byte bestTeam = 0;
		bool tied = false;
		for (byte team = 1; team <= 5; team++)
		{
			int progress = LineProgressTiebreakEnabled ? GetBestLineProgress(team) : -1;
			int score = LineAutoDegradeEnabled ? GetTeamScore(team) : -1;
			if (progress > bestProgress || progress == bestProgress && score > bestScore)
			{
				bestProgress = progress;
				bestScore = score;
				bestTeam = team;
				tied = false;
			}
			else if (progress == bestProgress && score == bestScore
				&& (!LineProgressTiebreakEnabled || progress > 0)
				&& (!LineAutoDegradeEnabled || score > 0))
				tied = true;
		}

		bool draw = tied || bestTeam == 0;
		FinishGame(reason, draw ? (byte)0 : bestTeam, draw);
	}

	private static int GetBestLineProgress(byte team)
	{
		if (BoardSize <= 0 || Owners.Length != BoardSize * BoardSize)
			return 0;

		int best = 0;
		for (int row = 0; row < BoardSize; row++)
		{
			int progress = 0;
			for (int column = 0; column < BoardSize; column++)
				progress += Owners[row * BoardSize + column] == team ? 1 : 0;
			best = Math.Max(best, progress);
		}
		for (int column = 0; column < BoardSize; column++)
		{
			int progress = 0;
			for (int row = 0; row < BoardSize; row++)
				progress += Owners[row * BoardSize + column] == team ? 1 : 0;
			best = Math.Max(best, progress);
		}

		int mainDiagonal = 0;
		int antiDiagonal = 0;
		for (int i = 0; i < BoardSize; i++)
		{
			mainDiagonal += Owners[i * BoardSize + i] == team ? 1 : 0;
			antiDiagonal += Owners[i * BoardSize + BoardSize - 1 - i] == team ? 1 : 0;
		}
		return Math.Max(best, Math.Max(mainDiagonal, antiDiagonal));
	}

	private static void ResetGameCore()
	{
		Phase = BingoGamePhase.NotStarted;
		Owners = new byte[ItemTypes.Length];
		ElapsedTicks = 0;
		TimeLimitTicks = 0;
		_claims = new List<BingoClaimRecord>();
		ClearAdvancedOptions();
		_randomStartLeaders.Clear();
		_randomStartFollowerDelay = 0;
		ClearCompletionState();
		RebuildLookup();
	}

	private static void ScheduleRandomStart()
	{
		_randomStartLeaders.Clear();
		_randomStartFollowerDelay = 0;
		if (!RandomStartEnabled)
			return;

		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			Main.LocalPlayer.TeleportationPotion();
			return;
		}
		if (Main.netMode != NetmodeID.Server)
			return;

		if (!RandomStartTeamTogether)
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (Main.player[i].active)
					global::BingoGame.BingoGame.SendApplyRandomTeleport(i);
			}
			return;
		}

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (!player.active || !TryGetEffectiveTeam(player, out byte team) || _randomStartLeaders.ContainsKey(team))
				continue;
			_randomStartLeaders.Add(team, i);
			global::BingoGame.BingoGame.SendApplyRandomTeleport(i);
		}
		if (_randomStartLeaders.Count > 0)
			_randomStartFollowerDelay = 120;
	}

	private static void UpdateRandomStartFollowers()
	{
		if (Main.netMode != NetmodeID.Server || _randomStartFollowerDelay <= 0)
			return;
		_randomStartFollowerDelay--;
		if (_randomStartFollowerDelay > 0)
			return;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (!player.active || !TryGetEffectiveTeam(player, out byte team)
				|| !_randomStartLeaders.TryGetValue(team, out int leaderId) || leaderId == i)
				continue;
			Player leader = Main.player[leaderId];
			if (!leader.active)
				continue;
			Vector2 destination = leader.position + new Vector2((i % 3 - 1) * 48f, 0f);
			player.Teleport(destination, 0);
			NetMessage.SendData(MessageID.TeleportEntity, number: 0, number2: i, number3: destination.X,
				number4: destination.Y, number5: 0);
		}
		_randomStartLeaders.Clear();
	}

	private static void ClearAdvancedOptions()
	{
		LineProgressTiebreakEnabled = true;
		LineAutoDegradeEnabled = true;
		KillStealEnabled = false;
		KillStealChance = 0f;
		RandomStartEnabled = false;
		RandomStartTeamTogether = false;
		ForcePvpEnabled = false;
		FogOfWarEnabled = false;
		InitialItemTypes = EmptyInts;
	}

	private static void ClearCompletionState()
	{
		WinningTeam = 0;
		IsDraw = false;
		FinishReason = BingoFinishReason.None;
	}

	private static void BroadcastClaim(BingoClaimRecord claim)
	{
		NetworkText text = Main.netMode == NetmodeID.SinglePlayer
			? NetworkText.FromKey("Mods.BingoGame.Chat.SingleItemClaim",
				FormatElapsed(claim.ElapsedTicks), claim.PlayerName, $"[i:{claim.ItemType}]",
				Lang.GetItemName(claim.ItemType).ToNetworkText(), claim.ItemType)
			: NetworkText.FromKey("Mods.BingoGame.Chat.ItemClaim",
				FormatElapsed(claim.ElapsedTicks), TeamNetworkText(claim.Team), claim.PlayerName,
				$"[i:{claim.ItemType}]", Lang.GetItemName(claim.ItemType).ToNetworkText(), claim.ItemType);
		BroadcastChat(text, BingoTeamDisplay.GetColor(claim.Team));
	}

	private static void BroadcastContributionRanking()
	{
		bool singlePlayer = Main.netMode == NetmodeID.SinglePlayer;
		BroadcastChat(NetworkText.FromKey(singlePlayer
			? "Mods.BingoGame.Chat.SingleContributionHeader"
			: "Mods.BingoGame.Chat.ContributionHeader"), Color.White);
		foreach (BingoContributionStanding standing in GetContributionStandings())
		{
			List<string> tags = new(standing.ItemTypes.Count);
			foreach (int itemType in standing.ItemTypes)
				tags.Add($"[i:{itemType}]");
			string itemTags = string.Join(" ", tags);
			NetworkText text = singlePlayer
				? NetworkText.FromKey("Mods.BingoGame.Chat.SingleContributionEntry", standing.PlayerName,
					standing.ItemTypes.Count, itemTags)
				: NetworkText.FromKey("Mods.BingoGame.Chat.ContributionEntry", standing.Rank,
					TeamNetworkText(standing.Team), standing.PlayerName, standing.ItemTypes.Count, itemTags);
			BroadcastChat(text, BingoTeamDisplay.GetColor(standing.Team));
		}
	}

	private static NetworkText TeamNetworkText(int team) =>
		NetworkText.FromKey(BingoTeamDisplay.GetLocalizationKey(team));

	private static void BroadcastChat(NetworkText text, Color color)
	{
		if (Main.netMode == NetmodeID.Server)
			ChatHelper.BroadcastChatMessage(text, color);
		else if (Main.netMode == NetmodeID.SinglePlayer)
			Main.NewText(text.ToString(), color);
	}

	private static string NormalizePlayerName(string playerName)
	{
		string normalized = (playerName ?? string.Empty).Trim();
		return normalized.Length <= 64 ? normalized : normalized[..64];
	}

	private sealed class ContributionBuilder
	{
		public byte Team { get; }
		public string PlayerName { get; }
		public int FirstOrder { get; }
		public List<int> ItemTypes { get; } = new();

		public ContributionBuilder(byte team, string playerName, int firstOrder)
		{
			Team = team;
			PlayerName = playerName;
			FirstOrder = firstOrder;
		}
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
