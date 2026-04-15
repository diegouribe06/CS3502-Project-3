using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{
    public List<ListableItem> RootItems { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DirectoryTreeView.ItemsSource = RootItems;

        var initialRoot = Environment.GetEnvironmentVariable("INITIAL_ROOT");
        if (string.IsNullOrWhiteSpace(initialRoot) || !Directory.Exists(initialRoot))
        {
            //will eventually implement an error screen
            throw new DirectoryNotFoundException("Initial root is not set correctly!");
        }

        var root = new ListableItem(
            Path.GetFileName(initialRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            initialRoot,
            Directory.GetLastWriteTime(initialRoot),
            Directory.GetCreationTime(initialRoot),
            ListableItemType.Directory);

        RootItems.Add(root);
    }
}