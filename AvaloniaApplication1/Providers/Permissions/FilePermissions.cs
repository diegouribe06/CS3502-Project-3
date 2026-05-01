namespace AvaloniaApplication1.Providers.Permissions;

/// <summary>
/// Stores the basic permission flags that the UI shows for one file or folder.
/// This is a small data object, not the logic that reads the permissions.
/// </summary>
public struct FilePermissions
{
    /// <summary>
    /// True when the user can read the item.
    /// </summary>
    public bool CanRead;

    /// <summary>
    /// True when the user can change or create data in the item.
    /// </summary>
    public bool CanWrite;

    /// <summary>
    /// True when the item can be executed or entered, depending on the platform.
    /// </summary>
    public bool CanExecute;

    /// <summary>
    /// Converts the flags into a sentence that is easier to display in the summary panel.
    /// </summary>
    public override readonly string ToString()
    {
        return $"Read: {(CanRead ? "Yes" : "No")}, " +
               $"Write: {(CanWrite ? "Yes" : "No")}, " +
               $"Execute: {(CanExecute ? "Yes" : "No")}";
    }
}