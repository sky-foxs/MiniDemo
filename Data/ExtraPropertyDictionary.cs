using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    [Serializable]
    public class ExtraPropertyDictionary : Dictionary<string, object>
    {
        public ExtraPropertyDictionary()
        {

        }

        public ExtraPropertyDictionary(IDictionary<string, object> dictionary)
            : base(dictionary)
        {
        }
    }
}
