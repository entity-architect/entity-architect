namespace EntityArchitect.CRUD.Files;

public class FilePathAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}