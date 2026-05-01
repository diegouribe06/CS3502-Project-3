using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using AvaloniaApplication1.Providers.Permissions;

namespace AvaloniaApplication1.Models;

public class ListableItem : INotifyPropertyChanged
{
    #region Properties
    public string Name { get; }
    public string FullPath { get; }
    public ListableItem? Parent { get; }
    public bool IsPlaceholder { get; }
    private DateTime _lastModified;
    public DateTime LastModified
    {
        get => _lastModified;
        private set => SetField(ref _lastModified, value);
    }

    private DateTime _created;
    public DateTime Created
    {
        get => _created;
        private set => SetField(ref _created, value);
    }

    private readonly ObservableCollection<ListableItem> _children = new();
    private bool _childrenLoaded;

    public ObservableCollection<ListableItem> Children
    {
        get => _children;
    }

    private ListableItemType _type;
    public ListableItemType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                OnPropertyChanged(nameof(IsDirectory));
                OnPropertyChanged(nameof(Extension));
            }
        }
    }
    
    private FilePermissions _permissions;
    public FilePermissions Permissions
    {
        get => _permissions;
        set => SetField(ref _permissions, value);
    }

    public List<string> Errors { get; set; } = new();
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetField(ref _isExpanded, value) && value && Type == ListableItemType.Directory)
            {
                EnsureChildrenLoaded();
            }
        }
    }

    private bool _isVisibleInSearch = true;
    public bool IsVisibleInSearch
    {
        get => _isVisibleInSearch;
        set => SetField(ref _isVisibleInSearch, value);
    }

    public bool IsDirectory => Type == ListableItemType.Directory;
    public IEnumerable<ListableItem> DirectoryChildren => _children.Where(child => child.Type == ListableItemType.Directory);
    public IEnumerable<ListableItem> FolderPickerChildren => _children.Where(child => child.IsPlaceholder || child.Type == ListableItemType.Directory);
    public bool HasLoadedChildren => _childrenLoaded;
    public string Extension
    {
        get => Type == ListableItemType.File ? Path.GetExtension(FullPath) : string.Empty;
    }
    #endregion

    //Loads only the direct children for the current directory.
    //Child items are created without recursively loading their own descendants so startup stays fast.
    public void SetChildren()
    {
        EnsureChildrenLoaded(forceReload: true);
    }

    /// <summary>
    /// Ensures the direct children of this item are loaded once.
    /// Use <paramref name="forceReload"/> when a refresh is needed.
    /// </summary>
    public void EnsureChildrenLoaded(bool forceReload = false)
    {
        if (Type == ListableItemType.File)
        {
            _childrenLoaded = true;
            return;
        }

        if (_childrenLoaded && !forceReload)
        {
            return;
        }

        if (!Permissions.CanRead)
        {
            if (forceReload)
            {
                _children.Clear();
            }

            if (_children.Count == 0)
            {
                _children.Add(CreatePlaceholderChild(this));
            }

            Errors.Add($"Insufficient permissions to read {FullPath}");
            _childrenLoaded = true;
            OnPropertyChanged(nameof(DirectoryChildren));
            OnPropertyChanged(nameof(FolderPickerChildren));
            return;
        }

        //make sure old children aren't present
        _children.Clear();

        if (Type == ListableItemType.Directory)
        {
            //get all the child directories with error handling for permission denied
            try
            {
                foreach (string directory in Directory.GetDirectories(FullPath))
                {
                    string folderName = Path.GetFileName(directory.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar));

                    try
                    {
                        _children.Add(
                            new ListableItem
                            (
                                folderName,
                                directory,
                                Directory.GetLastWriteTime(directory),
                                Directory.GetCreationTime(directory),
                                ListableItemType.Directory,
                                this
                            )
                        );
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        // Skip this subdirectory and record the error
                        Errors.Add($"Cannot access folder '{folderName}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // If we can't enumerate directories at all, record it but continue to files
                Errors.Add($"Cannot enumerate subdirectories: {ex.Message}");
            }

            //get all the child files with error handling for permission denied
            try
            {
                foreach (string file in Directory.GetFiles(FullPath))
                {
                    string fileName = Path.GetFileName(file.TrimEnd());
                    try
                    {
                        _children.Add(
                            new ListableItem
                                (
                                    fileName,
                                    file,
                                    File.GetLastWriteTime(file),
                                    File.GetCreationTime(file),
                                    ListableItemType.File,
                                    this
                                )
                            );
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        // Skip this file and record the error
                        Errors.Add($"Cannot access file '{fileName}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // If we can't enumerate files at all, record it
                Errors.Add($"Cannot enumerate files: {ex.Message}");
            }
        }

        _childrenLoaded = true;

        // Folder-only projections bind to this property.
        OnPropertyChanged(nameof(DirectoryChildren));
        OnPropertyChanged(nameof(FolderPickerChildren));
    }

    
    public ListableItem(string name, string path, DateTime lastModified, DateTime created, ListableItemType type, ListableItem? parent = null, bool createPlaceholder = false)
    {
        //set the current directory's properties
        Name = name;
        FullPath = path;
        Parent = parent;
        LastModified = lastModified;
        Created = created;
        Type = type;

        if (createPlaceholder)
        {
            IsPlaceholder = true;
            IsVisibleInSearch = false;
            _childrenLoaded = true;
            return;
        }
        
        //make sure the current directory properties are valid
        ValidatePath();
        
        //use a factory to get the correct permissions provider based on the host os
        var permissionsProvider = PermissionsProviderFactory.Create();
        Permissions = permissionsProvider.GetPermissions(FullPath);
        
        if (Type == ListableItemType.File)
        {
            _childrenLoaded = true;
        }
        else
        {
            _children.Add(CreatePlaceholderChild(this));
        }
    }

    private static ListableItem CreatePlaceholderChild(ListableItem parent)
    {
        return new ListableItem(string.Empty, parent.FullPath, DateTime.MinValue, DateTime.MinValue, ListableItemType.File, parent, createPlaceholder: true);
    }

    //used to make sure that the path given is a valid path
    //should only really be critical for the initial path
    public void ValidatePath()
    {
        if (Type == ListableItemType.File)
        {
            if (!File.Exists(FullPath))
            {
                throw new FileNotFoundException("The specified file path does not exist! Path: " + FullPath);
            }
        }
        else if (Type == ListableItemType.Directory)
        {
            if (!Directory.Exists(FullPath))
            {
                throw new DirectoryNotFoundException("The specified directory path does not exist! Path: " + FullPath);
            }
        }
    }

    /// <summary>
    /// Reloads file-system metadata that may change over time.
    /// </summary>
    public void ReloadProperties()
    {
        try
        {
            if (Type == ListableItemType.File)
            {
                LastModified = File.GetLastWriteTime(FullPath);
                Created = File.GetCreationTime(FullPath);
            }
            else
            {
                LastModified = Directory.GetLastWriteTime(FullPath);
                Created = Directory.GetCreationTime(FullPath);
            }

            var permissionsProvider = PermissionsProviderFactory.Create();
            Permissions = permissionsProvider.GetPermissions(FullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Errors.Add($"Unable to reload properties for {FullPath}: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
        {
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

//Helper struct to make file type comparisons a bit easier
public enum ListableItemType
{
    File,
    Directory
}