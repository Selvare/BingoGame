using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BingoGame.Common.UI.Services;

/// <summary>
/// Bingo 提示框服务，统一 Tooltip 管理
/// </summary>
internal static class BingoTooltipService
{
	private static string _currentTitle = "";
	private static string _currentBody = "";
	private static Color? _currentBodyColor;

	/// <summary>
	/// 显示物品提示框
	/// </summary>
	public static void ShowItem(Item item, string title, string body, Color? bodyColor = null)
	{
		_currentTitle = title ?? "";
		_currentBody = body ?? "";
		_currentBodyColor = bodyColor;

		// 需要在全局 GlobalItem 钩子中处理实际绘制
		// 这里只是存储状态
	}

	/// <summary>
	/// 显示自定义提示框
	/// </summary>
	public static void ShowCustom(string title, string body, Color? bodyColor = null)
	{
		_currentTitle = title ?? "";
		_currentBody = body ?? "";
		_currentBodyColor = bodyColor;
	}

	/// <summary>
	/// 清除当前提示框
	/// </summary>
	public static void Clear()
	{
		_currentTitle = "";
		_currentBody = "";
		_currentBodyColor = null;
	}

	/// <summary>
	/// 检查是否有活动的提示框
	/// </summary>
	public static bool HasActive => !string.IsNullOrEmpty(_currentTitle) || !string.IsNullOrEmpty(_currentBody);

	/// <summary>
	/// 获取当前提示框内容
	/// </summary>
	public static (string title, string body, Color? bodyColor) GetCurrent() 
		=> (_currentTitle, _currentBody, _currentBodyColor);
}
