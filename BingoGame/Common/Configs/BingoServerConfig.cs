using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BingoGame.Common.Configs;

public enum BingoWinRule
{
	Line,
	Majority
}

public sealed class BingoServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(5)]
	[Range(2, 10)]
	public int DefaultBoardSize;

	[DefaultValue(BingoWinRule.Line)]
	public BingoWinRule DefaultWinRule;
}
