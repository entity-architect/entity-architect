using EntityArchitect.CRUD.Queries;
using notatuj.Entities;

namespace notatuj.Queries.GetNotesByUserId;

public class GetNotesByUserIdQuery() : Query<Note>("Queries/GetNotesByUserId/query.sql", true, false);