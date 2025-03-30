using EntityArchitect.CRUD.Queries;
using notatuj.Entities;

namespace notatuj.Queries.GetExamsByNoteId;

public class GetExamsByNoteIdQuery() : Query<Exam>("Queries/GetExamsByNoteId/query.sql", true, true);