using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Requests
{
    /// <summary>
    /// Đối tượng DTO dùng để chứa thông tin yêu cầu kiểm tra trùng lặp dữ liệu
    /// <para/>Sử dụng trong các API kiểm tra tính duy nhất của một trường dữ liệu (unique check)
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class DuplicateCheckRequest
    {
        #region Property

        /// <summary>
        /// Tên thuộc tính (Property Name) hoặc tên cột (Column Name) cần kiểm tra trùng lặp
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Giá trị cần kiểm tra trùng lặp
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// ID của bản ghi hiện tại cần bỏ qua khi kiểm tra trùng lặp (áp dụng cho nghiệp vụ Sửa/Cập nhật)
        /// </summary>
        public string? IgnoreId { get; set; }

        #endregion Property
    }
}