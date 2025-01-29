using EntityArchitect.CRUD.Queries;

namespace EntityArchitect.Example.Entities;

public class AuthorGetAuthorsWithBooksQuery() : Query<Author>("sql/GetAuthorsWithBooks.sql", true);