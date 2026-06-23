
namespace BingoGame.Common.Systems;

internal enum BingoEditorSaveStatus
{
	None,
	Success,
	Warning,
	Failure
}

internal readonly record struct BingoEditorSaveResult(BingoEditorSaveStatus Status, string Message)
{
	public bool Failed => Status == BingoEditorSaveStatus.Failure;
}

internal enum BingoWindowPage
{
	Settings,
	AdvancedSettings,
	Waiting,
	Editor,
	Game,
	WhitelistList,
	WhitelistEditor,
	InitialItemList,
	InitialItemEditor
}
