using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.Infrastructure.Connection;
using MISA.CRM.CORE.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MISA.CRM.Core.DTOs.Responses;

namespace MISA.CRM.Infrastructure.Repositories
{
    /// <summary>
    /// Repository base cho tất cả các entity.
    /// dùng Dapper làm micro-ORM.
    /// Tự động lấy tên bảng từ [TableName] hoặc fallback sang quy tắc crm_ + snake_case.
    /// Created by: TMHieu (05/12/2025)
    /// </summary>
    /// <typeparam name="T">Entity kế thừa từ class, phải có ít nhất 1 property đánh dấu [Key] nếu muốn Insert trả về Id</typeparam>
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        // Connection factory (được inject qua DI container)
        protected readonly MySqlConnectionFactory _factory;

        // Tên bảng trong database (crm_customer, crm_customer_type, ...)
        protected readonly string _tableName;

        // Tên cột khóa chính, mặc định là Id
        protected readonly string _idColumn;

        //Tên cột đánh dấu xóa mềm
        protected readonly string _softKeyDelete;

        //Tên cột mã tự sinh dùng để sắp xếp mặc định
        protected readonly string _defaultSortFiled;

        /// <summary>
        /// Constructor cho BaseRepository.
        /// Khởi tạo factory kết nối và xác định tên bảng/entity.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="factory">Factory tạo MySqlConnection (bắt buộc, không được null)</param>

        protected BaseRepository(MySqlConnectionFactory factory)
        {
            // 1. Check factory trước (ưu tiên tránh lỗi runtime)
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);

            // 2. Lấy tên bảng qua attribute TableName
            var tableAttr = type.GetCustomAttribute<TableNameAttribute>();
            _tableName = tableAttr?.Name ?? ToSnakeCase(type.Name);

            // 3. Tạo soft delete key: "crm_customer_is_deleted"
            _softKeyDelete = _tableName + "_is_deleted";
            _defaultSortFiled = _tableName + "_code";
            // 4. Lấy property Key
            var keyProp = type.GetProperties()
                              .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
            {
                throw new Exception(
                    $"Entity {type.Name} phải có [Key] attribute trên 1 property.");
            }

            // 5. Lấy tên cột ID (cột DB)
            var colAttr = keyProp.GetCustomAttribute<ColumnAttribute>();

