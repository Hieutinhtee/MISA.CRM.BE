using Microsoft.AspNetCore.Http;
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
        Task<int> ImportFromExcelAsync(IFormFile file);
    }
}