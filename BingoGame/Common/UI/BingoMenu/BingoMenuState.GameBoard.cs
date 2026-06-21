using System;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	private void BuildGameBoard()
	{
		int size = BingoWorldSystem.BoardSize;
		bool host = BingoWorldSystem.IsLocalPlayerHost;
		float footerHeight = host ? 140f : 95f;
		float available = Math.Min(Main.screenWidth - 100f, Main.screenHeight - 190f);
		float defaultCellSize = Math.Clamp(available / size, 36f, 64f);
		float minimumWidth = Math.Max(360f, size * 36f + 40f);
		float minimumHeight = size * 36f + footerHeight;
		BingoResponsivePanel panel = CreateWindow(BingoWindowPage.Game, minimumWidth, minimumHeight,
			defaultCellSize * size + 40f, defaultCellSize * size + footerHeight);
		panel.Locked = _gameWindowLocked && BingoWorldSystem.Phase == BingoGamePhase.InProgress;
		UIVerticalStack root = CreateRootStack(6f, 8f);
		UIHorizontalStack header = new(8f);
		string title = BingoWorldSystem.WinRule == BingoWinRule.Line ? Text("UI.RuleLine") : Text("UI.RuleMajority");
		BingoAdaptiveText titleText = CreateText(panel, Text("UI.BoardTitle", title), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		_timerText = CreateText(panel, BingoWorldSystem.FormatElapsed(BingoWorldSystem.GetDisplayElapsedTicks()),
			0.5f, 0.5f, 0.92f, BingoTextRole.Compact);
		header.AddFixed(_timerText, 70f);
		header.AddWeighted(titleText, 1f, 120f);

		bool singlePlayer = Main.netMode == NetmodeID.SinglePlayer;
		BingoBoardElement board = new(size, BingoWorldSystem.ItemTypes, BingoWorldSystem.Owners,
			BingoWorldSystem.Claims, () => panel.BorderColor, singlePlayer);

		UIHorizontalStack scoreRow = new(6f);
		int visibleTeams = 0;
		for (int team = 1; team <= 5; team++)
		{
			int score = BingoWorldSystem.GetTeamScore(team);
			if (score <= 0)
				continue;
			string scoreText = singlePlayer
				? Text("UI.PersonalScore", score)
				: Text("UI.TeamScore", BingoTeamDisplay.GetName(team), score);
			BingoAdaptiveText teamScore = CreateText(panel,
				scoreText, 0.5f, 0.5f, 0.72f,
				BingoTextRole.Compact, BingoBoardElement.GetTeamColor(team));
			scoreRow.AddWeighted(teamScore);
			visibleTeams++;
		}
		if (visibleTeams == 0)
			scoreRow.AddWeighted(CreateText(panel, Text("UI.NoClaims"), 0.5f, 0.5f, 0.72f,
				BingoTextRole.Compact));
		BingoAdaptiveText resultText = CreateText(panel, ResultText(), 0.5f, 0.5f, 0.82f,
			BingoTextRole.Compact, ResultColor());

		UIElement stopRow = null;
		if (host)
		{
			if (_confirmingStop)
			{
				UIHorizontalStack confirmation = new(8f);
				confirmation.AddWeighted(CreateButton(panel, Text("UI.SettleGame"),
					() => RequestEnd(BingoEndAction.Settle), emphasized: true, textRole: BingoTextRole.Compact));
				confirmation.AddWeighted(CreateButton(panel, Text("UI.Return"), CancelStopConfirmation,
					textRole: BingoTextRole.Compact));
				confirmation.AddWeighted(CreateButton(panel, Text("UI.CancelGame"),
					() => RequestEnd(BingoEndAction.Cancel), textRole: BingoTextRole.Compact,
					backgroundColor: BingoUITheme.DangerBackground));
				stopRow = confirmation;
			}
			else
			{
				BingoButton stop = CreateButton(panel, Text("UI.StopGame"), ShowStopConfirmation, emphasized: true);
				stop.Width.Set(120f, 0f);
				stop.Height.Set(0f, 1f);
				stop.HAlign = 0.5f;
				stopRow = new UIElement();
				stopRow.Append(stop);
			}
		}

		BingoButton lockButton = null;
		if (BingoWorldSystem.Phase == BingoGamePhase.InProgress)
		{
			lockButton = CreateButton(panel, Text(_gameWindowLocked ? "UI.Unlock" : "UI.Lock"),
				ToggleGameWindowLock, selected: _gameWindowLocked, textRole: BingoTextRole.Compact);
			panel.DragExclusion = lockButton;
		}
		if (lockButton != null)
			header.AddFixed(lockButton, 70f);
		else
			header.AddFixed(new UIElement(), 70f);
		root.AddFixed(header, 32f);
		root.AddWeighted(board, 1f, size * 24f);
		root.AddFixed(scoreRow, 22f);
		root.AddFixed(resultText, 22f);
		if (stopRow != null)
			root.AddFixed(stopRow, 32f);
		panel.Append(root);
		panel.Recalculate();
	}

	private void ShowStopConfirmation()
	{
		_confirmingStop = true;
		Rebuild();
	}

	private void CancelStopConfirmation()
	{
		_confirmingStop = false;
		Rebuild();
	}

	private void RequestEnd(BingoEndAction action)
	{
		_confirmingStop = false;
		BingoGame.RequestEnd(action);
	}

	private void ToggleGameWindowLock()
	{
		_gameWindowLocked = !_gameWindowLocked;
		Rebuild();
	}
}
