using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace AvaloniaApplication1.Providers.Permissions;

[SupportedOSPlatform("windows")]
public class WindowsPermissionProvider : IPermissionsProvider
{
    public FilePermissions GetPermissions(string filePath)
    {
        bool isDirectory = Directory.Exists(filePath);
        if (!isDirectory && !File.Exists(filePath))
            throw new FileNotFoundException($"Path does not exist: {filePath}");

        FileSystemSecurity security = isDirectory
            ? new DirectoryInfo(filePath).GetAccessControl()
            : new FileInfo(filePath).GetAccessControl();

        AuthorizationRuleCollection rules = security.GetAccessRules(
            includeExplicit: true,
            includeInherited: true,
            targetType: typeof(SecurityIdentifier));

        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var sids = new HashSet<SecurityIdentifier>();

        if (identity.User is not null)
            sids.Add(identity.User);

        if (identity.Groups is not null)
        {
            foreach (IdentityReference group in identity.Groups)
            {
                if (group is SecurityIdentifier sid)
                    sids.Add(sid);
            }
        }

        FileSystemRights allow = 0;
        FileSystemRights deny = 0;

        foreach (FileSystemAccessRule rule in rules)
        {
            if (!sids.Contains((SecurityIdentifier)rule.IdentityReference))
                continue;

            if (rule.AccessControlType == AccessControlType.Deny)
                deny |= rule.FileSystemRights;
            else
                allow |= rule.FileSystemRights;
        }

        FileSystemRights effective = allow & ~deny;

        const FileSystemRights readMask =
            FileSystemRights.ReadData |
            FileSystemRights.ListDirectory |
            FileSystemRights.ReadAttributes |
            FileSystemRights.ReadExtendedAttributes |
            FileSystemRights.ReadPermissions;

        const FileSystemRights writeMask =
            FileSystemRights.WriteData |
            FileSystemRights.CreateFiles |
            FileSystemRights.AppendData |
            FileSystemRights.CreateDirectories |
            FileSystemRights.WriteAttributes |
            FileSystemRights.WriteExtendedAttributes;

        const FileSystemRights executeMask =
            FileSystemRights.ExecuteFile |
            FileSystemRights.Traverse;

        return new FilePermissions
        {
            CanRead = (effective & readMask) != 0,
            CanWrite = (effective & writeMask) != 0,
            CanExecute = (effective & executeMask) != 0
        };
    }
}
