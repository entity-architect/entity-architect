using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Attributes.CrudAttributes;

namespace EntityArchitect.Example.Entities;

[HasLightList]
[GetListPaginated(3)]
public class Author : Entity
{
    [LightListProperty] public string Name { get; private set; }

    [RelationManyToOne<Book>(nameof(Book.Author))]
    [IgnorePostRequest]
    [IgnorePutRequest]
    [IncludeInGet(1)]
    public List<Book> Books { get; private set;}

    public void AddToName(string addedByAction)
    {
        Name += addedByAction;
    }
}