using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BingoGame.Common.Configs;

public sealed class BingoClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

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
}