            // Nếu có attribute [Column("crm_customer_id")] → dùng tên cột
            // Nếu không có → dùng tên property
            _idColumn = colAttr?.Name ?? ToSnakeCase(keyProp.Name);
        }

        /// <summary>
        /// Phương thức helper để chuyển tên class sang snake_case (ví dụ: Customer → customer).
        /// Sử dụng để fallback khi không có [TableName].
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="s">Chuỗi tên class cần chuyển đổi.</param>
        /// <returns>Tên snake_case tương ứng.</returns>
        protected static string ToSnakeCase(string s)
        {
            // Khởi tạo kết quả rỗng
            var result = "";
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                // Thêm _ trước chữ cái hoa (trừ chữ đầu)
                if (i > 0 && char.IsUpper(c))
                {
                    result += "_" + char.ToLower(c);
                }
                else
                {
                    result += char.ToLower(c);
                }
            }
            return result;
        }

        /// <summary>
        /// Property để lấy connection từ factory.
        /// Mỗi lần gọi sẽ tạo mới connection để tránh reuse sai.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        protected IDbConnection Connection => _factory.CreateConnection();

        /// <summary>
        /// Phương thức abstract để lấy danh sách các trường hợp lệ cho sort (whitelist để tránh SQL injection).
        /// Phải override ở repository con.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <returns>HashSet các tên cột hợp lệ.</returns>
        protected abstract HashSet<string> GetSortableFields();

        /// <summary>
        /// Phương thức abstract để lấy danh sách các trường hợp lệ cho sort (whitelist để tránh SQL injection).
        /// Phải override ở repository con.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <returns>HashSet các tên cột hợp lệ.</returns>
        protected abstract HashSet<string> GetSearchFields();

        /// <summary>
        /// Lấy tất cả entity.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <returns>Danh sách entity T.</returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            // Sử dụng using để tự động đóng connection sau khi dùng xong
            using var conn = Connection;
            // SQL chỉ lấy các record chưa xóa (is_deleted = 0)
            var sql = $"SELECT * FROM {_tableName} WHERE {_softKeyDelete} = 0 ORDER BY RIGHT({_defaultSortFiled}, 6) * 1 DESC;";
            var res = await conn.QueryAsync<T>(sql);
            return res.ToList();
        }

        /// <summary>
        /// Lấy entity theo Id .
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="id">Id của entity (Guid).</param>
        /// <returns>Entity T nếu tồn tại, null nếu không.</returns>
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            using var conn = Connection;
            // SQL lấy record đầu tiên khớp Id và chưa xóa mềm
            var sql = $"SELECT * FROM {_tableName} WHERE {_idColumn} = @Id AND {_softKeyDelete} = 0 LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        /// <summary>
        /// Insert entity mới .
        /// Sử dụng attribute [ColumnName] để mapping cột.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="entity">Entity cần insert.</param>
        /// <returns>Id của entity mới (Guid, từ [Key] property).</returns>
        public virtual async Task<Guid> InsertAsync(T entity)
        {
            using var conn = Connection;
            // Lấy danh sách property có attribute [ColumnName] để mapping
            var properties = typeof(T)
                .GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
                .ToList();
            // Tạo chuỗi columns từ attribute Name
            var columns = string.Join(", ", properties.Select(p => p.GetCustomAttribute<ColumnAttribute>()!.Name));
            // Tạo chuỗi param names (@PropertyName)
            var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));
            // Xây dựng SQL INSERT
            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({paramNames});";

            // Tạo DynamicParameters từ entity properties
            var parameters = new DynamicParameters();
            foreach (var prop in properties)
            {
                parameters.Add("@" + prop.Name, prop.GetValue(entity));
            }
            // Thực thi INSERT
            await conn.ExecuteAsync(sql, parameters);
            // Lấy Id từ property có [Key]
            var keyProp = typeof(T).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            // Trả về Id nếu có Guid.Empty
            return keyProp != null ? (Guid)keyProp.GetValue(entity)! : Guid.Empty;
        }

        /// <summary>
        /// Update entity theo Id .
        /// nếu bảng nào không cho Update thì override lại method này.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="id">Id của entity cần update.</param>
        /// <param name="entity">Entity với dữ liệu mới.</param>
        /// <returns>Số record bị ảnh hưởng.</returns>
        public virtual async Task<Guid> UpdateAsync(Guid id, T entity)
        {
            using var conn = Connection;

            // Lấy property key
            var keyProp = typeof(T).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
                return Guid.Empty;

            // Lấy danh sách property có [ColumnName] nhưng bỏ cột key
            var properties = typeof(T)
                .GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<ColumnAttribute>() != null
                    && p != keyProp)
                .ToList();

            // Tạo chuỗi "Col1 = @Prop1, Col2 = @Prop2"
            var setClause = string.Join(", ",
                properties.Select(p =>
                    $"{p.GetCustomAttribute<ColumnAttribute>()!.Name} = @{p.Name}"
                ));

            // Lấy tên cột khóa chính trong DB
            var keyColumnName = keyProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? keyProp.Name;

            var sql = $"UPDATE {_tableName} SET {setClause} WHERE {keyColumnName} = @Id;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);

            foreach (var prop in properties)
            {
                parameters.Add("@" + prop.Name, prop.GetValue(entity));
            }

            await conn.ExecuteAsync(sql, parameters);

            return id;
        }

        /// <summary>
        /// Hàm cập nhật 1 cột thành các giá trị giống nhau của nhiều bản ghi theo id
        /// </summary>
        /// <param name="ids"> danh sách id của bản ghi mình muốn cập nhật</param>
        /// <param name="columnName">Cột cần cập nhật hàng loạt</param>
        /// param name="value">Giá trị mới cần cập nhật</param>
        /// <returns>Guid của dòng vừa thêm</returns>
        /// Created By: TMHieu (05/12/2025)
        public async Task<int> BulkUpdateSameValueAsync(List<Guid> ids, string columnName, object value)
        {
            using var conn = Connection;

            // Tìm property có ColumnAttribute khớp với columnName
            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p =>
                    p.GetCustomAttribute<ColumnAttribute>()?.Name
                    .Equals(columnName, StringComparison.OrdinalIgnoreCase) == true);

            if (prop == null)
                throw new Exception($"Column '{columnName}' không tồn tại trong entity {typeof(T).Name}.");

            // Lấy tên cột thật trong DB
            var dbColumnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name
                               ?? ToSnakeCase(prop.Name);

            var idColumn = _idColumn; // đã set trong constructor

            // SQL UPDATE
            var sql =
                $"UPDATE {_tableName} SET {dbColumnName} = @Value WHERE {idColumn} IN @Ids";

            var parameters = new DynamicParameters();
            parameters.Add("@Value", value);
            parameters.Add("@Ids", ids);

            return await conn.ExecuteAsync(sql, parameters);
        }

        /// <summary>
        /// Xóa mềm entity theo Id (async, set is_deleted = 1).
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="id">Id của entity cần xóa mềm.</param>
        /// <returns>Số record bị ảnh hưởng (thường là 1 hoặc 0).</returns>
        public virtual async Task<int> DeleteAsync(Guid id)
        {
            using var conn = Connection;
            // SQL UPDATE để xóa mềm (set is_deleted = 1)
            var sql = $"UPDATE {_tableName} SET {_softKeyDelete} = 1 WHERE {_idColumn} = @Id AND {_softKeyDelete} = 0";
            return await conn.ExecuteAsync(sql, new { Id = id });
        }

        /// <summary>
        /// Lấy entity theo Id (sync, chưa implement).
        /// Nên dùng GetByIdAsync thay thế.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="id">Id của entity.</param>
        /// <returns>Entity T nếu tồn tại.</returns>
        public async Task<T?> GetById(Guid id)
        {
            // Sử dụng using để tự động đóng connection sau khi dùng xong
            using var conn = Connection;

            var sql = $@" SELECT * FROM {_tableName} WHERE {_idColumn} = @Id AND {_softKeyDelete} = 0;";

            var entity = await conn.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
            return entity;
        }

        /// <summary>
        /// Kiểm tra giá trị có tồn tại trong cột (bỏ qua soft delete và ignoreId nếu có).
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="propertyOrColumnName">Truyền vào tên cột hàm sẽ tự map với tên cột</param>
        /// <param name="value">Giá trị cần kiểm tra.</param>
        /// <param name="ignoreId">ID cần bỏ qua (khi update để tránh trùng chính nó).</param>
        /// <returns>True nếu tồn tại, False nếu không.</returns>
        public async Task<bool> IsValueExistAsync(string propertyOrColumnName, object value, Guid? ignoreId = null)
        {
            using var conn = Connection;

            // 1) Tìm property theo ColumnAttribute.Name
            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p =>
                    string.Equals(p.GetCustomAttribute<ColumnAttribute>()?.Name, propertyOrColumnName, StringComparison.OrdinalIgnoreCase));

            // 2) Nếu không tìm thấy, thử tìm theo tên property C#
            if (prop == null)
            {
                prop = typeof(T).GetProperty(propertyOrColumnName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }

            // 3) Nếu vẫn null => báo lỗi rõ ràng
            if (prop == null)
                throw new Exception($"Không tìm thấy property hoặc column '{propertyOrColumnName}' trên entity {typeof(T).Name}.");

            // 4) Lấy tên cột thực trong DB:
            //    - nếu có [Column("...")] thì dùng attribute
            //    - nếu không có thì fallback: ToSnakeCase(property.Name)
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var columnName = colAttr?.Name ?? ToSnakeCase(prop.Name);

            // 5) Xây SQL, dùng param để tránh injection
            var sql = $@" SELECT COUNT(1)
                          FROM {_tableName}
                          WHERE {columnName} = @Value
                          AND {_softKeyDelete} = 0
                          AND (@IgnoreId IS NULL OR {_idColumn} <> @IgnoreId);";

            // 6) Thực thi và trả về kết quả
            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                Value = value,
                IgnoreId = ignoreId
            });

            return count > 0;
        }

        /// <summary>
        /// Hàm trả về danh sách có phân trang, tìm kiếm và sắp xếp
        /// Created by: TMHieu (07/12/2025)
        /// </summary>
        /// <param name="page">Trang thứ mấy</param>
        /// <param name="pageSize">Số bản ghi một trang.</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="sortBy">cột cần sắp xếp</param>
        /// <param name="sortOrder">hướng sắp xếp (ASC/DESC)</param>
        /// <returns>Đối tượng PagingResponse chứa dữ liệu và metadata</returns>
        public virtual async Task<PagingResponse<T>> QueryPagingAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortOrder
        )
        {
            using var conn = Connection;

            // 1. Xác định field được sort
            var sortableFields = GetSortableFields(); // HashSet<string> do repo con override, có thể rỗng
            var searchFields = GetSearchFields();

            string orderClause = "";

            // Nếu FE truyền sortField
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                // Nếu repo con không override hoặc whitelist rỗng → cho phép tất cả cột
                if (sortableFields == null || sortableFields.Count == 0 || sortableFields.Contains(sortBy.ToLower()))
                {
                    string direction = sortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";
                    orderClause = $" ORDER BY {sortBy} {direction} ";
                }
                else
                {
                    // sortField không hợp lệ theo whitelist → fallback sort theo khóa chính
                    orderClause = $" ORDER BY {_idColumn} DESC ";
                }
            }
            else
            {
                // FE không truyền sortField → default sort theo khóa chính
                orderClause = $" ORDER BY RIGHT({_defaultSortFiled}, 6) * 1 DESC";
            }

            // 3. Tạo điều kiện WHERE cơ bản (is_deleted = 0)
            var where = $" WHERE {_softKeyDelete} = 0 ";

            // 4. Thêm search nếu có
            if (!string.IsNullOrWhiteSpace(search) && searchFields.Any())
            {
                var likeParts = searchFields
                    .Select(f => $"{f} LIKE @SearchStr");

                // (name LIKE '%a%' OR code LIKE '%a%' OR phone LIKE '%a%')
                where += " AND (" + string.Join(" OR ", likeParts) + ") ";
            }

            // 5. Query tổng dòng
            string sqlCount = $"SELECT COUNT(*) FROM {_tableName} {where};";

            // 6. Query paging
            string sqlData = $@" SELECT * FROM {_tableName}
                                 {where}
                                 {orderClause}
                                 LIMIT @Offset, @PageSize;";

            var param = new DynamicParameters();
            param.Add("@SearchStr", $"%{search}%");
            param.Add("@Offset", (page - 1) * pageSize);
            param.Add("@PageSize", pageSize);

            // 7. Thực hiện query song song
            var total = await conn.ExecuteScalarAsync<int>(sqlCount, param);
            var data = (await conn.QueryAsync<T>(sqlData, param)).ToList();

            // 8. Trả về đúng format FE yêu cầu
            return new PagingResponse<T>
            {
                Data = data,
                Meta = new Meta
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total
                },
                Error = null
            };
        }
    }
}