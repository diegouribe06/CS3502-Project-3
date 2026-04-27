using System.IO;
using System.Runtime.Versioning;

namespace AvaloniaApplication1.Providers.Permissions;

[UnsupportedOSPlatform("windows")]
public class UnixPermissionsProvider : IPermissionsProvider
{
    public FilePermissions GetPermissions(string filePath)
    {
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            throw new FileNotFoundException($"Path does not exist: {filePath}");
        }

        bool isDirectory = Directory.Exists(filePath);

        // On Unix, this returns rwx mode bits for owner/group/other.
        UnixFileMode mode = File.GetUnixFileMode(filePath);

        return new FilePermissions
        {
            CanRead = HasAny(mode, UnixFileMode.UserRead, UnixFileMode.GroupRead, UnixFileMode.OtherRead),
            CanWrite = HasAny(mode, UnixFileMode.UserWrite, UnixFileMode.GroupWrite, UnixFileMode.OtherWrite),
            CanExecute = !isDirectory && HasAny(mode, UnixFileMode.UserExecute, UnixFileMode.GroupExecute, UnixFileMode.OtherExecute)
        };
    }
    
    //helper method to simplify or checks
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