using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    /// <summary>
    /// Implements <see cref="IFullAuditedObject"/> to be a base class for full-audited entities.
    /// </summary>
    [Serializable]
    public abstract class FullAuditedEntity : AuditedEntity, ICreationAuditedObject, IModificationAuditedObject,IDeletionAuditedObject
    {
        /// <inheritdoc />
        public virtual bool IsDeleted { get; set; }

        /// <inheritdoc />
        public virtual Guid? DeleterId { get; set; }

        /// <inheritdoc />
        public virtual DateTime? DeletionTime { get; set; }
    }

   
}
