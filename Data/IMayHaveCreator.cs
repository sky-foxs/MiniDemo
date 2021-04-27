using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Data
{
    public interface IMayHaveCreator<TCreator>
    {
        /// <summary>
        /// Reference to the creator.
        /// </summary>
        TCreator Creator { get; }
    }

    /// <summary>
    /// Standard interface for an entity that MAY have a creator.
    /// </summary>
    public interface IMayHaveCreator
    {
        /// <summary>
        /// Id of the creator.
        /// </summary>
        Guid? CreatorId { get; }
    }
}
