using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Files;

public class File : ValueObject
{
    public Guid? Id { get; set; }
    public string? Extension { get; set; }
    public string? Hash { get; set; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Extension;
        yield return Hash;
    }
}