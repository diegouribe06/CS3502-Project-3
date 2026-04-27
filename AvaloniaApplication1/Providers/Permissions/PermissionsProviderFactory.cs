using System;

namespace AvaloniaApplication1.Providers.Permissions;

public static class PermissionsProviderFactory
{
    public static IPermissionsProvider Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPermissionProvider();
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return new UnixPermissionsProvider();
        }

        throw new PlatformNotSupportedException("No permissions provider is available for this platform.");
    }
}