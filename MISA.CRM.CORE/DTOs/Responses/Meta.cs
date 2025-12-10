namespace MISA.CRM.Core.DTOs.Responses
{
    /// <summary>
    /// Đối tượng lưu trữ thông tin Meta Data của việc phân trang
    /// <para/>Sử dụng trong đối tượng PagingResponse để mô tả tổng số bản ghi, số trang hiện tại...
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class Meta
    {
        #region Property

        /// <summary>
        /// Trang hiện tại đang hiển thị
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Kích thước trang (Số lượng bản ghi trên một trang)
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Tổng số bản ghi (Total records) tìm được
        /// </summary>
        public int Total { get; set; }

        #endregion Property
    }
}