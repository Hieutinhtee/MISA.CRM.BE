using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Responses
{
    /// <summary>
    /// Đối tượng DTO chứa kết quả tổng hợp của quá trình nhập khẩu dữ liệu (Import)
    /// </summary>
    /// Created by TMHieu - 9/12/2025
    public class ImportResult
    {
        #region Property

        /// <summary>
        /// Số lượng bản ghi đã được nhập khẩu thành công (Success)
        /// </summary>
        public int Success { get; set; }

        /// <summary>
        /// Số lượng bản ghi đã nhập khẩu thất bại (Failed)
        /// </summary>
        public int Failed { get; set; }

        #endregion Property
    }
}