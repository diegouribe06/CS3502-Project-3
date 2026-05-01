using System;
using System.Collections.Generic;
using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

/// <summary>
/// Saves and restores which tree items are expanded and which item is selected.
/// This lets the tree keep its visual state after refreshes or data reloads.
/// </summary>
public sealed class TreeViewStateService
{
    public TreeViewState CaptureState(IEnumerable<ListableItem> roots, ListableItem? selectedItem)
    {
        // Expanded paths are stored in a set so duplicates are ignored automatically.
        var expandedPaths = new HashSet<string>(GetPathComparer());
        CaptureExpandedPaths(roots, expandedPaths);

        // The selected item may be null when nothing is currently highlighted.
        string? selectedPath = selectedItem is null ? null : NormalizePath(selectedItem.FullPath);
        return new TreeViewState(expandedPaths, selectedPath);
    }

    public ListableItem? RestoreState(IEnumerable<ListableItem> roots, TreeViewState state)
    {
        // Restore walks the tree again and re-applies the saved expansion and selection state.
        return RestoreStateRecursive(roots, state);
    }

    private static void CaptureExpandedPaths(IEnumerable<ListableItem> items, ISet<string> expandedPaths)
    {
        foreach (ListableItem item in items)
        {
            if (item.IsPlaceholder)
            {
                continue;
            }

            // Only expanded items are stored because collapsed items already match the default state.
            if (item.IsExpanded)
            {
                expandedPaths.Add(NormalizePath(item.FullPath));
            }

            if (item.Children.Count > 0)
            {
                CaptureExpandedPaths(item.Children, expandedPaths);
            }
        }
    }

    private static ListableItem? RestoreStateRecursive(IEnumerable<ListableItem> items, TreeViewState state)
    {
        foreach (ListableItem item in items)
        {
            if (item.IsPlaceholder)
            {
                continue;
            }

            // Compare normalized paths so restore still works after refreshes or path formatting changes.
            string normalizedPath = NormalizePath(item.FullPath);
            // Each item gets its expanded state back individually.
            item.IsExpanded = state.ExpandedPaths.Contains(normalizedPath);

            // If this item was selected before the refresh, return it so the UI can reselect it.
            if (state.SelectedPath is not null && PathsEqual(normalizedPath, state.SelectedPath))
            {
                return item;
            }

            if (item.Children.Count > 0)
            {
                ListableItem? match = RestoreStateRecursive(item.Children, state);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static string NormalizePath(string path)
    {
        // Normalizing removes extra separators and makes comparisons more predictable.
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        // Windows path comparisons should ignore case, while Unix comparisons should not.
        return OperatingSystem.IsWindows()
            ? string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            : string.Equals(left, right, StringComparison.Ordinal);
    }

    private static StringComparer GetPathComparer()
    {
        // The hash set uses the same case rules as the path comparison helper above.
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }
}

/// <summary>
/// Holds the expanded paths and selected path for the tree view.
/// The service uses this object to remember the tree state between refreshes.
/// </summary>
public sealed class TreeViewState
{
    public TreeViewState(HashSet<string> expandedPaths, string? selectedPath)
    {
        ExpandedPaths = expandedPaths;
        SelectedPath = selectedPath;
    }

    public HashSet<string> ExpandedPaths { get; }
    public string? SelectedPath { get; }
}

