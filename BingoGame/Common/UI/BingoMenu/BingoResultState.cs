using System;
using System.Collections.Generic;
using BingoGame.Common.Configs;
using BingoGame.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BingoGame.Common.Systems;

internal sealed record BingoResultSnapshot(int BoardSize, IReadOnlyList<int> ItemTypes,
	IReadOnlyList<byte> Owners, BingoWinRule WinRule,
	long ElapsedTicks, byte WinningTeam, bool IsDraw, BingoFinishReason FinishReason, bool SinglePlayer,
	IReadOnlyList<BingoClaimRecord> Claims,
	IReadOnlyList<BingoTeamStanding> TeamStandings,
	IReadOnlyList<BingoContributionStanding> ContributionStandings)
{
	public static BingoResultSnapshot Capture()
	{
		List<BingoTeamStanding> teams = new(BingoWorldSystem.GetTeamStandings());
		List<BingoClaimRecord> claims = new(BingoWorldSystem.Claims);
		IReadOnlyList<BingoContributionStanding> sourceContributions = BingoWorldSystem.GetContributionStandings();
		List<BingoContributionStanding> contributions = new(sourceContributions.Count);
		foreach (BingoContributionStanding standing in sourceContributions)
		{
			List<int> itemTypes = new(standing.ItemTypes);
			contributions.Add(new BingoContributionStanding(standing.Rank, standing.Team, standing.PlayerName,
				standing.FirstOrder, itemTypes.AsReadOnly()));
		}

		return new BingoResultSnapshot(BingoWorldSystem.BoardSize,
			Array.AsReadOnly((int[])BingoWorldSystem.ItemTypes.Clone()),
			Array.AsReadOnly((byte[])BingoWorldSystem.Owners.Clone()), BingoWorldSystem.WinRule,
			BingoWorldSystem.ElapsedTicks,
			BingoWorldSystem.WinningTeam, BingoWorldSystem.IsDraw, BingoWorldSystem.FinishReason,
			Main.netMode == NetmodeID.SinglePlayer, claims.AsReadOnly(), teams.AsReadOnly(),
			contributions.AsReadOnly());
	}
}

internal sealed class BingoResultState : UIState
{
	private readonly List<BingoResultWindow> _windows = new();
	private readonly Action _allClosed;
	private int _nextCascadeIndex;

	public BingoResultState(Action allClosed)
	{
		_allClosed = allClosed;
	}

	public void Add(BingoResultSnapshot snapshot)
	{
		BingoResultWindow window = null;
		window = new BingoResultWindow(snapshot, () => Remove(window), _nextCascadeIndex++);
		_windows.Add(window);
		Append(window.Panel);
	}

	public void Clear()
	{
		_windows.Clear();
		_nextCascadeIndex = 0;
		RemoveAllChildren();
	}

	public override void Update(GameTime gameTime)
	{
		foreach (BingoResultWindow window in _windows)
			window.EnsureLayout(this);
		base.Update(gameTime);
	}

	private void Remove(BingoResultWindow window)
	{
		if (!_windows.Remove(window))
			return;
		window.Panel.Remove();
		if (_windows.Count == 0)
			_allClosed();
	}
}

internal sealed class BingoResultWindow
{
	private const float ScreenMargin = 16f;
	private readonly BingoResultSnapshot _snapshot;
	private readonly Action _close;
	private Vector2 _windowCenterOffset;
	private int _shownWidth;
	private int _shownHeight;

	public BingoResponsivePanel Panel { get; private set; }

	public BingoResultWindow(BingoResultSnapshot snapshot, Action close, int cascadeIndex)
	{
		_snapshot = snapshot;
		_close = close;
		float cascadeOffset = cascadeIndex % 10 * 24f;
		_windowCenterOffset = new Vector2(cascadeOffset, cascadeOffset);
		Rebuild();
	}

	public void EnsureLayout(UIState owner)
	{
		if (_shownWidth == Main.screenWidth && _shownHeight == Main.screenHeight)
			return;
		Panel.Remove();
		Rebuild();
		owner.Append(Panel);
	}

	private void Rebuild()
	{
		_shownWidth = Main.screenWidth;
		_shownHeight = Main.screenHeight;
		int size = _snapshot.BoardSize;
		float statisticsHeight = _snapshot.SinglePlayer ? 120f : 100f;
		float available = Math.Min(Main.screenWidth - 100f, Main.screenHeight - 220f);
		float defaultCellSize = Math.Clamp(available / size, 36f, 64f);
		float minimumWidth = Math.Max(360f, size * 36f + 40f);
		float minimumHeight = size * 36f + statisticsHeight + 80f;
		Panel = CreateWindow(minimumWidth, minimumHeight,
			defaultCellSize * size + 40f, defaultCellSize * size + statisticsHeight + 80f);

		UIVerticalStack root = CreateRootStack(6f, 8f);
		UIHorizontalStack header = new(8f);
		BingoAdaptiveText timer = CreateText(Panel, BingoWorldSystem.FormatElapsed(_snapshot.ElapsedTicks),
			0.5f, 0.5f, 0.92f, BingoTextRole.Compact);
		BingoAdaptiveText title = CreateText(Panel, Text("UI.GameOver"), 0.5f, 0.5f, 1.1f,
			BingoTextRole.Title);
		BingoButton close = CreateButton(Panel, Text("UI.Close"), _close, textRole: BingoTextRole.Compact);
		Panel.DragExclusion = close;
		header.AddFixed(timer, 70f);
		header.AddWeighted(title, 1f, 120f);
		header.AddFixed(close, 70f);

		BingoBoardElement board = new(size, _snapshot.ItemTypes, _snapshot.Owners,
			_snapshot.Claims, () => Panel.BorderColor, _snapshot.SinglePlayer);
		BingoAdaptiveText result = CreateText(Panel, ResultText(), 0.5f, 0.5f, 0.82f,
			BingoTextRole.Compact, ResultColor());
		BingoScrollList standings = BuildStandings(Panel);

		root.AddFixed(header, 32f);
		root.AddWeighted(board, 1f, size * 24f);
		root.AddFixed(result, 24f);
		root.AddWeighted(standings, 0.35f, statisticsHeight);
		Panel.Append(root);
		Panel.Recalculate();
	}

