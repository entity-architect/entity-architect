using EntityArchitect.CRUD.Enumerations;

namespace notatuj.Enumerations;

public class ContributorType : Enumeration
{
    public ContributorType(int id) : base(id)
    {
    }

    public ContributorType(int id, string name) : base(id, name)
    {
    }

    public static ContributorType Creator = new(1, "Creator");
    public static ContributorType Contributor = new(2, "Contributor");
    public static ContributorType InviteToContribute = new(3, "InviteToContribute");
}