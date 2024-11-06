using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example;

[GetListPaginated(3)]
public class Book : Entity
{
    public string? Title { get; private set; }
    
    [RelationOneToMany<Author>(nameof(Author.Books)), IncludeInGet(1)]
    public Author Author { get;private set; }
}