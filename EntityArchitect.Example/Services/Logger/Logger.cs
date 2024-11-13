namespace EntityArchitect.Example.Services.Logger;

public class Logger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}