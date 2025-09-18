using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Files;

namespace EntityArchitect.Example.Entities;

public class Author : Entity
{
    public string Name { get; set; }
    
    [ManyToOne<Book>(nameof(Book.Author)), IgnorePostRequest, IgnorePutRequest]
    public ICollection<Book> Books { get; set;}
    

    public void AddToName(string addedByAction)
    {
        Name += addedByAction;
    }
}