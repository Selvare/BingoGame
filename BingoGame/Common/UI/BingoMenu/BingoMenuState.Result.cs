using Microsoft.Xna.Framework;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private static string ResultText()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.Preparing)
			return Text("UI.Preparation");
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
			return Text("UI.InProgress");
		if (BingoWorldSystem.IsDraw)
			return Text("UI.Draw");
		return Text("UI.TeamWon", BingoTeamDisplay.GetName(BingoWorldSystem.WinningTeam));
	}

	private static Color ResultColor()
	{
		if (BingoWorldSystem.Phase is BingoGamePhase.Preparing or BingoGamePhase.InProgress
			|| BingoWorldSystem.IsDraw)
			return Color.White;
		return BingoBoardElement.GetTeamColor(BingoWorldSystem.WinningTeam);
	}
}
