namespace AvaloniaApplication1.Models;

public sealed class TreeItemContextMenuState
{
    public TreeItemContextMenuState(ListableItem item, bool hasCopiedItem)
    {
        Item = item;
        HasCopiedItem = hasCopiedItem;
    }

    public ListableItem Item { get; }
    public bool HasCopiedItem { get; }
    public bool IsDirectory => Item.Type == ListableItemType.Directory;
    public bool ShowPaste => IsDirectory;
    public bool CanPaste => IsDirectory && HasCopiedItem;
    public bool ShowAddFile => IsDirectory;
    public bool ShowRefreshFolder => IsDirectory;
    public bool ShowExpandFolder => IsDirectory;
    public bool ShowCollapseFolder => IsDirectory;
}


