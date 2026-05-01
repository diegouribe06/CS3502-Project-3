namespace AvaloniaApplication1.Providers.Permissions;

/// <summary>
/// Defines the contract for reading permissions on a specific operating system.
/// The factory chooses one implementation, and the rest of the app only calls this interface.
/// </summary>
public interface IPermissionsProvider
{
    /// <summary>
    /// Reads the permission flags for the given file or folder path.
    /// The implementation decides how the operating system should be queried.
    /// </summary>
    public FilePermissions GetPermissions(string filePath);
}