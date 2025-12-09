using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MISA.CRM.Core.DTOs.Responses;

namespace MISA.CRM.CORE.Interfaces.Repositories
{
    /// <summary>
    /// Base repo interface cho CRUD cơ bản
    /// </summary>
    /// Created By: TMHieu (03/12/2025)
    /// <typeparam name="T">Thực thể cần truyền vào VD: Customer</typeparam>
    public interface IBaseRepository<T> where T : class
    {
        /// <summary>
        /// Hàm lấy danh sách tất cả dữ liệu
        /// </summary>
        /// <returns>trả về danh sách tất cả dữ liệu</returns>
        /// Created By: TMHieu (03/12/2025)
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Hàm lấy thông tin chi tiết theo id
        /// </summary>
        /// <param name="id"> id của đối tượng muốn tìm </param>
        /// <returns>đối tượng có id phù hợp</returns>
        /// Created By: TMHieu (03/12/2025)
        Task<T?> GetById(Guid id);

        /// <summary>
        /// Hàm thêm mới bản ghi trong database
        /// </summary>
        /// <param name="entity">thuộc tính của thực thể muốn thêm</param>
        /// <returns>trả về Id của bản ghi mới</returns>
        /// Created By: TMHieu (03/12/2025)
        Task<Guid> InsertAsync(T entity);

        /// <summary>
        /// Hàm cập nhật 1 bản ghi theo id
        /// </summary>
        /// <param name="id"> id của bản ghi mình muốn cập nhật</param>
        /// <param name="entity">thuộc tính của thực thể</param>
        /// <returns>Guid của bản ghi vừa cập nhật</returns>
        /// Created By: TMHieu (03/12/2025)
        Task<Guid> UpdateAsync(Guid id, T entity);

        /// <summary>
        /// Hàm cập nhật 1 cột thành các giá trị giống nhau của nhiều bản ghi theo id
        /// </summary>
        /// <param name="ids"> danh sách id của bản ghi muốn cập nhật</param>
        /// <param name="columnName">Cột cần cập nhật hàng loạt</param>
        /// <param name="value">Giá trị mới cần cập nhật</param>
        /// <returns>Số bản ghi bị ảnh hưởng</returns>
        /// Created By: TMHieu (05/12/2025)
        Task<int> BulkUpdateSameValueAsync(List<Guid> ids, string columnName, object value);

        /// <summary>
        /// Xóa mềm 1 bản ghi trong database
        /// </summary>
        /// <param name="id">id mà mình muốn xóa </param>
        /// <returns>số dòng bị ảnh hưởng </returns>
        /// Created By: TMHieu (03/12/2025)
        Task<int> DeleteAsync(Guid id);

        /// <summary>
        /// Kiểm tra giá trị có tồn tại trong cột (bỏ qua soft delete và ignoreId nếu có).
        /// </summary>
        /// <param name="columnName">Tên cột cần kiểm tra.</param>
        /// <param name="value">Giá trị cần kiểm tra.</param>
        /// <param name="ignoreId">ID cần bỏ qua (tùy chọn).</param>
        /// <returns>True nếu tồn tại, False nếu không.</returns>
        /// Created By: TMHieu (05/12/2025)
        Task<bool> IsValueExistAsync(string columnName, object value, Guid? ignoreId = null);

        /// <summary>
        /// Lấy danh sách entity có phân trang, tìm kiếm và sắp xếp
        /// </summary>
        /// <param name="page">Trang thứ mấy</param>
        /// <param name="pageSize">Số bản ghi một trang.</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="sortBy">Cột cần sắp xếp</param>
        /// <param name="sortOrder">Hướng sắp xếp (ASC/DESC)</param>
        /// <returns>Đối tượng PagingResponse chứa dữ liệu và metadata</returns>
        /// Created By: TMHieu (07/12/2025)
        Task<PagingResponse<T>> QueryPagingAsync(int page, int pageSize, string? search, string? sortBy, string? sortOrder, string? type = null);

        /// <summary>
        /// Bulk insert dùng 1 query thực hiện cho toàn bộ danh sách
        /// </summary>
        /// <param name="entities">Danh sách entity cần insert</param>
        /// Created By: TMHieu (12/12/2025)
        /// <returns>Số bản ghi insert thành công</returns>
        Task<int> BulkInsertAsync(IEnumerable<T> entities);
    }
}