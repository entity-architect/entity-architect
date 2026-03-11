using EntityArchitect.CRUD.Queries;
using EntityArchitect.Example.Entities;

namespace EntityArchitect.Example.Queries;

public class AuthorGetAuthorsWithBooksQuery() : Query<Author>("sql/GetAuthorsWithBooks.sql", true);