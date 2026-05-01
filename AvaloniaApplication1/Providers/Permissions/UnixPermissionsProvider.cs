using System.IO;
using System.Runtime.Versioning;

namespace AvaloniaApplication1.Providers.Permissions;

/// <summary>
/// Reads simple permission information from Unix file mode bits.
/// It translates the OS-level rwx values into the app's three checkbox-style flags.
/// </summary>
[UnsupportedOSPlatform("windows")]
public class UnixPermissionsProvider : IPermissionsProvider
{
    public FilePermissions GetPermissions(string filePath)
    {
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            throw new FileNotFoundException($"Path does not exist: {filePath}");
        }

        // Folders can be entered, but they are not executed the same way as files.
        bool isDirectory = Directory.Exists(filePath);

        // Unix stores permissions as rwx mode bits for owner, group, and other users.
        // The File API returns the combined mode that we can inspect with bit checks.
        UnixFileMode mode = File.GetUnixFileMode(filePath);

        return new FilePermissions
        {
            CanRead = HasAny(mode, UnixFileMode.UserRead, UnixFileMode.GroupRead, UnixFileMode.OtherRead),
            CanWrite = HasAny(mode, UnixFileMode.UserWrite, UnixFileMode.GroupWrite, UnixFileMode.OtherWrite),
            CanExecute = !isDirectory && HasAny(mode, UnixFileMode.UserExecute, UnixFileMode.GroupExecute, UnixFileMode.OtherExecute)
        };
    }
    
    // This helper keeps the permission checks readable when one permission can come from several roles.
    private static bool HasAny(UnixFileMode mode, params UnixFileMode[] flags)
    {
        foreach (UnixFileMode flag in flags)
        {
            if ((mode & flag) == flag)
            {
                return true;
            }
        }

        return false;
    }
}