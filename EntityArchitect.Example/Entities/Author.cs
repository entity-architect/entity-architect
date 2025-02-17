using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using File = EntityArchitect.CRUD.Files.File;

namespace EntityArchitect.Example.Entities;

[Secured(typeof(Entities.Client))]
public class Author : Entity
{
    public string Name { get; set; }
    
    [RelationManyToOne<Book>(nameof(Book.Author)), IgnorePostRequest, IgnorePutRequest]
    public List<Book> Books { get; set;}
    public File Avatar { get; set; }

    public void AddToName(string addedByAction)
    {
        Name += addedByAction;
    }
}