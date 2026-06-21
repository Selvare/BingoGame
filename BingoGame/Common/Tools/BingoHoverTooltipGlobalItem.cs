using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace BingoGame.Common.Tools;

internal sealed class BingoHoverTooltipGlobalItem : GlobalItem
{
	public override bool InstancePerEntity => true;

	private string _customTitle;
	private string _customBody;

	public static void Show(Item item, string title, string body)
	{
		Main.LocalPlayer.mouseInterface = true;
		item.SetNameOverride(title);
		BingoHoverTooltipGlobalItem global = item.GetGlobalItem<BingoHoverTooltipGlobalItem>();
		global._customTitle = title;
		global._customBody = body;
		Main.HoverItem = item;
		Main.hoverItemName = title;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (string.IsNullOrEmpty(_customTitle))
			return;

		TooltipLine nameLine = tooltips.FirstOrDefault(line => line.Name == "ItemName");
		tooltips.RemoveAll(line => line.Name != "ItemName");

		if (nameLine is not null)
		{
			nameLine.Text = _customTitle;
		}
		else
		{
			nameLine = new TooltipLine(Mod, "ItemName", _customTitle);
			tooltips.Insert(0, nameLine);
		}
		if (string.IsNullOrEmpty(_customBody))
			return;

		string[] lines = _customBody.Split('\n');
		for (int i = 0; i < lines.Length; i++)
			if (!string.IsNullOrWhiteSpace(lines[i]))
				tooltips.Add(new TooltipLine(Mod, $"BingoTooltip{i}", lines[i]));
	}
}
