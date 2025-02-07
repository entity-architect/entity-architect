using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[IncludeInGet(2)]
[Secured(typeof(Client))]
public class Rental : Entity
{
    [RelationOneToMany<Book>(nameof(Entities.Book.Rentals))]
    public Book Book { get; private set; }

    [RelationOneToMany<Client>(nameof(Client.Rentals))]
    public Client Client { get; private set; }

    public DateOnly RentDate { get; private set;}
}