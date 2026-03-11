using EntityArchitect.CRUD.Queries;

namespace EntityArchitect.Example.Entities;

public class RentalGetClientsWithRentalsQuery() : Query<Rental>("sql/GetClientsWithRentals.sql", true);