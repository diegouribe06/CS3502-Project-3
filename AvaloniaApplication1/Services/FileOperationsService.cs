using System;
using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

/// <summary>
/// Contains the non-UI logic for file and folder actions.
/// The view layer calls into this service when the user clicks toolbar buttons or menu items.
/// </summary>
public sealed class FileOperationsService
{
    private enum ClipboardOperation
    {
        None,
        Copy,
        Cut
    }

    // These fields act like a tiny clipboard so copy and cut can be completed later with paste.
    private string? _copiedPath;
    private ListableItemType? _copiedType;
    private ClipboardOperation _clipboardOperation = ClipboardOperation.None;

    public bool HasCopiedItem => !string.IsNullOrWhiteSpace(_copiedPath) && _copiedType is not null;
    public bool HasClipboardItem => _clipboardOperation != ClipboardOperation.None && HasCopiedItem;
    public bool IsCutOperation => _clipboardOperation == ClipboardOperation.Cut;

    public void Copy(ListableItem item)
    {
        _clipboardOperation = ClipboardOperation.Copy;
        _copiedPath = item.FullPath;
        _copiedType = item.Type;
    }

    public void Cut(ListableItem item)
    {
        _clipboardOperation = ClipboardOperation.Cut;
        _copiedPath = item.FullPath;
        _copiedType = item.Type;
    }

    public FileOperationResult PasteInto(ListableItem targetItem)
    {
        // Paste only makes sense for folders because that is where the new item will be placed.
        if (targetItem.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Paste is only available for folders.");
        }

        // A paste request without a copied item usually means the user has not used Copy or Cut yet.
        if (!HasCopiedItem || _copiedPath is null || _copiedType is null)
        {
            return FileOperationResult.Fail("Copy a file or folder first.");
        }

        // We stop early if the destination folder no longer exists.
        if (!Directory.Exists(targetItem.FullPath))
        {
            return FileOperationResult.Fail("Destination folder does not exist.");
        }

        // Build the final destination path inside the selected folder.
        string destinationPath = Path.Combine(targetItem.FullPath, Path.GetFileName(_copiedPath));
        if (ArePathsEqual(_copiedPath, destinationPath))
        {
            return FileOperationResult.Fail("Source and destination are the same.");
        }

        // Avoid overwriting an existing file or folder with the same name.
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return FileOperationResult.Fail("An item with that name already exists in the destination.");
        }

        FileOperationResult result = Execute(() =>
        {
            // Cut moves the original item, while copy keeps the source in place.
            if (_clipboardOperation == ClipboardOperation.Cut)
            {
                if (_copiedType == ListableItemType.File)
                {
                    File.Move(_copiedPath, destinationPath);
                }
                else
                {
                    Directory.Move(_copiedPath, destinationPath);
                }

                return FileOperationResult.Success();
            }

            if (_copiedType == ListableItemType.File)
            {
                File.Copy(_copiedPath, destinationPath);
            }
            else
            {
                CopyDirectory(_copiedPath, destinationPath);
            }

            return FileOperationResult.Success();
        });

        // Once a cut succeeds, the clipboard should be cleared because the source item has moved.
        if (result.IsSuccess && _clipboardOperation == ClipboardOperation.Cut)
        {
            ClearClipboard();
        }

