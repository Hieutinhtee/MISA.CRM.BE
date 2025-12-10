using Microsoft.AspNetCore.Mvc;
using MISA.CRM.CORE.DTOs.Requests;
using MISA.CRM.CORE.DTOs.Responses;
using MISA.CRM.CORE.Entities;
using MISA.CRM.CORE.Exceptions;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.CORE.Interfaces.Services;
using MISA.CRM.CORE.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace MISA.CRM.API.Controllers
{
    /// <summary>
    /// Controller cung cấp các API xử lý nghiệp vụ liên quan đến Khách hàng (Customer)
    /// <para/>Sử dụng để giao tiếp với Client trong các nghiệp vụ CRUD và xử lý dữ liệu khách hàng
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<Customer>
    {
        #region Declaration

        private readonly ICustomerService _customerService;

        #endregion Declaration

        #region Constructor

        /// <summary>
        /// Hàm khởi tạo Controller
        /// </summary>
        /// <param name="customerService">Service xử lý nghiệp vụ khách hàng được tiêm vào (Dependency Injection)</param>
        /// Created by TMHieu - 7/12/2025
        public CustomersController(ICustomerService customerService)
            : base(customerService)
        {
            _customerService = customerService;
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Lấy mã khách hàng mới tự động tăng
        /// <para/>Sử dụng khi hiển thị form thêm mới khách hàng để gợi ý mã code tiếp theo
        /// </summary>
        /// <returns>Mã khách hàng mới (Ví dụ: KH0001)</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpGet("next-code")]
        public async Task<IActionResult> GetNextCode()
        {
            var code = await _customerService.NextCodeAsync();
            return Ok(new { CustomerCode = code });
        }

        /// <summary>
        /// Cập nhật cùng loại khách hàng cho nhiều bản ghi
        /// <para/>Sử dụng trong chức năng chọn nhiều dòng và cập nhật hàng loạt trên giao diện danh sách
        /// </summary>
        /// <param name="request">Thông tin danh sách id, tên cột, giá trị mới cần cập nhật</param>
        /// <returns>Số bản ghi đã được cập nhật thành công</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpPost("update-type")]
        public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateCustomerType request)
        {
            if (request == null || request.Ids == null || request.Ids.Count == 0)
                throw new ValidateException("Danh sách ID không được rỗng.");
            if (request.Ids == null || string.IsNullOrWhiteSpace(request.Value))
                throw new ValidateException("Loại khách hàng không hợp lệ.");

            // Lấy tên cột trong database dựa trên Attribute của property CrmCustomerType
            var columnName = typeof(Customer).GetProperty(nameof(Customer.CrmCustomerType))?.GetCustomAttribute<ColumnAttribute>()?.Name;

            if (columnName == null)
            {
                throw new NotFoundException("Không tìm thấy cột CrmCustomerType trong bảng Customer", "Lỗi khi sửa loại khách hàng, vui lòng thử lại sau ít phút");
            }

            try
            {
                // Gọi service thực hiện update hàng loạt
                int updatedCount = await _service.BulkUpdateSameValueAsync(request.Ids, columnName, request.Value);
                return Ok(new { updatedCount });
            }
            catch (Exception ex)
            {
                throw new ValidateException($"Lỗi khi cập nhật: {ex.Message}");
            }
        }

        /// <summary>
        /// private Import dữ private liệu từ file CSV
        /// </summary>
        /// <private param name = "file" > File private CSV được private gửi lên private từ form data</param>
        /// <returns>private Số bản private ghi đã private insert thành công</returns>
        /// private Created by TMHieu - 7/12/2025

        [HttpPost("import")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File rỗng.");

            // Gọi service để đọc và import dữ liệu từ stream
            ImportResult res = await _customerService.ImportFromExcelAsync(file);

            return Ok(res);
        }

        /// <summary>
        /// Thực hiện xóa mềm (soft delete) hàng loạt các bản ghi Khách hàng
        /// <para/>Sử dụng phương thức HTTP PUT để cập nhật trạng thái xóa mềm thay vì DELETE
        /// </summary>
        /// <param name="ids">Danh sách ID (Guid) của các bản ghi Khách hàng cần xóa mềm</param>
        /// <returns>Kết quả HTTP 200 OK kèm theo số lượng bản ghi đã bị ảnh hưởng</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpPut("soft-delete-many")]
        public async Task<IActionResult> SoftDeleteMany([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                // Ném lỗi ValidateException để Middleware bắt, thay vì trả về BadRequest trực tiếp
                throw new ValidateException("Danh sách Id trống.", "Danh sách ID không được để trống.");
            }

            int affected = await _customerService.SoftDeleteManyAsync(ids);

            return Ok(new
            {
                TotalAffected = affected
            });
        }

        #endregion Method
    }
}