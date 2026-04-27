using System;
using System.Collections.Generic;
using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

public sealed class TreeViewStateService
{
    public TreeViewState CaptureState(IEnumerable<ListableItem> roots, ListableItem? selectedItem)
    {
        var expandedPaths = new HashSet<string>(GetPathComparer());
        CaptureExpandedPaths(roots, expandedPaths);

        string? selectedPath = selectedItem is null ? null : NormalizePath(selectedItem.FullPath);
        return new TreeViewState(expandedPaths, selectedPath);
    }

    public ListableItem? RestoreState(IEnumerable<ListableItem> roots, TreeViewState state)
    {
        return RestoreStateRecursive(roots, state);
    }

    private static void CaptureExpandedPaths(IEnumerable<ListableItem> items, ISet<string> expandedPaths)
    {
        foreach (ListableItem item in items)
        {
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
            string normalizedPath = NormalizePath(item.FullPath);
            item.IsExpanded = state.ExpandedPaths.Contains(normalizedPath);

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
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        return OperatingSystem.IsWindows()
            ? string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            : string.Equals(left, right, StringComparison.Ordinal);
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }
}

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

