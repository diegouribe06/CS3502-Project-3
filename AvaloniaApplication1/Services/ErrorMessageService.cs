namespace AvaloniaApplication1.Services;

/// <summary>
/// Provides detailed error information for file operation failures.
/// This enriches simple error messages with context and helpful suggestions.
/// </summary>
public sealed class ErrorMessageService
{
    /// <summary>
    /// Contains the detailed information for an error: a short friendly message for everyday users,
    /// an expanded context description, and a suggested next step.
    /// </summary>
    public class ErrorDetail
    {
        // A short, non-technical message suitable for the main dialog text.
        public string FriendlyMessage { get; }

        // A slightly longer explanation of what went wrong (shown in the dialog body).
        public string Context { get; }

        // A suggested action the user can take to recover (shown in the dialog body).
        public string Suggestion { get; }

        public ErrorDetail(string friendlyMessage, string context, string suggestion)
        {
            FriendlyMessage = friendlyMessage;
            Context = context;
            Suggestion = suggestion;
        }
    }

    /// <summary>
    /// Gets detailed error information based on the operation error message.
    /// This helps users understand what went wrong and how to fix it.
    /// </summary>
    public ErrorDetail GetErrorDetail(string errorMessage, string operationType)
    {
        // Match common error patterns and provide context and suggestions for each type.
        // This transforms low-level OS errors into helpful user-facing messages.
        
        if (errorMessage.Contains("Permission denied") || errorMessage.Contains("Unauthorized") || errorMessage.Contains("Access to the path") || errorMessage.Contains("is denied"))
        {
            // Permission errors happen when the user lacks read, write, or execute rights.
            return new ErrorDetail(
                "Permission denied",
                "The operation failed because you do not have permission to access this file or folder.",
                "Try running the application as an administrator, or check the file/folder permissions in your system settings.");
        }

        if (errorMessage.Contains("file in use") || errorMessage.Contains("is being used by another process") || errorMessage.Contains("in use by another"))
        {
            // Files locked by other processes cannot be moved or deleted until they are released.
            return new ErrorDetail(
                "File is in use",
                "The file or folder is currently open or being used by another program.",
                "Close the file in any text editors, or wait for the other program to finish using it, then try again.");
        }

        if (errorMessage.Contains("does not exist") || errorMessage.Contains("Cannot find"))
        {
            // The path no longer exists, usually because it was deleted or moved elsewhere.
            return new ErrorDetail(
                "Not found",
                "The file or folder was not found. It may have been moved, deleted, or the path is incorrect.",
                "Refresh the folder view to see the current state of the filesystem, or check if the item exists elsewhere.");
        }

        if (errorMessage.Contains("already exists"))
        {
            // An item with the same name is in the way of the operation (copy, paste, rename, etc).
            return new ErrorDetail(
                "Name conflict",
                "An item with the same name already exists in the destination.",
                "Try renaming the item with a different name, or delete the existing item first if you want to replace it.");
        }

        if (errorMessage.Contains("is required") || errorMessage.Contains("Name is required"))
        {
            // The user left a required field blank in a dialog.
            return new ErrorDetail(
                "Missing name",
                "A required field (like the file or folder name) was left empty.",
                "Enter a name and try again. Names cannot be empty or contain only spaces.");
        }

        if (errorMessage.Contains("path separators"))
        {
            // The user typed a slash or backslash inside a file name, which is not allowed.
            return new ErrorDetail(
                "Invalid path characters",
                "The name contains path separators like / or \\, which are not allowed in file names.",
                "Remove the slashes from the name. For example, use 'MyFolder' instead of 'My/Folder'.");
        }

        if (errorMessage.Contains("invalid characters"))
        {
            // The OS does not allow certain characters like < > : " | ? * in file names.
            return new ErrorDetail(
                "Invalid characters",
                "The name contains characters that the operating system does not allow.",
                "Avoid using characters like < > : \" | ? * in file names. Try using underscores or hyphens instead.");
        }

        if (errorMessage.Contains("cannot be inside the folder being moved"))
        {
            // Attempting to move a folder into one of its own children creates a circular dependency.
            return new ErrorDetail(
                "Invalid destination",
                "You tried to move a folder into one of its own subfolders, which would create a loop.",
                "Choose a different destination folder that is not inside the folder you want to move.");
        }

        if (errorMessage.Contains("same") || errorMessage.Contains("Destination must be a different directory"))
        {
            // Moving to the same location as the source is a no-op.
            return new ErrorDetail(
                "No-op destination",
                "The destination is the same as the source, so there is nothing to do.",
                "Choose a different destination folder. If you are trying to rename, use the Rename button instead.");
        }

        if (errorMessage.Contains("No such file or directory"))
        {
            // Standard OS error when a path cannot be found.
            return new ErrorDetail(
                "Not found",
                "The specified file or folder could not be found.",
                "Check that the path is correct and the file or folder still exists.");
        }

        if (errorMessage.Contains("Is a directory") && operationType.Contains("File"))
        {
            // The user tried to do a file operation on a folder, or vice versa.
            return new ErrorDetail(
                "Wrong item type",
                "You tried to perform a file operation on a folder, or vice versa.",
                "Make sure you selected the right type of item. Files and folders require different operations.");
        }

        // Provide sensible default guidance for any error we did not recognize.
        return new ErrorDetail(
            "Operation failed",
            "An unexpected error occurred during the operation.",
            "Check that the file or folder exists and that you have the necessary permissions. Try refreshing the folder view.");
    }
}



