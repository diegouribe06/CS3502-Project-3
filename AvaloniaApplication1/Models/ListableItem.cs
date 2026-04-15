using System;
using System.Collections.Generic;
using System.IO;

namespace AvaloniaApplication1.Models;

public class ListableItem
{
    public string Name { get; }
    public string FullPath { get; }
    public DateTime LastModified { get; }
    public DateTime Created { get; }
    private List<ListableItem> _children = new();

    public List<ListableItem> Children
    {
        get => _children;
    }

    public void SetChildren()
    {
        _children.Clear();

        //handle non-folders
        if (Type == ListableItemType.File)
        {
        }
        else if (Type == ListableItemType.Directory)
        {
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
                        ListableItemType.Directory
                    )
                );
            }

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
                            ListableItemType.File
                        )
                    );
            }
        }
        
        
    }

    public ListableItemType Type { get; set; }
    
    public ListableItem(string name, string path, DateTime lastModified, DateTime created, ListableItemType type)
    {
        Name = name;
        FullPath = path;
        LastModified = lastModified;
        Created = created;
        Type = type;
        
        ValidatePath();
        SetChildren();
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
    
}

public enum ListableItemType
{
    File,
    Directory
}