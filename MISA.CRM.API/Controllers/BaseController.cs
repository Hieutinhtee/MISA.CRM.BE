using Microsoft.AspNetCore.Mvc;
using MISA.CRM.Core.DTOs.Responses;
using MISA.CRM.CORE.DTOs.Requests;
using MISA.CRM.CORE.Exceptions;
using MISA.CRM.CORE.Interfaces.Services;

namespace MISA.CRM.API.Controllers
{
    /// <summary>
    /// Base Controller (Generic) cung cấp các API cơ bản cho một thực thể (Entity)
    /// <para/>Các Controller cụ thể (như CustomersController) sẽ kế thừa BaseController để sử dụng lại các API CRUD, Paging...
    /// </summary>
    /// <typeparam name="T">Loại Entity (Class) mà Controller này quản lý</typeparam>
    /// Created by TMHieu - 7/12/2025
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BaseController<T> : ControllerBase where T : class
    {
        #region Declaration

        /// <summary>
        /// Service xử lý nghiệp vụ cơ sở cho Entity T
        /// </summary>
        protected readonly IBaseService<T> _service;

        #endregion Declaration

        #region Constructor

        /// <summary>
        /// Hàm khởi tạo Base Controller
        /// </summary>
        /// <param name="service">Service xử lý nghiệp vụ được tiêm vào (Dependency Injection)</param>
        /// Created by TMHieu - 7/12/2025
        public BaseController(IBaseService<T> service)
        {
            _service = service;
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Lấy tất cả bản ghi của Entity T
        /// </summary>
        /// <returns>Danh sách tất cả các Entity T</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpGet]
        public virtual async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data });
        }

        /// <summary>
        /// Lấy một bản ghi theo ID
        /// </summary>
        /// <param name="id">ID (Guid) của bản ghi cần lấy</param>
        /// <returns>Bản ghi Entity T tương ứng</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);
                return Ok(new { data });
            }
            catch (NotFoundException ex)
            {
                // Xử lý lỗi không tìm thấy (404)
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Thêm mới một bản ghi Entity T
        /// </summary>
        /// <param name="entity">Đối tượng Entity T cần thêm mới</param>
        /// <returns>ID (Guid) của bản ghi vừa được tạo</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] T entity)
        {
            var id = await _service.CreateAsync(entity);
            return StatusCode(201, new { id, message = "Created successfully" }); // 201 Created
        }

        /// <summary>
        /// Cập nhật một bản ghi Entity T theo ID
        /// </summary>
        /// <param name="id">ID (Guid) của bản ghi cần cập nhật</param>
        /// <param name="entity">Đối tượng Entity T chứa dữ liệu mới</param>
        /// <returns>Số bản ghi đã được cập nhật</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(Guid id, [FromBody] T entity)
        {
            try
            {
                var rows = await _service.UpdateAsync(id, entity);
                return Ok(new { updated = rows, message = "Updated successfully" });
            }
            catch (NotFoundException ex)
            {
                // Xử lý lỗi không tìm thấy (404)
                return NotFound(new { error = ex.Message });
            }
            catch (ValidateException ex)
            {
                // Xử lý lỗi nghiệp vụ/validate (400)
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa một bản ghi theo ID
        /// </summary>
        /// <param name="id">ID (Guid) của bản ghi cần xóa</param>
        /// <returns>Số bản ghi đã được xóa</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var rows = await _service.DeleteAsync(id);
                return Ok(new { deleted = rows, message = "Deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                // Xử lý lỗi không tìm thấy (404)
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra tính trùng lặp của một giá trị trong một trường dữ liệu (unique check)
        /// </summary>
        /// <param name="req">DTO chứa Tên trường, Giá trị cần kiểm tra và ID cần bỏ qua (nếu có)</param>
        /// <returns>Đối tượng chứa cờ `isDuplicate` (true/false)</returns>
        /// <exception cref="ValidateException">Ném ra ngoại lệ nếu dữ liệu request không hợp lệ</exception>
        /// Created by TMHieu - 7/12/2025
        [HttpPost("check-duplicate")]
        public virtual async Task<IActionResult> CheckDuplicate([FromBody] DuplicateCheckRequest req)
        {
            if (req == null)
                throw new ValidateException("Dữ liệu gửi lên không hợp lệ");

            // Xử lý và phân tích IgnoreId (chấp nhận null hoặc chuỗi không hợp lệ)
            Guid? ignoreGuid = null;
            if (!string.IsNullOrWhiteSpace(req.IgnoreId)
                && Guid.TryParse(req.IgnoreId, out Guid parsed))
            {
                ignoreGuid = parsed;
            }

            // Validate các trường bắt buộc
            if (string.IsNullOrWhiteSpace(req.PropertyName) ||
                string.IsNullOrWhiteSpace(req.Value))
            {
                throw new ValidateException("Thiếu trường cần thiết để kiểm tra trùng");
            }

            bool exists = await _service.IsValueExistAsync(req.PropertyName, req.Value, ignoreGuid);

            return Ok(new { isDuplicate = exists });
        }

        /// <summary>
        /// Lấy dữ liệu phân trang, hỗ trợ tìm kiếm và sắp xếp
        /// </summary>
        /// <param name="page">Số trang hiện tại (mặc định là 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định là 100)</param>
        /// <param name="search">Chuỗi tìm kiếm</param>
        /// <param name="sortBy">Tên trường muốn sắp xếp</param>
        /// <param name="sortOrder">Thứ tự sắp xếp (asc/desc)</param>
        /// <param name="type">Tham số lọc theo loại (nếu có)</param>
        /// <returns>Đối tượng PagingResponse chứa danh sách dữ liệu và thông tin meta</returns>
        /// Created by TMHieu - 7/12/2025
        [HttpGet("paging")]
        public async Task<PagingResponse<T>> GetPaging([FromQuery] int page = 1,
                                                        [FromQuery] int pageSize = 100,
                                                        [FromQuery] string? search = null,
                                                        [FromQuery] string? sortBy = null,
                                                        [FromQuery] string? sortOrder = null,
                                                        [FromQuery] string? type = null
        )
        {
            var response = await _service.QueryPagingAsync(page, pageSize, search, sortBy, sortOrder, type);
            return response;
        }

        #endregion Method
    }
}