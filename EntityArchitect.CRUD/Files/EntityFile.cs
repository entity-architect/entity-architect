using System.Security.Cryptography;
using EntityArchitect.CRUD.Entities.Entities;
using SHA256 = SshNet.Security.Cryptography.SHA256;

namespace EntityArchitect.CRUD.Files;

public class EntityFile : ValueObject
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
    
    public static EntityFile Create(IFormFile file)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var hash = ComputeMd5(file);
        
        return new EntityFile
        {
            Id = id,
            Extension = extension,
            Hash = hash
        };
    }
    
    static string ComputeMd5(IFormFile file)
    {
        using (HashAlgorithm hashAlgorithm = MD5.Create())
        using (var stream = file.OpenReadStream()) 
        {
         byte[] hashBytes = hashAlgorithm.ComputeHash(stream);
         return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}