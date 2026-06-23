using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
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

	[DefaultValue(620)]
	public int AdvancedSettingsWidth;

	[DefaultValue(430)]
	public int AdvancedSettingsHeight;

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

	[DefaultValue(460)]
	public int ResultWidth;

	[DefaultValue(360)]
	public int ResultHeight;

	[DefaultValue(620)]
	public int WhitelistListWidth;

	[DefaultValue(470)]
	public int WhitelistListHeight;

	[DefaultValue(560)]
	public int WhitelistEditorWidth;

	[DefaultValue(260)]
	public int WhitelistEditorHeight;

	[DefaultValue(620)]
	public int InitialItemListWidth;

	[DefaultValue(470)]
	public int InitialItemListHeight;

	[DefaultValue(560)]
	public int InitialItemEditorWidth;

	[DefaultValue(260)]
	public int InitialItemEditorHeight;

	[DefaultValue(0)]
	[Range(0, 10)]
	public int DraftBoardSize;

	[JsonProperty("DraftWinRule")]
	private BingoWinRule LegacyDraftWinRule = BingoWinRule.Line;

	[JsonProperty("DraftItemTypes")]
	private List<int> LegacyDraftItemTypes = new();

	private bool ShouldSerializeLegacyDraftWinRule() => false;
	private bool ShouldSerializeLegacyDraftItemTypes() => false;

	internal void MigrateLegacyDraft(BingoGameConfig gameConfig)
	{
		LegacyDraftItemTypes ??= new List<int>();
		gameConfig.DraftItemTypes ??= new List<int>();
		bool hasLegacyDraft = LegacyDraftWinRule != BingoWinRule.Line || LegacyDraftItemTypes.Count > 0;
		bool gameConfigIsDefault = gameConfig.DraftWinRule == BingoWinRule.Line
			&& gameConfig.DraftItemTypes.Count == 0;
		if (!hasLegacyDraft || !gameConfigIsDefault)
			return;

		gameConfig.DraftWinRule = LegacyDraftWinRule;
		gameConfig.DraftItemTypes = new List<int>(LegacyDraftItemTypes);
		LegacyDraftWinRule = BingoWinRule.Line;
		LegacyDraftItemTypes.Clear();
		gameConfig.SaveChanges();
		SaveChanges();
	}
}
