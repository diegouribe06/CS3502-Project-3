using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Views;

public partial class FolderPickerDialog : Window
{
    private readonly RootTreeService _rootTreeService = new();
    private TaskCompletionSource<string?>? _resultSource;
    private readonly TextBlock _selectedPathTextBlock;
    private readonly TreeView _folderTreeView;
    private readonly Button _cancelButton;
    private readonly Button _selectButton;
    private List<ListableItem> _rootItems = new();

    public FolderPickerDialog()
        : this(Directory.GetCurrentDirectory())
    {
    }

    public FolderPickerDialog(string startPath)
    {
        InitializeComponent();

        _selectedPathTextBlock = this.FindControl<TextBlock>("SelectedPathTextBlock")
            ?? throw new InvalidOperationException("SelectedPathTextBlock control was not found.");
        _folderTreeView = this.FindControl<TreeView>("FolderTreeView")
            ?? throw new InvalidOperationException("FolderTreeView control was not found.");
        _cancelButton = this.FindControl<Button>("CancelButton")
            ?? throw new InvalidOperationException("CancelButton control was not found.");
        _selectButton = this.FindControl<Button>("SelectButton")
            ?? throw new InvalidOperationException("SelectButton control was not found.");

        string effectiveStartPath = Directory.Exists(startPath)
            ? startPath
            : Directory.GetCurrentDirectory();

        RootTreeResult rootResult = _rootTreeService.BuildRoot(effectiveStartPath);
        if (!rootResult.IsSuccess || rootResult.RootItem is null)
        {
            throw new DirectoryNotFoundException(rootResult.ErrorMessage ?? "Unable to open the folder picker.");
        }

        _rootItems.Add(rootResult.RootItem);
        _folderTreeView.ItemsSource = _rootItems;
        _folderTreeView.SelectionChanged += OnSelectionChanged;
        _folderTreeView.DoubleTapped += OnFolderTreeViewDoubleTapped;
        _cancelButton.Click += OnCancelClicked;
        _selectButton.Click += OnSelectClicked;
        Closed += (_, _) => _resultSource?.TrySetResult(null);

        _folderTreeView.SelectedItem = rootResult.RootItem;
        UpdateSelection(rootResult.RootItem);
    }

    public Task<string?> ShowAsync(Window owner)
    {
        _resultSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = ShowDialog(owner);
        return _resultSource.Task;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelection(_folderTreeView.SelectedItem as ListableItem);
    }

    private void OnFolderTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_folderTreeView.SelectedItem is ListableItem item && item.Type == ListableItemType.Directory)
        {
            SelectCurrentFolder();
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(null);
        Close();
    }

    private void OnSelectClicked(object? sender, RoutedEventArgs e)
    {
        SelectCurrentFolder();
    }

    private void SelectCurrentFolder()
    {
        if (_folderTreeView.SelectedItem is not ListableItem item || item.Type != ListableItemType.Directory)
        {
            return;
        }

        _resultSource?.TrySetResult(item.FullPath);
        Close();
    }

    private void UpdateSelection(ListableItem? selectedItem)
    {
        bool hasSelection = selectedItem is not null && selectedItem.Type == ListableItemType.Directory;
        _selectButton.IsEnabled = hasSelection;
        _selectedPathTextBlock.Text = hasSelection
            ? $"Selected folder: {selectedItem!.FullPath}"
            : "Select a destination folder";
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}




