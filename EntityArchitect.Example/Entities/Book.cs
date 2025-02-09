using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
[Secured(typeof(Client))]
public class Book : Entity
{
    public string? Title { get; private set; }

    [RelationOneToMany<Author>(nameof(Author.Books))]
    [IncludeInGet(1)]
    public Author Author { get; private set;}

    [RelationManyToOne<Rental>(nameof(Rental.Book))]
    [IgnorePostRequest]
    [IgnorePutRequest]
    public List<Rental> Rentals { get; private set; }
    public BookType BookType { get; private set; }
}