using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BingoGame.Common.Configs;

public sealed class BingoGameConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(BingoWinRule.Line)]
	public BingoWinRule DraftWinRule;

	[DefaultValue(true)]
	public bool LineProgressTiebreakEnabled = true;

	[DefaultValue(true)]
	public bool LineAutoDegradeEnabled = true;

	public List<int> DraftItemTypes = new();

	[DefaultValue(false)]
	public bool WhitelistEnabled;

	[DefaultValue(false)]
	public bool TimeLimitEnabled;

	[DefaultValue(0)]
	public int TimeLimitMinutes;

	[DefaultValue(0)]
	[Range(0, 59)]
	public int TimeLimitSeconds;

	[DefaultValue(false)]
	public bool KillStealEnabled;

	[DefaultValue(0.25f)]
	[Range(0f, 1f)]
	[Increment(0.05f)]
	[Slider]
	public float KillStealChance;

	[DefaultValue(false)]
	public bool RandomStartEnabled;

	[DefaultValue(false)]
	public bool RandomStartTeamTogether;

	[DefaultValue(false)]
	public bool ForcePvpEnabled;

	[DefaultValue(false)]
	public bool NoRetreatEnabled;

	[DefaultValue(false)]
	public bool FogOfWarEnabled;

	public List<BingoWhitelistEntry> Whitelists = new();

	public List<BingoInitialItemListEntry> InitialItemLists = CreateDefaultInitialItemLists();

	public override ModConfig Clone()
	{
		BingoGameConfig clone = (BingoGameConfig)base.Clone();
		clone.DraftItemTypes = new List<int>(DraftItemTypes ?? new List<int>());
		List<BingoWhitelistEntry> sourceWhitelists = Whitelists ?? new List<BingoWhitelistEntry>();
		clone.Whitelists = new List<BingoWhitelistEntry>(sourceWhitelists.Count);
		foreach (BingoWhitelistEntry whitelist in sourceWhitelists)
			clone.Whitelists.Add(whitelist?.Clone() ?? new BingoWhitelistEntry());
		List<BingoInitialItemListEntry> sourceInitialItemLists = InitialItemLists
			?? new List<BingoInitialItemListEntry>();
		clone.InitialItemLists = new List<BingoInitialItemListEntry>(sourceInitialItemLists.Count);
		foreach (BingoInitialItemListEntry initialItemList in sourceInitialItemLists)
			clone.InitialItemLists.Add(initialItemList?.Clone() ?? new BingoInitialItemListEntry());
		return clone;
	}

	private static List<BingoInitialItemListEntry> CreateDefaultInitialItemLists() => new()
	{
		new BingoInitialItemListEntry
		{
			Enabled = true,
			Items = new List<BingoInitialItemStack>
			{
				new(29, 15),
				new(109, 4),
				new(724, 1),
				new(165, 1),
				new(1320, 1),
				new(3506, 1),
				new(88, 1),
				new(410, 1),
				new(411, 1),
				new(50, 1),
				new(2997, 300),
				new(4759, 1),
				new(898, 1),
				new(53, 1),
				new(8, 9999)
			}
		}
	};
}

public sealed class BingoWhitelistEntry
{
	[LabelKey("$Mods.BingoGame.Configs.BingoWhitelistEntry.Name.Label")]
	public string Name = string.Empty;

	[DefaultValue(false)]
	[LabelKey("$Mods.BingoGame.Configs.BingoWhitelistEntry.Enabled.Label")]
	public bool Enabled;

	[LabelKey("$Mods.BingoGame.Configs.BingoWhitelistEntry.ItemTypes.Label")]
	public List<int> ItemTypes = new();

	[LabelKey("$Mods.BingoGame.Configs.BingoWhitelistEntry.RawText.Label")]
	public string RawText = string.Empty;

	internal BingoWhitelistEntry Clone() => new()
	{
		Name = Name ?? string.Empty,
		Enabled = Enabled,
		ItemTypes = new List<int>(ItemTypes ?? new List<int>()),
		RawText = RawText ?? string.Empty
	};
}

public sealed class BingoInitialItemListEntry
{
	[LabelKey("$Mods.BingoGame.Configs.BingoInitialItemListEntry.Name.Label")]
	public string Name = string.Empty;

	[DefaultValue(false)]
	[LabelKey("$Mods.BingoGame.Configs.BingoInitialItemListEntry.Enabled.Label")]
	public bool Enabled;

	[LabelKey("$Mods.BingoGame.Configs.BingoInitialItemListEntry.Items.Label")]
	public List<BingoInitialItemStack> Items = new();

	internal BingoInitialItemListEntry Clone() => new()
	{
		Name = Name ?? string.Empty,
		Enabled = Enabled,
		Items = CloneItems(Items)
	};

	private static List<BingoInitialItemStack> CloneItems(List<BingoInitialItemStack> source)
	{
		List<BingoInitialItemStack> clone = new();
		foreach (BingoInitialItemStack item in source ?? new List<BingoInitialItemStack>())
			clone.Add(item?.Clone() ?? new BingoInitialItemStack());
		return clone;
	}
}

public sealed class BingoInitialItemStack
{
	[LabelKey("$Mods.BingoGame.Configs.BingoInitialItemStack.ItemType.Label")]
	public int ItemType;

	[DefaultValue(1)]
	[Range(1, 9999)]
	[LabelKey("$Mods.BingoGame.Configs.BingoInitialItemStack.Stack.Label")]
	public int Stack = 1;

	public BingoInitialItemStack()
	{
	}

	public BingoInitialItemStack(int itemType, int stack)
	{
		ItemType = itemType;
		Stack = stack;
	}

	internal BingoInitialItemStack Clone() => new(ItemType, Stack);
}
