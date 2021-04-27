using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    /// <summary>
    /// This interface can be implemented to store creation information (who and when created).
    /// </summary>
    public interface ICreationAuditedObject : IHasCreationTime, IMayHaveCreator
    {

    }

    /// <summary>
    /// Adds navigation property (object reference) to <see cref="ICreationAuditedObject"/> interface.
    /// </summary>
    /// <typeparam name="TCreator">Type of the user</typeparam>
    public interface ICreationAuditedObject<TCreator> : ICreationAuditedObject, IMayHaveCreator<TCreator>
    {

    }
}
