using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
public class Book : Entity
{
    public string? Title { get; private set; }

    [OneToMany<Author>(nameof(Author.Books))]
    public Author Author { get; private set;}

    [ManyToOne<Rental>(nameof(Rental.Book))]
    [IgnorePostRequest]
    [IgnorePutRequest]
    public ICollection<Rental> Rentals { get; private set; }
    public BookType BookType { get; private set; }
}