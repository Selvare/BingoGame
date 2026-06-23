using System.Collections.Generic;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;

namespace BingoGame.Common.Systems;

public sealed class BingoFogOfWarSystem : ModSystem
{
	public override void PreDrawMapIconOverlay(IReadOnlyList<IMapLayer> layers,
		MapOverlayDrawContext mapOverlayDrawContext)
	{
		if (!BingoWorldSystem.FogOfWarEnabled || BingoWorldSystem.Phase != BingoGamePhase.InProgress
			|| layers is not IList<IMapLayer> mutableLayers)
			return;

		for (int i = mutableLayers.Count - 1; i >= 0; i--)
		{
			string layerName = mutableLayers[i].GetType().FullName ?? string.Empty;
			if (layerName.Contains("Player") || layerName.Contains("Boss") || layerName.Contains("NPC"))
				mutableLayers.RemoveAt(i);
		}
	}
}

public sealed class BingoFogOfWarGlobalNPC : GlobalNPC
{
	public override void BossHeadSlot(NPC npc, ref int index)
	{
		if (BingoWorldSystem.FogOfWarEnabled && BingoWorldSystem.Phase == BingoGamePhase.InProgress)
			index = -1;
	}
}
