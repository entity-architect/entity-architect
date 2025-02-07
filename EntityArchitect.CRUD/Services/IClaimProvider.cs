using System.Security.Claims;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Services;

public interface IClaimProvider
{
    void SetClaims(List<Claim> claims);
    List<Claim> GetClaims();
}