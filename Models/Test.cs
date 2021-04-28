using MiniDemo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Models
{
    public class Test : IDeletionAuditedObject
    {
        public Guid Id { get; set; }

        public string Content { get; set; }

        public bool IsDeleted { get; set; }
        public Guid? DeleterId { get; set; }
        public DateTime? DeletionTime { get; set; }
    }
}
