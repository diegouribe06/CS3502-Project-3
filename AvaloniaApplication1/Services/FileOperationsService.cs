using System;
using System.IO;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

public sealed class FileOperationsService
{
    private enum ClipboardOperation
    {
        None,
        Copy,
        Cut
    }

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
        if (targetItem.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Paste is only available for folders.");
        }

        if (!HasCopiedItem || _copiedPath is null || _copiedType is null)
        {
            return FileOperationResult.Fail("Copy a file or folder first.");
        }

        if (!Directory.Exists(targetItem.FullPath))
        {
            return FileOperationResult.Fail("Destination folder does not exist.");
        }

        string destinationPath = Path.Combine(targetItem.FullPath, Path.GetFileName(_copiedPath));
        if (ArePathsEqual(_copiedPath, destinationPath))
        {
            return FileOperationResult.Fail("Source and destination are the same.");
        }

        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return FileOperationResult.Fail("An item with that name already exists in the destination.");
        }

        FileOperationResult result = Execute(() =>
        {
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

        if (result.IsSuccess && _clipboardOperation == ClipboardOperation.Cut)
        {
            ClearClipboard();
        }

        return result;
    }

    public FileOperationResult AddFile(ListableItem targetFolder, string fileName)
    {
        if (targetFolder.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Files can only be created inside folders.");
        }

        string? validationError = ValidateNewName(fileName, targetFolder.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

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
        if (targetFolder.Type != ListableItemType.Directory)
        {
            return FileOperationResult.Fail("Folders can only be created inside folders.");
        }

        string? validationError = ValidateNewName(folderName, targetFolder.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        string newFolderPath = Path.Combine(targetFolder.FullPath, folderName);

        return Execute(() =>
        {
            Directory.CreateDirectory(newFolderPath);
            return FileOperationResult.Success();
        });
    }

    public FileOperationResult Rename(ListableItem item, string newName, bool isRootItem)
    {
        string? parentDirectory = GetParentDirectoryPath(item);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return FileOperationResult.Fail("Unable to resolve the parent folder for this item.");
        }

        string? validationError = ValidateNewName(newName, parentDirectory, item.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        string destinationPath = Path.Combine(parentDirectory, newName);
        if (ArePathsEqual(item.FullPath, destinationPath))
        {
            return FileOperationResult.Success();
        }

        return Execute(() =>
        {
            if (item.Type == ListableItemType.File)
            {
                File.Move(item.FullPath, destinationPath);
            }
            else
            {
                Directory.Move(item.FullPath, destinationPath);
            }

            UpdateCopiedPathOnMove(item.FullPath, destinationPath);

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
        string? validationError = ValidateDestinationDirectory(destinationDirectory, item.FullPath);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return FileOperationResult.Fail(validationError);
        }

        string destinationPath = Path.Combine(destinationDirectory, item.Name);
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return FileOperationResult.Fail("An item with that name already exists in the destination.");
        }

        return Execute(() =>
        {
            if (item.Type == ListableItemType.File)
            {
                File.Move(item.FullPath, destinationPath);
            }
            else
            {
                Directory.Move(item.FullPath, destinationPath);
            }

            UpdateCopiedPathOnMove(item.FullPath, destinationPath);

            string? updatedRootPath = isRootItem && item.Type == ListableItemType.Directory
                ? destinationPath
                : null;

            return FileOperationResult.Success(updatedRootPath);
        });
    }

    public string? ValidateNewName(string value, string destinationDirectory, string? sourcePath = null)
    {
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
        if (destinationExists && (sourcePath is null || !ArePathsEqual(sourcePath, destinationPath)))
        {
            return "An item with this name already exists.";
        }

        return null;
    }

    public string? ValidateDestinationDirectory(string value, string sourcePath)
    {
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

        if (ArePathsEqual(sourceDirectory, value))
        {
            return "Destination must be a different directory.";
        }

        if (Directory.Exists(sourcePath) && IsSubdirectory(value, sourcePath))
        {
            return "Destination cannot be inside the folder being moved.";
        }

        return null;
    }

    public string? GetParentDirectoryPath(ListableItem item)
    {
        return Path.GetDirectoryName(item.FullPath);
    }

    public bool ArePathsEqual(string left, string right)
    {
        string normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return OperatingSystem.IsWindows()
            ? string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase)
            : string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
    }

    private void UpdateCopiedPathOnMove(string sourcePath, string destinationPath)
    {
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
        _clipboardOperation = ClipboardOperation.None;
        _copiedPath = null;
        _copiedType = null;
    }

    private static bool IsSubdirectory(string candidatePath, string parentPath)
    {
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
            return FileOperationResult.Fail(ex.Message);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
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


