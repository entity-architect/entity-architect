using EntityArchitect.CRUD.Queries;
using EntityArchitect.Example.Entities;

namespace EntityArchitect.Example.Queries;

public class AuthorGetAuthorsWithBooksAndRentalsQuery() : Query<Author>("sql/GetAuthorsWithBooksAndRentals.sql", true);