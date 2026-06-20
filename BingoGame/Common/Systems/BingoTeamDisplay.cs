using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace BingoGame.Common.Systems;

internal static class BingoTeamDisplay
{
	public static string GetLocalizationKey(int team) => $"Mods.BingoGame.UI.TeamNames.{GetKey(team)}";

	public static string GetName(int team)
		=> Language.GetTextValue(GetLocalizationKey(team));

	public static Color GetColor(int team) => team switch
	{
		1 => new Color(230, 70, 70),
		2 => new Color(80, 210, 90),
		3 => new Color(75, 135, 235),
		4 => new Color(235, 210, 65),
		5 => new Color(225, 105, 205),
		_ => Color.Gray
	};

	private static string GetKey(int team) => team switch
	{
		1 => "Red",
		2 => "Green",
		3 => "Blue",
		4 => "Yellow",
		5 => "Pink",
		_ => "Red"
	};
}
