using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Tenant
{
    /// <summary>
    /// to do
    /// </summary>
    public interface ICurrentTenant
    {
        bool IsAvailable { get; }

        Guid? Id { get; }

        string Name { get; }

        IDisposable Change(Guid? id, string name = null);
    }
}
