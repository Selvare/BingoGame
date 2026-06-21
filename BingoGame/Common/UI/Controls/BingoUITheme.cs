using System;
using BingoGame.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace BingoGame.Common.Systems;

internal static class BingoUITheme
{
	private static byte _backgroundAlpha = 204;
	private static byte BackgroundAlpha => _backgroundAlpha;
	public static Color CellBackground => WithOpacity(new Color(63, 82, 151));
	public static Color SuccessBackground => new(45, 150, 70);
	public static Color DangerBackground => new(170, 45, 45);

	public static void RefreshOpacity()
	{
		BingoClientConfig config = ModContent.GetInstance<BingoClientConfig>();
		float value = BingoWorldSystem.Phase == BingoGamePhase.InProgress
			? config.InProgressUIOpacity
			: config.InactiveUIOpacity;
		_backgroundAlpha = (byte)MathF.Round(255f * Math.Clamp(value, 0f, 1f));
	}

	public static void Apply(UIPanel panel, bool selected = false, bool emphasized = false)
	{
		Color background = panel.BackgroundColor;
		Color border = panel.BorderColor;
		if (selected)
		{
			background = Color.Lerp(background, Color.White, 0.2f);
			border = Color.Lerp(border, Color.White, 0.35f);
		}
		else if (emphasized)
		{
			background = Color.Lerp(background, new Color(120, 170, 255), 0.18f);
			border = Color.Lerp(border, Color.White, 0.18f);
		}
		panel.BackgroundColor = WithOpacity(background);
		panel.BorderColor = WithFullOpacity(border);
	}

	public static Color WithOpacity(Color color)
	{
		color.A = BackgroundAlpha;
		return color;
	}

	public static Color WithFullOpacity(Color color)
	{
		color.A = byte.MaxValue;
		return color;
	}
}
