using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Requests
{
    /// <summary>
    /// Đối tượng DTO dùng để chứa thông tin yêu cầu cập nhật hàng loạt (Bulk Update)
    /// <para/>Sử dụng trong các API cho phép cập nhật một trường dữ liệu cho nhiều bản ghi cùng lúc
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class BulkUpdateRequest
    {
        #region Property

        /// <summary>
        /// Danh sách ID (Guid) của các bản ghi cần cập nhật
        /// </summary>
        public List<Guid> Ids { get; set; }

        /// <summary>
        /// Tên cột (Column Name) trong database cần cập nhật giá trị
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Giá trị mới cần gán cho cột đã chỉ định
        /// </summary>
        public int Value { get; set; }

        #endregion Property
    }
}