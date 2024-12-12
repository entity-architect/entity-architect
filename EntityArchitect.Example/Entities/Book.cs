using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
public class Book : Entity
{
    public string? Title { get; private set; }
    
    [RelationOneToMany<Author>(nameof(Author.Books)), IncludeInGet(1)]
    public Author Author { get;private set; }
    
    [RelationManyToOne<Rental>(nameof(Rental.Book)), IgnorePostRequest, IgnorePutRequest]
    public List<Rental> Rentals { get; private set; }
}