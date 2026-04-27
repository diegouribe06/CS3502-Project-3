namespace AvaloniaApplication1.Providers.Permissions;

public interface IPermissionsProvider
{
    public FilePermissions GetPermissions(string filePath);
}