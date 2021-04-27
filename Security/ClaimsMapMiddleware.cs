using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MiniDemo.Security
{
    public class ClaimsMapMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                await next(context);
                return;
            }

            var currentPrincipalAccessor = context.RequestServices
                .GetRequiredService<ICurrentPrincipalAccessor>();

            var mapOptions = context.RequestServices
                .GetRequiredService<IOptions<ClaimsMapOptions>>().Value;

            var mapClaims = currentPrincipalAccessor
                .Principal
                .Claims
                .Where(claim => mapOptions.Maps.Keys.Contains(claim.Type));

            currentPrincipalAccessor
                .Principal
                .AddIdentity(
                    new ClaimsIdentity(
                        mapClaims
                            .Select(
                                claim => new Claim(
                                    mapOptions.Maps[claim.Type](),
                                    claim.Value,
                                    claim.ValueType,
                                    claim.Issuer
                                )
                            )
                    )
                );

            await next(context);
        }
    }
}
