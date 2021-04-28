using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Audit
{
    public interface IAuditPropertySetter
    {
        void SetCreationProperties(object targetObject);

        void SetModificationProperties(object targetObject);

        void SetDeletionProperties(object targetObject);
    }
}
