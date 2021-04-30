using System;

namespace MiniDemo.Data
{
    [Serializable]
    public abstract class CreationAuditedEntity : ICreationAuditedObject
    {
        public virtual DateTime CreationTime { get; protected set; }
        public virtual Guid? CreatorId { get; protected set; }
    }


}
