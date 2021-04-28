using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Entities
{
    public enum EntityChangeType : byte
    {
        Created = 0,

        Updated = 1,

        Deleted = 2
    }
}
