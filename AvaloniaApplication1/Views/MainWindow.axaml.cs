using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{
    public List<ListableItem> RootItems { get; } = new();
    private readonly FileOperationsService _fileOperationsService = new();
    private readonly RootTreeService _rootTreeService = new();
    private readonly TreeViewStateService _treeViewStateService = new();
    private string _currentSearchQuery = string.Empty;

    private Button CopyButtonControl => this.FindControl<Button>("CopyButton") ?? throw new InvalidOperationException("CopyButton control was not found.");
    private Button CutButtonControl => this.FindControl<Button>("CutButton") ?? throw new InvalidOperationException("CutButton control was not found.");
    private Button PasteButtonControl => this.FindControl<Button>("PasteButton") ?? throw new InvalidOperationException("PasteButton control was not found.");
    private Button AddFileButtonControl => this.FindControl<Button>("AddFileButton") ?? throw new InvalidOperationException("AddFileButton control was not found.");
    private Button AddFolderButtonControl => this.FindControl<Button>("AddFolderButton") ?? throw new InvalidOperationException("AddFolderButton control was not found.");
    private Button RenameButtonControl => this.FindControl<Button>("RenameButton") ?? throw new InvalidOperationException("RenameButton control was not found.");
    private Button MoveButtonControl => this.FindControl<Button>("MoveButton") ?? throw new InvalidOperationException("MoveButton control was not found.");
    private Button DeleteButtonControl => this.FindControl<Button>("DeleteButton") ?? throw new InvalidOperationException("DeleteButton control was not found.");
    private Button RefreshFolderButtonControl => this.FindControl<Button>("RefreshFolderButton") ?? throw new InvalidOperationException("RefreshFolderButton control was not found.");
    private Button ExpandFolderButtonControl => this.FindControl<Button>("ExpandFolderButton") ?? throw new InvalidOperationException("ExpandFolderButton control was not found.");
    private Button CollapseFolderButtonControl => this.FindControl<Button>("CollapseFolderButton") ?? throw new InvalidOperationException("CollapseFolderButton control was not found.");
    private TextBox SearchTextBoxControl => this.FindControl<TextBox>("SearchTextBox") ?? throw new InvalidOperationException("SearchTextBox control was not found.");
    private CheckBox LightModeToggleControl => this.FindControl<CheckBox>("LightModeToggle") ?? throw new InvalidOperationException("LightModeToggle control was not found.");

    public MainWindow()
    {
        InitializeComponent();
        DirectoryTreeView.ItemsSource = RootItems;
        DirectoryTreeView.SelectionChanged += OnDirectoryTreeSelectionChanged;
        DirectoryTreeView.DoubleTapped += OnDirectoryTreeViewDoubleTapped;

        RootTreeResult initialRootResult = _rootTreeService.CreateInitialRootFromEnvironment();
        if (!initialRootResult.IsSuccess || initialRootResult.RootItem is null)
        {
            //will eventually implement an error screen
            throw new DirectoryNotFoundException(initialRootResult.ErrorMessage ?? "Initial root is not set correctly!");
        }

        RootItems.Add(initialRootResult.RootItem);
        ClearFileInformation();
        UpdateToolbarState();

        ThemeVariant? currentTheme = Application.Current?.RequestedThemeVariant;
        LightModeToggleControl.IsChecked = currentTheme == ThemeVariant.Light;
    }

    private async void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox)
        {
            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.C:
                HandleCopyShortcut();
                e.Handled = true;
                break;
            case Key.X:
                if (TryGetSelectedItem(out ListableItem cutItem))
                {
                    _fileOperationsService.Cut(cutItem);
                    UpdateToolbarState();
                }

                e.Handled = true;
                break;
            case Key.V:
                await HandlePasteShortcutAsync();
                e.Handled = true;
                break;
        }
    }

    private void OnDirectoryTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DirectoryTreeView.SelectedItem is ListableItem item)
        {
            item.ReloadProperties();
            UpdateFileInformation(item);
            UpdateToolbarState();
            return;
        }

        ClearFileInformation();
        UpdateToolbarState();
    }

    private void UpdateFileInformation(ListableItem item)
    {
        this.FindControl<TextBlock>("NoSelectionTextBlock")!.IsVisible = false;
        this.FindControl<StackPanel>("FileInformationPanel")!.IsVisible = true;

        this.FindControl<TextBlock>("NameValueTextBlock")!.Text = item.Name;
        this.FindControl<TextBlock>("ExtensionValueTextBlock")!.Text = string.IsNullOrEmpty(item.Extension) ? "-" : item.Extension;
        this.FindControl<TextBlock>("PathValueTextBlock")!.Text = item.FullPath;
        this.FindControl<TextBlock>("TypeValueTextBlock")!.Text = item.Type.ToString();
        this.FindControl<TextBlock>("LastModifiedValueTextBlock")!.Text = item.LastModified.ToString("G");
        this.FindControl<TextBlock>("CreatedValueTextBlock")!.Text = item.Created.ToString("G");
        this.FindControl<TextBlock>("PermissionsValueTextBlock")!.Text = item.Permissions.ToString();
    }

    private void ClearFileInformation()
    {
        this.FindControl<TextBlock>("NoSelectionTextBlock")!.IsVisible = true;
        this.FindControl<StackPanel>("FileInformationPanel")!.IsVisible = false;

        this.FindControl<TextBlock>("NameValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("ExtensionValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("PathValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("TypeValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("LastModifiedValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("CreatedValueTextBlock")!.Text = string.Empty;
        this.FindControl<TextBlock>("PermissionsValueTextBlock")!.Text = string.Empty;
    }

    private void OnDirectoryTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DirectoryTreeView.SelectedItem is not ListableItem item || item.Type != ListableItemType.File)
        {
            return;
        }

        var viewer = new FileViewer(item.FullPath, item.Permissions.CanWrite);
        viewer.Show(this);
    }

    private void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        HandleCopyShortcut();
    }

    private void OnCutClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item))
        {
            return;
        }

        _fileOperationsService.Cut(item);
        UpdateToolbarState();
    }

    private async void OnPasteClicked(object? sender, RoutedEventArgs e)
    {
        await HandlePasteShortcutAsync();
    }

    private async void OnAddFileClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        var inputDialog = new TextInputDialog(
            "Add File",
            $"Create a new file in '{item.Name}'. Enter a file name:",
            "",
            "Create",
            "Cancel",
            value => _fileOperationsService.ValidateNewName(value, item.FullPath));

        string? newFileName = await inputDialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(newFileName))
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.AddFile(item, newFileName);
        await HandleOperationResultAsync("Create File Failed", result);
    }

    private async void OnAddFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        var inputDialog = new TextInputDialog(
            "Add Folder",
            $"Create a new folder in '{item.Name}'. Enter a folder name:",
            "",
            "Create",
            "Cancel",
            value => _fileOperationsService.ValidateNewName(value, item.FullPath));

        string? newFolderName = await inputDialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(newFolderName))
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.AddFolder(item, newFolderName);
        await HandleOperationResultAsync("Create Folder Failed", result);
    }

    private async void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item))
        {
            return;
        }

        var confirmation = new ConfirmationDialog(
            "Delete",
            $"Delete '{item.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!await confirmation.ShowAsync(this))
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.Delete(item);
        await HandleOperationResultAsync("Delete Failed", result);
    }

    private async void OnRenameClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item))
        {
            return;
        }

        string? parentDirectory = _fileOperationsService.GetParentDirectoryPath(item);
        if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
        {
            await ShowMessageAsync("Rename", "Unable to resolve the parent folder for this item.");
            return;
        }

        string renameTitle = item.Type == ListableItemType.File ? "Rename File" : "Rename Folder";
        string renameMessage = item.Type == ListableItemType.File
            ? $"Enter the new file name for '{item.Name}'. Include the extension if you want to change it:"
            : $"Enter a new folder name for '{item.Name}':";

        var inputDialog = new TextInputDialog(
            renameTitle,
            renameMessage,
            item.Name,
            item.Type == ListableItemType.File ? "Rename File" : "Rename Folder",
            "Cancel",
            value => _fileOperationsService.ValidateNewName(value, parentDirectory, item.FullPath));

        string? newName = await inputDialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.Rename(item, newName, IsRootItem(item));
        await HandleOperationResultAsync("Rename Failed", result);
    }

    private async void OnMoveClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item))
        {
            return;
        }

        if (RootItems.Count == 0)
        {
            await ShowMessageAsync("Move", "No destination tree is available.");
            return;
        }

        var folderPicker = new FolderPickerDialog(RootItems[0].FullPath);
        string? destinationDirectory = await folderPicker.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.Move(item, destinationDirectory, IsRootItem(item));
        await HandleOperationResultAsync("Move Failed", result);
    }

    private async Task HandleOperationResultAsync(string errorTitle, FileOperationResult result)
    {
        if (!result.IsSuccess)
        {
            await ShowMessageAsync(errorTitle, result.ErrorMessage ?? "Operation failed.");
            return;
        }

        await RefreshTreeAsync(result.UpdatedRootPath);
        UpdateToolbarState();
    }

    private void HandleCopyShortcut()
    {
        if (!TryGetSelectedItem(out ListableItem item))
        {
            return;
        }

        _fileOperationsService.Copy(item);
        UpdateToolbarState();
    }

    private async Task HandlePasteShortcutAsync()
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        FileOperationResult result = _fileOperationsService.PasteInto(item);
        await HandleOperationResultAsync("Paste Failed", result);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ConfirmationDialog(title, message, "OK", "Close");
        await dialog.ShowAsync(this);
    }

    private async Task RefreshTreeAsync(string? rootPathOverride = null)
    {
        TreeViewState previousState = _treeViewStateService.CaptureState(RootItems, DirectoryTreeView.SelectedItem as ListableItem);

        if (RootItems.Count == 0 && string.IsNullOrWhiteSpace(rootPathOverride))
        {
            ClearFileInformation();
            return;
        }

        string rootPath = rootPathOverride ?? RootItems[0].FullPath;
        RootTreeResult refreshResult = _rootTreeService.BuildRoot(rootPath);
        if (!refreshResult.IsSuccess || refreshResult.RootItem is null)
        {
            RootItems.Clear();
            DirectoryTreeView.SelectedItem = null;
            DirectoryTreeView.ItemsSource = null;
            ClearFileInformation();
            UpdateToolbarState();
            return;
        }

        RootItems.Clear();
        RootItems.Add(refreshResult.RootItem);

        DirectoryTreeView.ItemsSource = null;
        DirectoryTreeView.ItemsSource = RootItems;

        ListableItem? restoredSelection = _treeViewStateService.RestoreState(RootItems, previousState);
        DirectoryTreeView.SelectedItem = restoredSelection;
        if (restoredSelection is not null)
        {
            restoredSelection.ReloadProperties();
            UpdateFileInformation(restoredSelection);
        }
        else
        {
            ClearFileInformation();
        }

        ApplySearchFilter(_currentSearchQuery, selectFirstMatch: true);

        UpdateToolbarState();
        await Task.CompletedTask;
    }

    private bool TryGetSelectedItem(out ListableItem item)
    {
        if (DirectoryTreeView.SelectedItem is not ListableItem listableItem)
        {
            item = null!;
            return false;
        }

        item = listableItem;
        return true;
    }

    private bool IsRootItem(ListableItem item)
    {
        return RootItems.Count > 0 && _fileOperationsService.ArePathsEqual(item.FullPath, RootItems[0].FullPath);
    }

    private void UpdateToolbarState()
    {
        ListableItem? selectedItem = DirectoryTreeView.SelectedItem as ListableItem;
        bool hasSelection = selectedItem is not null;
        bool isDirectory = selectedItem?.Type == ListableItemType.Directory;

        CopyButtonControl.IsEnabled = hasSelection;
        CutButtonControl.IsEnabled = hasSelection;
        PasteButtonControl.IsEnabled = isDirectory && _fileOperationsService.HasClipboardItem;
        AddFileButtonControl.IsEnabled = isDirectory;
        AddFolderButtonControl.IsEnabled = isDirectory;
        RenameButtonControl.IsEnabled = hasSelection;
        MoveButtonControl.IsEnabled = hasSelection;
        DeleteButtonControl.IsEnabled = hasSelection;
        RefreshFolderButtonControl.IsEnabled = isDirectory;
        ExpandFolderButtonControl.IsEnabled = isDirectory;
        CollapseFolderButtonControl.IsEnabled = isDirectory;
    }

    private void OnRefreshFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        item.ReloadProperties();
        item.SetChildren();

        if (DirectoryTreeView.SelectedItem == item)
        {
            UpdateFileInformation(item);
        }

        UpdateToolbarState();
    }

    private void OnExpandFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        item.IsExpanded = true;
    }

    private void OnCollapseFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out ListableItem item) || item.Type != ListableItemType.Directory)
        {
            return;
        }

        item.IsExpanded = false;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplySearchFilter(SearchTextBoxControl.Text ?? string.Empty, selectFirstMatch: true);
    }

    private void ApplySearchFilter(string query, bool selectFirstMatch)
    {
        _currentSearchQuery = query;

        if (RootItems.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            ListableItem? noMatch = null;
            foreach (ListableItem root in RootItems)
            {
                SetSearchVisibilityRecursive(root, string.Empty, ref noMatch);
            }

            return;
        }

        ListableItem? firstMatch = null;
        foreach (ListableItem root in RootItems)
        {
            if (SetSearchVisibilityRecursive(root, query, ref firstMatch) && firstMatch is not null && !selectFirstMatch)
            {
                // ...selection intentionally preserved during refresh...
            }
        }

        if (!selectFirstMatch)
        {
            return;
        }

        if (firstMatch is null)
        {
            DirectoryTreeView.SelectedItem = null;
            ClearFileInformation();
            UpdateToolbarState();
            return;
        }

        ExpandAncestors(firstMatch);
        DirectoryTreeView.SelectedItem = firstMatch;
        UpdateFileInformation(firstMatch);
        UpdateToolbarState();
    }

    private static bool SetSearchVisibilityRecursive(ListableItem item, string query, ref ListableItem? firstMatch)
    {
        bool matchesSelf = string.IsNullOrWhiteSpace(query) ||
                           item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           item.FullPath.Contains(query, StringComparison.OrdinalIgnoreCase);

        bool anyChildVisible = false;
        foreach (ListableItem child in item.Children)
        {
            if (SetSearchVisibilityRecursive(child, query, ref firstMatch))
            {
                anyChildVisible = true;
            }
        }

        bool isVisible = string.IsNullOrWhiteSpace(query) || matchesSelf || anyChildVisible;
        item.IsVisibleInSearch = isVisible;

        if (isVisible && matchesSelf && firstMatch is null && !string.IsNullOrWhiteSpace(query))
        {
            firstMatch = item;
        }

        return isVisible;
    }

    private static void ExpandAncestors(ListableItem item)
    {
        ListableItem? current = item.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }


    private void OnLightModeToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is null)
        {
            return;
        }

        bool isLightMode = LightModeToggleControl.IsChecked ?? false;
        Application.Current.RequestedThemeVariant = isLightMode
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}

