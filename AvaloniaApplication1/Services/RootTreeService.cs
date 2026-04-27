using System;
using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

public sealed class RootTreeService
{
    private const string InitialRootEnvironmentVariable = "INITIAL_ROOT";

    public RootTreeResult CreateInitialRootFromEnvironment()
    {
        string? initialRoot = Environment.GetEnvironmentVariable(InitialRootEnvironmentVariable);
        return BuildRoot(initialRoot);
    }

    public RootTreeResult BuildRoot(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return RootTreeResult.Fail("Initial root is not set correctly!");
        }

        string trimmedRootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootName = Path.GetFileName(trimmedRootPath);

        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = rootPath;
        }

        var root = new ListableItem(
            rootName,
            rootPath,
            Directory.GetLastWriteTime(rootPath),
            Directory.GetCreationTime(rootPath),
            ListableItemType.Directory)
        {
            IsExpanded = true
        };

        return RootTreeResult.Success(root);
    }
}

public sealed class RootTreeResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public ListableItem? RootItem { get; }

    private RootTreeResult(bool isSuccess, string? errorMessage, ListableItem? rootItem)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        RootItem = rootItem;
    }

    public static RootTreeResult Success(ListableItem rootItem)
    {
        return new RootTreeResult(true, null, rootItem);
    }

    public static RootTreeResult Fail(string errorMessage)
    {
        return new RootTreeResult(false, errorMessage, null);
    }
}

