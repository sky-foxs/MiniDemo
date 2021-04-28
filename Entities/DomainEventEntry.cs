using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Entities
{
    [Serializable]
    public class DomainEventEntry
    {
        public object SourceEntity { get; }

        public object EventData { get; }

        public DomainEventEntry(object sourceEntity, object eventData)
        {
            SourceEntity = sourceEntity;
            EventData = eventData;
        }
    }
}
