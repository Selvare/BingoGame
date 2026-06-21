using System;
using System.Collections.Generic;
using System.Linq;
using BingoGame.Common.Configs;
using Terraria.ModLoader;

namespace BingoGame.Common.Systems;

internal sealed partial class BingoMenuState
{
	public void SaveDraftToConfig()
	{
		if (_draftSize is < 2 or > 10 || _draftItems.Length != _draftSize * _draftSize)
			return;

		BingoClientConfig clientConfig = ModContent.GetInstance<BingoClientConfig>();
		BingoGameConfig gameConfig = ModContent.GetInstance<BingoGameConfig>();
		clientConfig.DraftBoardSize = _draftSize;
		gameConfig.DraftWinRule = _draftRule;
		gameConfig.DraftItemTypes = _draftItems.ToList();
		clientConfig.SaveChanges();
		gameConfig.SaveChanges();
	}

	private void LoadDraftFromWorld()
	{
		BingoClientConfig clientConfig = ModContent.GetInstance<BingoClientConfig>();
		BingoGameConfig gameConfig = ModContent.GetInstance<BingoGameConfig>();
		if (clientConfig.DraftBoardSize is >= 2 and <= 10
			&& gameConfig.DraftItemTypes?.Count == clientConfig.DraftBoardSize * clientConfig.DraftBoardSize)
		{
			_draftSize = clientConfig.DraftBoardSize;
			_draftRule = Enum.IsDefined(typeof(BingoWinRule), gameConfig.DraftWinRule)
				? gameConfig.DraftWinRule
				: BingoWinRule.Line;
			_draftItems = gameConfig.DraftItemTypes.ToArray();
			return;
		}

		_draftSize = Math.Clamp(BingoWorldSystem.BoardSize, 2, 10);
		_draftRule = BingoWorldSystem.WinRule;
		_draftItems = BingoWorldSystem.ItemTypes.Length == _draftSize * _draftSize
			? (int[])BingoWorldSystem.ItemTypes.Clone()
			: new int[_draftSize * _draftSize];
	}

	private static BingoGameConfig GetGameConfig()
	{
		BingoGameConfig config = ModContent.GetInstance<BingoGameConfig>();
		bool changed = false;
		config.DraftItemTypes ??= new List<int>();
		config.Whitelists ??= new List<BingoWhitelistEntry>();
		for (int i = 0; i < config.Whitelists.Count; i++)
		{
			config.Whitelists[i] ??= new BingoWhitelistEntry();
			config.Whitelists[i].ItemTypes ??= new List<int>();
			config.Whitelists[i].Name ??= string.Empty;
		}
		config.InitialItemLists ??= new List<BingoInitialItemListEntry>();
		bool foundEnabled = false;
		for (int i = 0; i < config.InitialItemLists.Count; i++)
		{
			if (config.InitialItemLists[i] == null)
			{
				config.InitialItemLists[i] = new BingoInitialItemListEntry();
				changed = true;
			}
			BingoInitialItemListEntry entry = config.InitialItemLists[i];
			if (entry.Items == null)
			{
				entry.Items = new List<BingoInitialItemStack>();
				changed = true;
			}
			for (int itemIndex = entry.Items.Count - 1; itemIndex >= 0; itemIndex--)
			{
				if (entry.Items[itemIndex] != null)
					continue;
				entry.Items.RemoveAt(itemIndex);
				changed = true;
			}
			if (string.IsNullOrWhiteSpace(entry.Name))
			{
				entry.Name = i == 0 ? Text("UI.InitialItemBuiltInName") : Text("UI.InitialItemDefaultName", i + 1);
				changed = true;
			}
			if (!entry.Enabled)
				continue;
			if (foundEnabled)
			{
				entry.Enabled = false;
				changed = true;
			}
			else
				foundEnabled = true;
		}
		if (changed)
			config.SaveChanges();
		return config;
	}
}

