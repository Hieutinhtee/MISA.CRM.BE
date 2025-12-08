using Microsoft.AspNetCore.Mvc;
using MISA.CRM.Core.DTOs.Responses;
using MISA.CRM.CORE.DTOs.Requests;
using MISA.CRM.CORE.Exceptions;
using MISA.CRM.CORE.Interfaces.Services;

namespace MISA.CRM.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BaseController<T> : ControllerBase where T : class
    {
        protected readonly IBaseService<T> _service;

        public BaseController(IBaseService<T> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data });
        }

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
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] T entity)
        {
            var id = await _service.CreateAsync(entity);
            return StatusCode(201, new { id, message = "Created successfully" });
        }

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
                return NotFound(new { error = ex.Message });
            }
            catch (ValidateException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

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
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("check-duplicate")]
        public virtual async Task<IActionResult> CheckDuplicate([FromBody] T entity)
        {
            try
            {
                var id = await _service.CreateAsync(entity);
                return StatusCode(201, new { id, message = "Created successfully" });
            }
            catch (ValidateException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("paging")]
        public async Task<PagingResponse<T>> GetPaging([FromQuery] int page = 1,
                                                       [FromQuery] int pageSize = 100,
                                                       [FromQuery] string? search = null,
                                                       [FromQuery] string? sortBy = null,
                                                       [FromQuery] string? sortOrder = null
        )
        {
            var response = await _service.QueryPagingAsync(page, pageSize, search, sortBy, sortOrder);
            return response;
        }

        /// <summary>
        /// Cập nhật cùng 1 giá trị cho nhiều bản ghi
        /// </summary>
        /// <param name="request">Thông tin danh sách id, tên cột, giá trị mới</param>
        /// <returns>Số bản ghi đã được cập nhật</returns>
        [HttpPost("bulk-update")]
        public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest request)
        {
            if (request == null || request.Ids == null || request.Ids.Count == 0)
                return BadRequest(new { message = "Danh sách Id không được rỗng." });

            if (string.IsNullOrWhiteSpace(request.ColumnName))
                return BadRequest(new { message = "Tên cột không được rỗng." });

            try
            {
                int updatedCount = await _service.BulkUpdateSameValueAsync(request.Ids, request.ColumnName, request.Value);
                return Ok(new { updatedCount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}