using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Files;

public enum ContentTypes
{
    Text = 1,
    Jpeg = 2,
    Png = 3,
    Gif = 4,
    Bmp = 5,
    Svg = 6,
    Webp = 7,
    Ico = 8,
}

public class ContentType : Enumeration
{
    public IReadOnlyList<string> AcceptedExtensions { get; } 

    public ContentType(int id, string name, string[] acceptedExtensions) : base(id, name)
    {
        AcceptedExtensions = acceptedExtensions;
    }
    
    public ContentType(int id) : base(id)
    {
        
    }
    
    public ContentType(int id, string name) : base(id, name)
    {
        
    }

    public static ContentType Text = new((int)ContentTypes.Text, "text/plain", new[] { ".txt" });
    public static ContentType Jpeg = new((int)ContentTypes.Jpeg, "image/jpeg", new[] { ".jpg", ".jpeg" });
    public static ContentType Png = new((int)ContentTypes.Png, "image/png", new[] { ".png" });
}