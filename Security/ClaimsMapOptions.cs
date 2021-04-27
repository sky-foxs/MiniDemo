using System;
using System.Collections.Generic;

namespace MiniDemo.Security
{
    public class ClaimsMapOptions
    {
        public Dictionary<string, Func<string>> Maps { get; }

        public ClaimsMapOptions()
        {
            Maps = new Dictionary<string, Func<string>>()
            {
                { "sub", () => MiniClaimTypes.UserId },
                { "role", () => MiniClaimTypes.Role },
                { "email", () => MiniClaimTypes.Email },
                { "name", () => MiniClaimTypes.UserName },
                { "family_name", () => MiniClaimTypes.SurName },
                { "given_name", () => MiniClaimTypes.Name }
            };
        }
    }
}
