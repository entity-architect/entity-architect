namespace EntityArchitect.CRUD.Feature;

public class RouteAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}