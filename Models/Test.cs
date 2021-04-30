using MiniDemo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Models
{
    public class Test :FullAuditedEntity
    {
        public Guid Id { get; set; }

        public string Content { get; set; }

      
    }
}