	private BingoScrollList BuildStandings(BingoResponsivePanel panel)
	{
		BingoScrollList standings = new(4f);
		if (_snapshot.SinglePlayer)
		{
			int score = 0;
			foreach (byte owner in _snapshot.Owners)
				score += owner == BingoWorldSystem.SinglePlayerTeam ? 1 : 0;
			standings.AddRow(CreateText(panel, Text("UI.PersonalResultScore", score), 0.5f, 0.5f, 0.86f,
				BingoTextRole.Compact, BingoBoardElement.GetTeamColor(BingoWorldSystem.SinglePlayerTeam)), 34f);
			foreach (BingoContributionStanding standing in _snapshot.ContributionStandings)
			{
				standings.AddRow(CreateText(panel,
					Text("UI.PersonalContribution", standing.PlayerName, standing.ItemTypes.Count,
						BuildItemNames(standing.ItemTypes)), 0.5f, 0.5f, 0.78f, BingoTextRole.Compact), 38f);
			}
			return standings;
		}

		if (_snapshot.TeamStandings.Count == 0)
		{
			standings.AddRow(CreateText(panel, Text("UI.NoClaims"), 0.5f, 0.5f, 0.86f,
				BingoTextRole.Compact, Color.Silver), 40f);
			return standings;
		}

		foreach (BingoTeamStanding standing in _snapshot.TeamStandings)
		{
			standings.AddRow(CreateText(panel,
				Text("UI.TeamRanking", standing.Rank, BingoTeamDisplay.GetName(standing.Team), standing.Score),
				0.5f, 0.5f, 0.9f, BingoTextRole.Compact, BingoTeamDisplay.GetColor(standing.Team)), 40f);
		}
		return standings;
	}

	private string ResultText()
	{
		if (_snapshot.SinglePlayer)
			return Text(_snapshot.FinishReason == BingoFinishReason.Natural
				? "UI.ChallengeSuccess"
				: "UI.ChallengeIncomplete");
		if (_snapshot.IsDraw)
			return Text("UI.Draw");
		return Text("UI.TeamWon", BingoTeamDisplay.GetName(_snapshot.WinningTeam));
	}

	private Color ResultColor()
	{
		if (_snapshot.SinglePlayer)
			return _snapshot.FinishReason == BingoFinishReason.Natural
				? BingoUITheme.SuccessBackground
				: Color.Silver;
		return _snapshot.IsDraw ? Color.White : BingoBoardElement.GetTeamColor(_snapshot.WinningTeam);
	}

	private BingoResponsivePanel CreateWindow(float minimumWidth, float minimumHeight, float defaultWidth,
		float defaultHeight)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		float width = config.ResultWidth > 0 ? config.ResultWidth : defaultWidth;
		float height = config.ResultHeight > 0 ? config.ResultHeight : defaultHeight;
		Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight / 2f);
		center += _windowCenterOffset;
		BingoResponsivePanel panel = new(width, height, minimumWidth, minimumHeight, defaultWidth, defaultHeight,
			ScreenMargin, center,
			newCenter => _windowCenterOffset = newCenter - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f),
			SaveSize);
		BingoUITheme.Apply(panel);
		panel.OverflowHidden = true;
		return panel;
	}

	private static void SaveSize(float width, float height)
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		config.ResultWidth = (int)MathF.Round(width);
		config.ResultHeight = (int)MathF.Round(height);
		config.SaveChanges();
	}

	private static UIVerticalStack CreateRootStack(float spacing, float padding)
	{
		UIVerticalStack root = new(spacing);
		root.Width.Set(0f, 1f);
		root.Height.Set(0f, 1f);
		root.SetPadding(padding);
		return root;
	}

	private static BingoAdaptiveText CreateText(BingoResponsivePanel panel, string value, float hAlign,
		float vAlign, float scale, BingoTextRole role = BingoTextRole.Normal, Color? color = null)
	{
		BingoAdaptiveText text = new(value, scale, hAlign, vAlign, role, () => panel.LayoutScale);
		if (color.HasValue)
			text.TextColor = color.Value;
		return text;
	}

	private static BingoButton CreateButton(BingoResponsivePanel panel, string label, Action action,
		bool selected = false, bool emphasized = false, BingoTextRole textRole = BingoTextRole.Normal)
	{
		BingoButton button = new(action, selected, emphasized);
		BingoAdaptiveText text = CreateText(panel, label, 0.5f, 0.5f, 0.86f, textRole);
		text.Width.Set(0f, 1f);
		text.Height.Set(0f, 1f);
		button.Append(text);
		return button;
	}

	private static string BuildItemNames(IReadOnlyList<int> itemTypes)
	{
		List<string> names = new(itemTypes.Count);
		foreach (int itemType in itemTypes)
			names.Add(Lang.GetItemNameValue(itemType));
		return string.Join(", ", names);
	}

	private static string Text(string key, params object[] args) =>
		Language.GetTextValue($"Mods.BingoGame.{key}", args);
}
