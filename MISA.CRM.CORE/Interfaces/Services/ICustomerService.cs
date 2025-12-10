using Microsoft.AspNetCore.Http;
using MISA.CRM.CORE.DTOs.Responses;
using MISA.CRM.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.Interfaces.Services
{
    public interface ICustomerService : IBaseService<Customer>
    {
        /// <summary>
        /// Lấy mã khách hàng tiếp theo
        /// Created by: TMHieu (07/12/2025)
        /// </summary>
        /// <returns>Mã khách hàng tiếp theo</returns>
        Task<string> NextCodeAsync();

        /// <summary>
        /// Import dữ liệu từ Stream CSV và lưu vào DB
        /// </summary>
        /// <param name="fileStream">Stream của file CSV</param>
        /// <returns>Số bản ghi insert thành công</returns>
        /// Created By: TMHieu (9/12/2025)
        Task<ImportResult> ImportFromExcelAsync(IFormFile file);

        /// <summary>
        /// Thực hiện xóa mềm (Soft Delete) hàng loạt cho danh sách ID khách hàng
        /// <para/>Logic: Tăng giá trị `crm_customer_is_deleted` lên 1 so với giá trị lớn nhất hiện có của các bản ghi cùng Email/Phone
        /// </summary>
        /// <param name="ids">Danh sách ID (Guid) khách hàng cần xóa mềm</param>
        /// <returns>Tổng số bản ghi đã bị ảnh hưởng</returns>
        /// Created by TMHieu - 8/12/2025
        Task<int> SoftDeleteManyAsync(List<Guid> ids);
    }
}