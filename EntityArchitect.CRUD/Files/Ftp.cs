namespace EntityArchitect.CRUD.Files;

public class Ftp
{
    public string Host { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string Root { get; set; }
    public int Port { get; set; }
    public string Protocol { get; set; }
    public int Timeout { get; set; }
}