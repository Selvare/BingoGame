using System.Linq;
using BingoGame.Common.Configs;
using BingoGame.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BingoGame.Common.Commands;

public sealed class BingoStatusCommand : ModCommand
{
	public override string Command => "bingo";
	public override CommandType Type => CommandType.World | CommandType.Console;
	public override string Usage => "/bingo [status]";
	public override string Description => Language.GetTextValue("Mods.BingoGame.Commands.StatusDescription");

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (args.Length > 1 || args.Length == 1 && args[0] != "status")
			throw new UsageException(Usage);
		ReplyStatus(caller);
	}

	private static void ReplyStatus(CommandCaller caller)
	{
		if (BingoWorldSystem.Phase == BingoGamePhase.NotStarted)
		{
			caller.Reply(Language.GetTextValue("Mods.BingoGame.Commands.NotStarted"), Color.LightGray);
			return;
		}

		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			string singleResult = BingoWorldSystem.Phase == BingoGamePhase.Finished
				? Language.GetTextValue(BingoWorldSystem.FinishReason == BingoFinishReason.Natural
					? "Mods.BingoGame.UI.ChallengeSuccess"
					: "Mods.BingoGame.UI.ChallengeIncomplete")
				: Language.GetTextValue("Mods.BingoGame.UI.InProgress");
			string score = Language.GetTextValue("Mods.BingoGame.UI.PersonalScore",
				BingoWorldSystem.GetTeamScore(BingoWorldSystem.SinglePlayerTeam));
			caller.Reply(Language.GetTextValue("Mods.BingoGame.Commands.Status", BingoWorldSystem.BoardSize,
				RuleName(BingoWorldSystem.WinRule), singleResult, score), Color.LightBlue);
			return;
		}

		string scores = string.Join(Language.GetTextValue("Mods.BingoGame.Commands.ScoreSeparator"), Enumerable.Range(1, 5)
			.Select(team => Language.GetTextValue("Mods.BingoGame.UI.TeamScore", BingoTeamDisplay.GetName(team),
				BingoWorldSystem.GetTeamScore(team))));
		string result = BingoWorldSystem.Phase == BingoGamePhase.Finished
			? BingoWorldSystem.IsDraw
				? Language.GetTextValue("Mods.BingoGame.UI.Draw")
				: Language.GetTextValue("Mods.BingoGame.UI.TeamWon", BingoTeamDisplay.GetName(BingoWorldSystem.WinningTeam))
			: Language.GetTextValue("Mods.BingoGame.UI.InProgress");
		caller.Reply(Language.GetTextValue("Mods.BingoGame.Commands.Status", BingoWorldSystem.BoardSize,
			RuleName(BingoWorldSystem.WinRule), result, scores), Color.LightBlue);
	}

	private static string RuleName(BingoWinRule rule) => Language.GetTextValue(rule == BingoWinRule.Line
		? "Mods.BingoGame.UI.RuleLine" : "Mods.BingoGame.UI.RuleMajority");
}
