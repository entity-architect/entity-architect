using EntityArchitect.CRUD.Queries;

namespace EntityArchitect.Example.Entities;

public class AuthorGetAuthorsWithBooksAndRentalsQuery() : Query<Author>("sql/GetAuthorsWithBooksAndRentals.sql", true);