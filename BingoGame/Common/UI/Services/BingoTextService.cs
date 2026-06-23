using System;
using BingoGame.Common.UI.Core;
using Terraria;
using Terraria.Localization;

namespace BingoGame.Common.UI.Services;

/// <summary>
/// Bingo 文本服务，统一本地化文本处理
/// </summary>
internal sealed class BingoTextService
{
	private const string LanguagePrefix = "Mods.BingoGame.";

	/// <summary>
	/// 获取本地化文本
	/// </summary>
	/// <param name="key">相对于 Mods.BingoGame 的 key</param>
	/// <param name="args">格式化参数</param>
	public string Get(string key, params object[] args)
	{
		string fullKey = LanguagePrefix + key;
		try
		{
			string text = Language.GetTextValue(fullKey, args);
			return string.IsNullOrEmpty(text) ? key : text;
		}
		catch
		{
			return key;
		}
	}

	/// <summary>
	/// 获取本地化文本（带缓存提示）
	/// </summary>
	public string GetSafe(string key)
	{
		string fullKey = LanguagePrefix + key;
		LocalizedText text = Language.GetOrRegister(fullKey);
		return text?.Value ?? key;
	}
}