        return result;
    }

    public FileOperationResult AddFile(ListableItem targetFolder, string fileName)
    {
        // New files must be created inside a folder, not on a file item.
        if (targetFolder.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Files can only be created inside folders.");
        }

        // Reuse the same name checks that rename uses so creation follows the same rules.
        string? validationError = ValidateNewName(fileName, targetFolder.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        // CreateNew makes sure we do not silently overwrite anything.
        string newFilePath = Path.Combine(targetFolder.FullPath, fileName);

        return Execute(() =>
        {
            using var stream = new FileStream(newFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Flush();
            return FileOperationResult.Success();
        });
    }

    public FileOperationResult AddFolder(ListableItem targetFolder, string folderName)
    {
        // Folder creation follows the same rules as file creation: only folders can contain new items.
        if (targetFolder.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Folders can only be created inside folders.");
        }

        // Reuse the name validation so folder creation rejects the same bad inputs as rename.
        string? validationError = ValidateNewName(folderName, targetFolder.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        // CreateDirectory safely makes the folder if it does not already exist.
        string newFolderPath = Path.Combine(targetFolder.FullPath, folderName);

        return Execute(() =>
        {
            Directory.CreateDirectory(newFolderPath);
            return FileOperationResult.Success();
        });
    }

    public FileOperationResult Rename(ListableItem item, string newName, bool isRootItem)
    {
        // Renaming needs the parent folder because the new name stays in the same directory.
        string? parentDirectory = GetParentDirectoryPath(item);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return FileOperationResult.Fail("Unable to resolve the parent folder for this item.");
        }

        // The rename should use the same safety checks as create and move.
        string? validationError = ValidateNewName(newName, parentDirectory, item.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        string destinationPath = Path.Combine(parentDirectory, newName);
        // If the name did not actually change, there is nothing to do.
        if (ArePathsEqual(item.FullPath, destinationPath))
        {
            return FileOperationResult.Success();
        }

        return Execute(() =>
        {
            // Files and folders use different OS calls, so we branch here.
            if (item.Type == ListableItemType.File)
            {
                File.Move(item.FullPath, destinationPath);
            }
            else
            {
                Directory.Move(item.FullPath, destinationPath);
            }

            UpdateCopiedPathOnMove(item.FullPath, destinationPath);

            // If the renamed item was the root folder, the tree root path also needs to change.
            string? updatedRootPath = isRootItem && item.Type == ListableItemType.Directory
                ? destinationPath
                : null;

            return FileOperationResult.Success(updatedRootPath);
        });
    }

    public FileOperationResult Delete(ListableItem item)
    {
        return Execute(() =>
        {
            // Folders are deleted recursively because they may contain files and subfolders.
            if (item.Type == ListableItemType.File)
            {
                File.Delete(item.FullPath);
            }
            else
            {
                Directory.Delete(item.FullPath, recursive: true);
            }

            return FileOperationResult.Success();
        });
    }

    public FileOperationResult Move(ListableItem item, string destinationDirectory, bool isRootItem)
    {
        // The destination must exist and must not be the same directory as the source.
        string? validationError = ValidateDestinationDirectory(destinationDirectory, item.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        // The moved item keeps its own name when it is placed into the new folder.
        string destinationPath = Path.Combine(destinationDirectory, item.Name);
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return FileOperationResult.Fail("An item with that name already exists in the destination.");
        }

        return Execute(() =>
        {
            // Moving files and folders uses the same idea, but different OS methods.
            if (item.Type == ListableItemType.File)
            {
                File.Move(item.FullPath, destinationPath);
            }
            else
            {
                Directory.Move(item.FullPath, destinationPath);
            }

            UpdateCopiedPathOnMove(item.FullPath, destinationPath);

            // Keep the tree root in sync if the user moved the current root folder.
            string? updatedRootPath = isRootItem && item.Type == ListableItemType.Directory
                ? destinationPath
                : null;

            return FileOperationResult.Success(updatedRootPath);
        });
    }

    public string? ValidateNewName(string value, string destinationDirectory, string? sourcePath = null)
    {
        // Keep names simple so we reject empty values, path fragments, and invalid characters early.
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Name is required.";
        }

        if (value != Path.GetFileName(value))
        {
            return "Name cannot contain path separators.";
        }

        if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "Name contains invalid characters.";
        }

        string destinationPath = Path.Combine(destinationDirectory, value);
        bool destinationExists = File.Exists(destinationPath) || Directory.Exists(destinationPath);
        // If the destination already exists and it is not the current item, the operation would overwrite something.
        if (destinationExists && (sourcePath is null || !ArePathsEqual(sourcePath, destinationPath)))
        {
            return "An item with this name already exists.";
        }

        return null;
    }

    public string? ValidateDestinationDirectory(string value, string sourcePath)
    {
        // Moving only works when the destination is a real folder and not the same place as the source.
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Destination directory is required.";
        }

        if (!Directory.Exists(value))
        {
            return "Destination directory does not exist.";
        }

        string sourceDirectory = Directory.Exists(sourcePath)
            ? sourcePath
            : Path.GetDirectoryName(sourcePath) ?? string.Empty;

        // Moving to the current directory would not change anything, so we treat it as invalid.
        if (ArePathsEqual(sourceDirectory, value))
        {
            return "Destination must be a different directory.";
        }

        // A folder cannot be moved into one of its own children because that would create a loop.
        if (Directory.Exists(sourcePath) && IsSubdirectory(value, sourcePath))
        {
            return "Destination cannot be inside the folder being moved.";
        }

        return null;
    }

    public string? GetParentDirectoryPath(ListableItem item)
    {
        // Path.GetDirectoryName gives the folder that contains the current item.
        return Path.GetDirectoryName(item.FullPath);
    }

    public bool ArePathsEqual(string left, string right)
    {
        // Full paths are normalized so comparisons work even if separators differ.
        string normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase)
            : string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
    }

    private void UpdateCopiedPathOnMove(string sourcePath, string destinationPath)
    {
        // If the clipboard item was moved, keep the clipboard path in sync so paste still works.
        if (!HasCopiedItem || _copiedPath is null)
        {
            return;
        }

        if (ArePathsEqual(_copiedPath, sourcePath))
        {
            _copiedPath = destinationPath;
        }
    }

    private void ClearClipboard()
    {
        // Cut operations should not keep pointing at an item that has already been moved.
        _clipboardOperation = ClipboardOperation.None;
        _copiedPath = null;
        _copiedType = null;
    }

    private static bool IsSubdirectory(string candidatePath, string parentPath)
    {
        // Add a trailing separator so "C:\Temp2" does not look like it is inside "C:\Temp".
        string normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        string normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return OperatingSystem.IsWindows()
            ? normalizedCandidate.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase)
            : normalizedCandidate.StartsWith(normalizedParent, StringComparison.Ordinal);
    }

    private static FileOperationResult Execute(Func<FileOperationResult> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // File operations can fail for normal reasons such as permissions or files being in use.
            return FileOperationResult.Fail(ex.Message);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        // Recursively copy the whole folder tree so the copied folder keeps all nested content.
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFilePath in Directory.GetFiles(sourceDirectory))
        {
            string destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath));
            File.Copy(sourceFilePath, destinationFilePath);
        }

        foreach (string sourceSubDirectory in Directory.GetDirectories(sourceDirectory))
        {
            string destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileName(sourceSubDirectory));
            CopyDirectory(sourceSubDirectory, destinationSubDirectory);
        }
    }
}

/// <summary>
/// Holds the outcome of a file operation so the UI can show success or a helpful error message.
/// Some operations also return a new root path when the root folder itself was moved or renamed.
/// </summary>
public sealed class FileOperationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? UpdatedRootPath { get; }

    private FileOperationResult(bool isSuccess, string? errorMessage, string? updatedRootPath)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        UpdatedRootPath = updatedRootPath;
    }

    public static FileOperationResult Success(string? updatedRootPath = null)
    {
        return new FileOperationResult(true, null, updatedRootPath);
    }

    public static FileOperationResult Fail(string errorMessage)
    {
        return new FileOperationResult(false, errorMessage, null);
    }
}


