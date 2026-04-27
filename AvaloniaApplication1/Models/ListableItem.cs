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
        set => SetField(ref _isExpanded, value);
    }

    private bool _isVisibleInSearch = true;
    public bool IsVisibleInSearch
    {
        get => _isVisibleInSearch;
        set => SetField(ref _isVisibleInSearch, value);
    }

    public bool IsDirectory => Type == ListableItemType.Directory;
    public IEnumerable<ListableItem> DirectoryChildren => _children.Where(child => child.Type == ListableItemType.Directory);
    public string Extension
    {
        get => Type == ListableItemType.File ? Path.GetExtension(FullPath) : string.Empty;
    }
    #endregion

    //Set children works recursively. It checks for anything that could be considered a child in the current directory
    //and adds it to the children list. Since the constructor calls this method, it is functionally recursive as new
    //objects are made when adding to children.
    public void SetChildren()
    {
        //make sure old children aren't present
        _children.Clear();

        //handle non-folders
        if (Type == ListableItemType.File)
        {
            return;
        }
        else if (Type == ListableItemType.Directory)
        {
            //get all the child directories
            foreach (string directory in Directory.GetDirectories(FullPath))
            {
                string folderName = Path.GetFileName(directory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));

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

            //get all the child files
            foreach (string file in Directory.GetFiles(FullPath))
            {
                string fileName = Path.GetFileName(file.TrimEnd());
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
        }

        // Folder-only projections bind to this property.
        OnPropertyChanged(nameof(DirectoryChildren));
    }

    
    public ListableItem(string name, string path, DateTime lastModified, DateTime created, ListableItemType type, ListableItem? parent = null)
    {
        //set the current directory's properties
        Name = name;
        FullPath = path;
        Parent = parent;
        LastModified = lastModified;
        Created = created;
        Type = type;
        
        //make sure the current directory properties are valid and then get children
        ValidatePath();
        
        //use a factory to get the correct permissions provider based on the host os
        var permissionsProvider = PermissionsProviderFactory.Create();
        Permissions = permissionsProvider.GetPermissions(FullPath);
        
        if (Permissions.CanRead)
        {
            SetChildren();
        }
        else
        {
            Errors.Add($"Insufficient permissions to read {FullPath}");
        }
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