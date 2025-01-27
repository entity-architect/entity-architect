using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
public class Book : Entity
{
    public string? Title { get; }

    [RelationOneToMany<Author>(nameof(Author.Books))]
    [IncludeInGet(1)]
    public Author Author { get; }

    [RelationManyToOne<Rental>(nameof(Rental.Book))]
    [IgnorePostRequest]
    [IgnorePutRequest]
    public List<Rental> Rentals { get; }
}