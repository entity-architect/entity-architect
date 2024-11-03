using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example;

[HasLightList]
public class Author : Entity
{
    public string Name { get; private set; }
    
    [IgnorePostRequest, IgnorePutRequest]
    [IncludeInGet]
    [RelationManyToOne<Book>(nameof(Book.Author))]
    public List<Book> Books { get; private set; }
}