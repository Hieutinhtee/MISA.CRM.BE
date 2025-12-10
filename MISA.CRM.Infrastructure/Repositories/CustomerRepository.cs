using Dapper;
using MISA.CRM.CORE.Entities;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.Infrastructure.Connection;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace MISA.CRM.Infrastructure.Repositories
{
    /// <summary>
    /// Repository xử lý các thao tác tương tác với Database của Entity Customer
    /// <para/>Triển khai interface ICustomerRepository, kế thừa các thao tác CRUD cơ bản từ BaseRepository
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        #region Constructor

        /// <summary>
        /// Hàm khởi tạo Repository
        /// </summary>
        /// <param name="factory">Factory tạo kết nối MySQL (được tiêm vào)</param>
        /// Created by TMHieu - 7/12/2025
        public CustomerRepository(MySqlConnectionFactory factory)
        : base(factory)
        {
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Lấy mã khách hàng lớn nhất hiện tại trong database
        /// </summary>
        /// <returns>Mã khách hàng lớn nhất (ví dụ: KH202512000001)</returns>
        /// Created by TMHieu - 7/12/2025
        public async Task<string?> GetLastCodeAsync()
        {
            using var connection = Connection;

            // Câu truy vấn lấy mã code lớn nhất. Sử dụng CAST để sắp xếp theo phần số tự tăng của code
            string sql = @" SELECT crm_customer_code
                            FROM crm_customer
                            ORDER BY CAST(RIGHT(crm_customer_code, 6) AS UNSIGNED) DESC
                            LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<string>(sql);
        }

        /// <summary>
        /// Định nghĩa danh sách các trường (cột) cho phép sắp xếp trong chức năng phân trang
        /// </summary>
        /// <returns>HashSet chứa tên các cột cho phép sắp xếp</returns>
        /// Created by TMHieu - 7/12/2025
        protected override HashSet<string> GetSortableFields()
        {
            // Các trường được trả về từ BaseRepository sẽ tự động được thêm vào
            return new HashSet<string>
            {
                // Thêm các trường đặc thù của Customer (nếu cần):
                //"crm_customer_code",
                //"crm_customer_name",
                //"crm_customer_email",
                //"crm_customer_phone_number",
                //"crm_customer_date_of_birth"
            };
        }

        /// <summary>
        /// Định nghĩa danh sách các trường (cột) cho phép tìm kiếm trong chức năng phân trang
        /// </summary>
        /// <returns>HashSet chứa tên các cột cho phép tìm kiếm (sử dụng LIKE)</returns>
        /// Created by TMHieu - 7/12/2025
        protected override HashSet<string> GetSearchFields()
        {
            return new HashSet<string>
            {
                "crm_customer_phone_number",
                "crm_customer_name",
                "crm_customer_email",
                "crm_customer_type"
            };
        }

        /// <summary>
        /// Thực hiện xóa mềm (Soft Delete) hàng loạt cho danh sách ID khách hàng
        /// <para/>Logic: Tăng giá trị `crm_customer_is_deleted` lên 1 so với giá trị lớn nhất hiện có của các bản ghi cùng Email/Phone
        /// </summary>
        /// <param name="ids">Danh sách ID (Guid) khách hàng cần xóa mềm</param>
        /// <returns>Tổng số bản ghi đã bị ảnh hưởng</returns>
        /// Created by TMHieu - 9/12/2025
        public async Task<int> SoftDeleteManyAsync(List<Guid> ids)
        {
            using var conn = (MySqlConnection)Connection;
            await conn.OpenAsync();

            // Bắt đầu Transaction để đảm bảo tính toàn vẹn dữ liệu: tất cả thành công hoặc tất cả thất bại
            using var tran = conn.BeginTransaction();

            int totalAffected = 0;

            foreach (var id in ids)
            {
                // 1) Lấy thông tin Email và Phone của bản ghi cần xóa
                string sqlInfo = @" SELECT
                                        crm_customer_email AS Email,
                                        crm_customer_phone_number AS Phone
                                        FROM crm_customer
                                        WHERE crm_customer_id = @Id AND crm_customer_is_deleted = 0";

                // Tham số 'tran' báo cho Dapper biết câu truy vấn này nằm trong Transaction đang mở
                var info = await conn.QueryFirstOrDefaultAsync<(string Email, string Phone)>(sqlInfo, new { Id = id }, tran);

                if (info.Email == null && info.Phone == null)
                    continue;

                int maxDeleted = 0;

                // 2.1) Kiểm tra Email: Tìm giá trị crm_customer_is_deleted lớn nhất của các bản ghi cùng Email
                if (!string.IsNullOrWhiteSpace(info.Email))
                {
                    string sqlMaxEmail = @" SELECT IFNULL(MAX(crm_customer_is_deleted), 0)
                                            FROM crm_customer
                                            WHERE crm_customer_email = @Email";

                    int maxEmail = await conn.QueryFirstOrDefaultAsync<int>(sqlMaxEmail, new { info.Email }, tran);

                    maxDeleted = Math.Max(maxDeleted, maxEmail);
                }

                // 2.2) Kiểm tra Phone: Tìm giá trị crm_customer_is_deleted lớn nhất của các bản ghi cùng Phone
                if (!string.IsNullOrWhiteSpace(info.Phone))
                {
                    string sqlMaxPhone = @" SELECT IFNULL(MAX(crm_customer_is_deleted), 0)
                                            FROM crm_customer
                                            WHERE crm_customer_phone_number = @Phone";

                    int maxPhone = await conn.QueryFirstOrDefaultAsync<int>(sqlMaxPhone, new { info.Phone }, tran);

                    maxDeleted = Math.Max(maxDeleted, maxPhone);
                }

                // Giá trị crm_customer_is_deleted mới = Max(Deleted) + 1
                int newDeleted = maxDeleted + 1;

                // 3) UPDATE bản ghi hiện tại: Chỉ cập nhật bản ghi chưa bị xóa (crm_customer_is_deleted = 0)
                string sqlUpdate = @" UPDATE crm_customer
                                      SET crm_customer_is_deleted = @NewDeleted
                                      WHERE crm_customer_id = @Id
                                      AND crm_customer_is_deleted = 0"; // Chỉ cập nhật bản ghi đang active (is_deleted = 0)

                totalAffected += await conn.ExecuteAsync(sqlUpdate, new { Id = id, NewDeleted = newDeleted }, tran);
            }

            // Hoàn tất Transaction
            tran.Commit();
            return totalAffected;
        }

        #endregion Method
    }
}