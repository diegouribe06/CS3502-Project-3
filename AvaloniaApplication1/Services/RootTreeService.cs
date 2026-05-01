using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

/// <summary>
/// Builds the initial tree root for the folder that the app starts in.
/// This keeps the startup folder logic in one place instead of spreading it through the UI.
/// </summary>
public sealed class RootTreeService
{

    public RootTreeResult BuildRoot(string? rootPath)
    {
        // If the path is missing or invalid, the app cannot build the tree and should report that clearly.
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return RootTreeResult.Fail("Initial root is not set correctly!");
        }

        // Trim any trailing separator before we try to extract the display name.
        string trimmedRootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootName = Path.GetFileName(trimmedRootPath);

        // Root folders like C:\ or / can return an empty name, so we fall back to the full path for display.
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = rootPath;
        }

        // The root starts expanded so the user immediately sees the first layer of folders.
        var root = new ListableItem(
            rootName,
            rootPath,
            Directory.GetLastWriteTime(rootPath),
            Directory.GetCreationTime(rootPath),
            ListableItemType.Directory);

        // Load only the immediate children so the UI can render quickly.
        root.SetChildren();
        root.IsExpanded = true;

        return RootTreeResult.Success(root);
    }
}

/// <summary>
/// Wraps the result of building the tree root so callers can handle success or failure.
/// Using a result object keeps errors out of the normal return path and makes the calling code simpler.
/// </summary>
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

