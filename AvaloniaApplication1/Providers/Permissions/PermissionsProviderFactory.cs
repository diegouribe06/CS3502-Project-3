using System;

namespace AvaloniaApplication1.Providers.Permissions;

/// <summary>
/// Chooses the right permissions reader for the current operating system.
/// This keeps platform checks out of the rest of the application.
/// </summary>
public static class PermissionsProviderFactory
{
    public static IPermissionsProvider Create()
    {
        // Windows uses ACLs, while Linux and macOS use Unix-style permission bits.
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPermissionProvider();
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return new UnixPermissionsProvider();
        }

        // If a new platform is added later, this is where another provider should be plugged in.
        throw new PlatformNotSupportedException("No permissions provider is available for this platform.");
    }
}