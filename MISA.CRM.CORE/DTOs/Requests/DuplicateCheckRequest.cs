using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Requests
{
    public class DuplicateCheckRequest
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public string? IgnoreId { get; set; }
    }
}