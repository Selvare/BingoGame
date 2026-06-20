using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BingoGame.Common.Configs;

public sealed class BingoGameConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(BingoWinRule.Line)]
	public BingoWinRule DraftWinRule;

	public List<int> DraftItemTypes = new();

	[DefaultValue(false)]
	public bool WhitelistEnabled;

	public List<BingoWhitelistEntry> Whitelists = new();

	public override ModConfig Clone()
	{
		BingoGameConfig clone = (BingoGameConfig)base.Clone();
		clone.DraftItemTypes = new List<int>(DraftItemTypes ?? new List<int>());
		List<BingoWhitelistEntry> sourceWhitelists = Whitelists ?? new List<BingoWhitelistEntry>();
		clone.Whitelists = new List<BingoWhitelistEntry>(sourceWhitelists.Count);
		foreach (BingoWhitelistEntry whitelist in sourceWhitelists)
			clone.Whitelists.Add(whitelist?.Clone() ?? new BingoWhitelistEntry());
		return clone;
	}
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

	internal BingoWhitelistEntry Clone() => new()
	{
		Name = Name ?? string.Empty,
		Enabled = Enabled,
		ItemTypes = new List<int>(ItemTypes ?? new List<int>())
	};
}
