using MISA.CRM.Core.DTOs.Responses;

namespace MISA.CRM.Core.DTOs.Responses
{
    /// <summary>
    /// Đối tượng DTO (Data Transfer Object) dùng để đóng gói dữ liệu và thông tin phân trang (Paging Response)
    /// <para/>Sử dụng để trả về cho Client, bao gồm danh sách dữ liệu và thông tin về tổng số bản ghi, số trang...
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu (Entity) của danh sách bản ghi</typeparam>
    /// Created by TMHieu - 7/12/2025
    public class PagingResponse<T>
    {
        #region Property

        /// <summary>
        /// Danh sách các bản ghi/thực thể được phân trang
        /// </summary>
        public List<T>? Data { get; set; }

        /// <summary>
        /// Thông tin meta data của việc phân trang (tổng số bản ghi, số trang hiện tại...)
        /// </summary>
        public Meta Meta { get; set; } = new Meta();

        /// <summary>
        /// Trường dùng để trả về thông tin lỗi (nếu có)
        /// </summary>
        public string? Error { get; set; }

        #endregion Property
    }
}