using MISA.CRM.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.Interfaces.Repositories
{
    /// <summary>
    /// Interface định nghĩa các phương thức đặc thù cho nghiệp vụ Khách hàng (Customer)
    /// <para/>Các phương thức liên quan đến thao tác Database (Repository)
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public interface ICustomerRepository : IBaseRepository<Customer>
    {
        /// <summary>
        /// Lấy mã khách hàng lớn nhất hiện tại trong database
        /// </summary>
        /// <returns>Mã khách hàng lớn nhất (ví dụ: KH202512000001)</returns>
        /// Created by TMHieu - 7/12/2025
        Task<string?> GetLastCodeAsync();

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