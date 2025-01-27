using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Queries;

namespace EntityArchitect.Example.Entities;

[IncludeInGet(2)]
public class Rental : Entity
{
    [RelationOneToMany<Book>(nameof(Entities.Book.Rentals))]
    public Book Book { get; }

    [RelationOneToMany<Client>(nameof(Client.Rentals))]
    public Client Client { get; }

    public DateOnly RentDate { get; }
}

public class RentalGetClientsWithRentalsQuery() : Query<Rental>("sql/GetClientsWithRentals.sql", true);