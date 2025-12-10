using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Requests
{
    /// <summary>
    /// Đối tượng DTO dùng để chứa thông tin yêu cầu cập nhật loại khách hàng hàng loạt (Bulk Update Customer Type)
    /// <para/>Sử dụng trong API cho phép cập nhật trường Loại Khách hàng (CrmCustomerType) cho nhiều bản ghi Customer
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class BulkUpdateCustomerType
    {
        #region Property

        /// <summary>
        /// Danh sách ID (Guid) của các bản ghi Khách hàng cần cập nhật
        /// </summary>
        public List<Guid> Ids { get; set; }

        /// <summary>
        /// Giá trị Loại Khách hàng (Customer Type ID) mới cần gán
        /// </summary>
        public string Value { get; set; }

        #endregion Property
    }
}