using System;
using BingoGame.Common.UI.Core;
using Microsoft.Xna.Framework;
using Terraria;

namespace BingoGame.Common.UI;

/// <summary>
/// Bingo UI 管理器 - 集成新的 UI 系统
/// 负责路由、状态管理、和生命周期控制
/// </summary>
internal sealed class BingoUIManager
{
	private BingoUiContext _context;
	private bool _isVisible;

	public static BingoUIManager Instance { get; private set; }

	public bool IsVisible => _isVisible;
	public BingoUiContext Context => _context;

	/// <summary>
	/// 初始化 UI 管理器
	/// </summary>
	public void Initialize()
	{
		try
		{
			// 初始化上下文（包含状态、操作、路由）
			_context = new BingoUiContext();
			_isVisible = false;
		}
		catch (Exception ex)
		{
			Main.NewText($"[c/FF0000:UI 初始化错误] {ex.Message}", 255, 0, 0);
		}
	}

	/// <summary>
	/// 显示 UI
	/// </summary>
	public void Show()
	{
		if (_isVisible || _context == null)
			return;

		try
		{
			_isVisible = true;
		}
		catch (Exception ex)
		{
			Main.NewText($"[c/FF0000:UI 错误] {ex.Message}", 255, 0, 0);
			_isVisible = false;
		}
	}

	/// <summary>
	/// 隐藏 UI
	/// </summary>
	public void Hide()
	{
		if (!_isVisible)
			return;

		try
		{
			_isVisible = false;
		}
		catch (Exception ex)
		{
			Main.NewText($"[c/FF0000:UI 错误] {ex.Message}", 255, 0, 0);
		}
	}

	/// <summary>
	/// 切换 UI 显示
	/// </summary>
	public void Toggle()
	{
		if (_isVisible)
			Hide();
		else
			Show();
	}

	/// <summary>
	/// 更新 UI 状态
	/// </summary>
	public void Update(GameTime gameTime)
	{
		if (!_isVisible || _context == null)
			return;

		try
		{
			// UI 状态更新逻辑
			// 由 BingoUiRouter 根据 BingoWorldSystem 状态决定当前屏幕
		}
		catch (Exception ex)
		{
			Main.NewText($"[c/FF0000:UI 更新错误] {ex.Message}", 255, 0, 0);
		}
	}

	/// <summary>
	/// 绘制 UI
	/// </summary>
	public void Draw(GameTime gameTime)
	{
		if (!_isVisible || _context == null)
			return;

		try
		{
			// UI 绘制逻辑
			// 由路由器确定的当前屏幕进行绘制
		}
		catch (Exception ex)
		{
			Main.NewText($"[c/FF0000:UI 绘制错误] {ex.Message}", 255, 0, 0);
		}
	}

	/// <summary>
	/// 清理资源
	/// </summary>
	public void Dispose()
	{
		Hide();
		_context = null;
	}

	/// <summary>
	/// 创建或获取实例
	/// </summary>
	public static void CreateInstance()
	{
		Instance = new BingoUIManager();
		Instance.Initialize();
	}
}
