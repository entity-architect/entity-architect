using System.Collections.Generic;
using System.Security.Claims;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;

namespace EntityArchitect.CRUD.Services;

public class ClaimProvider : IClaimProvider
{
    private List<Claim> _claims = [];

    public void SetClaims(List<Claim> claims) => _claims = claims;
    public List<Claim> GetClaims() => _claims;
}