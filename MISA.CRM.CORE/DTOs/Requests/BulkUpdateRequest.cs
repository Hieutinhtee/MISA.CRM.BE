using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Requests
{
    public class BulkUpdateRequest
    {
        public List<Guid> Ids { get; set; }
        public string ColumnName { get; set; }
        public int Value { get; set; }
    }
}