using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    public interface IHasExtraProperties
    {
        ExtraPropertyDictionary ExtraProperties { get; }
    }
}
