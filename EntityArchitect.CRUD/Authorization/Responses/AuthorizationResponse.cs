namespace EntityArchitect.CRUD.Authorization.Responses;

public class AuthorizationResponse
{
    public string AuthroziationToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime AuthorizationTokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}