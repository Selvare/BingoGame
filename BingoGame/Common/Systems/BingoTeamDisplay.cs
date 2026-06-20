using Terraria.Localization;

namespace BingoGame.Common.Systems;

internal static class BingoTeamDisplay
{
	public static string GetName(int team)
	{
		string key = team switch
		{
			1 => "Red",
			2 => "Green",
			3 => "Blue",
			4 => "Yellow",
			5 => "Pink",
			_ => "Red"
		};
		return Language.GetTextValue($"Mods.BingoGame.UI.TeamNames.{key}");
	}
}
