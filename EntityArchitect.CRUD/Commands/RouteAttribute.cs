namespace EntityArchitect.CRUD.Feature;

public class RouteAttribute(string group, params string[] route) : Attribute
{
    public string Group { get; } = group.ToLower();
    public string Route { get; } = group + "/" + Path.Combine(route).ToLower().Replace("command", "");
    public string FixedRoute { get; } = Path.Combine(route).ToLower().Replace("command", "");
}