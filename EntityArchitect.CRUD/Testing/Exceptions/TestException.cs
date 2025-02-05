using System.Diagnostics;

namespace EntityArchitect.CRUD.Testing.Exceptions;

public class TestException(Exception e, string test) : Exception
{
    public override string Message { get; } = test + " \n" + e.Message;
    public override string? StackTrace { get; } = e.StackTrace;
}