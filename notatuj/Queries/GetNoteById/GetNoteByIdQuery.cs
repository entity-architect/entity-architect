using EntityArchitect.CRUD.Queries;
using notatuj.Entities;

namespace notatuj.Queries.GetNoteById;

public class GetNoteByIdQuery() : Query<Note>("Queries/GetNoteById/query.sql", true, true);