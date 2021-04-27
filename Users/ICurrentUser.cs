using System;
using System.Security.Claims;

namespace MiniDemo.Users
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        Guid? Id { get; }

        string UserName { get; }

        string Name { get; }

        string SurName { get; }

        string PhoneNumber { get; }

        string Email { get; }

        Guid? TenantId { get; }

        string[] Roles { get; }

        Claim FindClaim(string claimType);

        Claim[] FindClaims(string claimType);

        Claim[] GetAllClaims();

        bool IsInRole(string roleName);
    }
}
