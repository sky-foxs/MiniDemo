using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    /// <summary>
    /// This class can be used to simplify implementing <see cref="IAuditedObject"/>.
    /// </summary>
    [Serializable]
    public abstract class AuditedEntity : CreationAuditedEntity, ICreationAuditedObject, IModificationAuditedObject
    {
        /// <inheritdoc />
        public virtual DateTime? LastModificationTime { get; set; }

        /// <inheritdoc />
        public virtual Guid? LastModifierId { get; set; }
    }

   
}
