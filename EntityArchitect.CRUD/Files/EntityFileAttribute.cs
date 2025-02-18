using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Files;

public class EntityFileAttribute(string path, params ContentTypes[] contentTypes) : Attribute
{
    public IEnumerable<ContentType> ContentTypes { get; } = contentTypes.Select(c => Enumeration.GetById<ContentType>((int)c));
    public string Path { get; } = path;
}