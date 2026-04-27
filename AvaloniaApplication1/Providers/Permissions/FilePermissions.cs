namespace AvaloniaApplication1.Providers.Permissions;

public struct FilePermissions
{
    public bool CanRead;
    public bool CanWrite;
    public bool CanExecute;

    public override readonly string ToString()
    {
        return $"Read: {(CanRead ? "Yes" : "No")}, " +
               $"Write: {(CanWrite ? "Yes" : "No")}, " +
               $"Execute: {(CanExecute ? "Yes" : "No")}";
    }
}