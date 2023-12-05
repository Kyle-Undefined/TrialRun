using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network.Models
{
    public class Client : BaseEntity
    {
        public string? RbsDatabase { get; set; }
        public string? RcsDatabase { get; set; }
        public string? VirtualMachine { get; set; }
    }
}
