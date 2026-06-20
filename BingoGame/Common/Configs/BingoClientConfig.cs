using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BingoGame.Common.Configs;

public sealed class BingoClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(0.2f)]
	[Range(0f, 1f)]
	[Increment(0.05f)]
	[Slider]
	public float InProgressUIOpacity;

	[DefaultValue(0.8f)]
	[Range(0f, 1f)]
	[Increment(0.05f)]
	[Slider]
	public float InactiveUIOpacity;

	[DefaultValue(620)]
	public int SettingsWidth;

	[DefaultValue(430)]
	public int SettingsHeight;

	[DefaultValue(420)]
	public int WaitingWidth;

	[DefaultValue(170)]
	public int WaitingHeight;

	[DefaultValue(350)]
	public int EditorWidth;

	[DefaultValue(435)]
	public int EditorHeight;

	[DefaultValue(360)]
	public int GameWidth;

	[DefaultValue(470)]
	public int GameHeight;

	[DefaultValue(0)]
	[Range(0, 10)]
	public int DraftBoardSize;

	[DefaultValue(BingoWinRule.Line)]
	public BingoWinRule DraftWinRule;

	public List<int> DraftItemTypes = new();
}
