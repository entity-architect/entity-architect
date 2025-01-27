namespace EntityArchitect.CRUD.Queries;

public enum SqlParameterPosition
{
    StartsWith, // parameter%
    EndsWith, // %parameter
    Contains, // %parameter%
    Exact // parameter
}