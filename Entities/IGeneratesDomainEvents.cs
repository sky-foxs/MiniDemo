using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Entities
{
    public interface IGeneratesDomainEvents
    {
        IEnumerable<object> GetLocalEvents();

        IEnumerable<object> GetDistributedEvents();

        void ClearLocalEvents();

        void ClearDistributedEvents();
    }
}
