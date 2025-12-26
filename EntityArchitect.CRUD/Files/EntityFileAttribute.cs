using System;
using System.Collections.Generic;
using System.Linq;
using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Files;

public class EntityFileAttribute(string path,  params ContentTypes[] contentTypes) : Attribute
{
    public IEnumerable<ContentType> ContentTypes { get; } = contentTypes.Select(c => Enumeration.GetById<ContentType>((int)c));
    public string Path { get; } = path;
    public string? DefaultValue { get; } = null;
    public Tuple<int,int>? MinFileSize { get; set; } = null;

    public EntityFileAttribute(string path, string? defaultValue,  params ContentTypes[] contentTypes) : this(path, contentTypes)
    {
        DefaultValue = defaultValue;
    }
    
    public EntityFileAttribute(string path, string? defaultValue, Tuple<int,int> minFileSize, params ContentTypes[] contentTypes) : this(path, contentTypes)
    {
        DefaultValue = defaultValue;
        MinFileSize = minFileSize;
        
        if(minFileSize is not null && contentTypes.Any(c => 
               c is Files.ContentTypes.Bmp or 
                   Files.ContentTypes.Gif or 
                   Files.ContentTypes.Jpeg or 
                   Files.ContentTypes.Png
           ))       
            MinFileSize = minFileSize;
        else
            throw new ArgumentException("MinFileSize can only be set for image content types (Bmp, Gif, Jpeg, Png).");
    }
}

public class MinFileAttribute(int x, int y) : Attribute
{
    public int X {get; set;} = x;
    public int Y {get; set;} = y;
}