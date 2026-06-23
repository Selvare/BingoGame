// 向后兼容性 using 别名
// 此文件提供对旧命名空间的支持，避免一次性大改

// Layout 层别名
global using UIStack = BingoGame.Common.UI.Layout.BingoStackPanel;
global using UIVerticalStack = BingoGame.Common.UI.Layout.BingoVerticalStack;
global using UIHorizontalStack = BingoGame.Common.UI.Layout.BingoHorizontalStack;
global using UIBingoBoardGrid = BingoGame.Common.UI.Layout.BingoSquareGrid;
global using BingoTextRole = BingoGame.Common.UI.Layout.BingoTextRole;
global using BingoScrollList = BingoGame.Common.UI.Layout.BingoScrollList;

// Components 层别名
global using BingoButton = BingoGame.Common.UI.Components.BingoButton;
global using BingoAdaptiveText = BingoGame.Common.UI.Components.BingoAdaptiveText;
global using BingoResponsivePanel = BingoGame.Common.UI.Components.BingoResponsivePanel;
global using BingoBoardElement = BingoGame.Common.UI.Components.BingoBoardElement;
global using BingoBoardCell = BingoGame.Common.UI.Components.BingoBoardCell;
global using BingoItemIconRenderer = BingoGame.Common.UI.Components.BingoItemIconRenderer;
global using BingoResizeEdge = BingoGame.Common.UI.Components.BingoResizeEdge;

// Inputs 层别名 - 暂时保留，准备迁移旧的Controls引用
global using OldBingoTextInput = BingoGame.Common.UI.Inputs.BingoTextInput;
global using OldBingoNumericInput = BingoGame.Common.UI.Inputs.BingoNumericInput;

// Theme 层别名
global using BingoTheme = BingoGame.Common.UI.Theme.BingoTheme;
global using BingoButtonVariant = BingoGame.Common.UI.Theme.BingoButtonVariant;
