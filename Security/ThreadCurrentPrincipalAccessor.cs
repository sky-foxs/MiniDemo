using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MiniDemo.Security
{
    public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessorBase
    {
        protected override ClaimsPrincipal GetClaimsPrincipal()
        {
            return Thread.CurrentPrincipal as ClaimsPrincipal;
        }
    }
}
