using System.Collections.Generic;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private void BuildResultWindow()
	{
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Result, 400f, 300f, 460f, 360f);
		UIVerticalStack root = CreateRootStack(8f, 8f);
		BingoAdaptiveText title = CreateText(panel, ResultText(), 0.5f, 0.5f, 1.15f,
			BingoTextRole.Title, ResultColor());
		BingoScrollList standings = new(6f);
		IReadOnlyList<BingoTeamStanding> teamStandings = BingoWorldSystem.GetTeamStandings();
		if (teamStandings.Count == 0)
		{
			BingoAdaptiveText empty = CreateText(panel, Text("UI.NoClaims"), 0.5f, 0.5f, 0.86f,
				BingoTextRole.Compact, Color.Silver);
			standings.AddRow(empty, 40f);
		}
		else
		{
			foreach (BingoTeamStanding standing in teamStandings)
			{
				BingoAdaptiveText row = CreateText(panel,
					Text("UI.TeamRanking", standing.Rank, BingoTeamDisplay.GetName(standing.Team), standing.Score),
					0.5f, 0.5f, 0.9f, BingoTextRole.Compact, BingoTeamDisplay.GetColor(standing.Team));
				standings.AddRow(row, 40f);
			}
		}

		UIHorizontalStack footer = new(12f);
		footer.AddWeighted(CreateButton(panel, Text("UI.Close"), BingoUISystem.Toggle));
		if (BingoWorldSystem.IsLocalPlayerHost)
			footer.AddWeighted(CreateButton(panel, Text("UI.NewGame"),
				() => BingoGame.RequestEnd(BingoEndAction.Reset), emphasized: true));
		root.AddFixed(title, 42f);
		root.AddWeighted(standings, 1f, 160f);
		root.AddFixed(footer, 48f);
		panel.Append(root);
		panel.Recalculate();
	}

	private static string ResultText()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
			return Text("UI.InProgress");
		if (BingoWorldSystem.IsDraw)
			return Text("UI.Draw");
		return Text("UI.TeamWon", BingoTeamDisplay.GetName(BingoWorldSystem.WinningTeam));
	}

	private static Color ResultColor()
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress || BingoWorldSystem.IsDraw)
			return Color.White;
		return BingoBoardElement.GetTeamColor(BingoWorldSystem.WinningTeam);
	}
}

