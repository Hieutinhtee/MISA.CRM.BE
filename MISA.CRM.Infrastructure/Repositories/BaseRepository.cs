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

        protected readonly string _softKeyDelete;

        /// <summary>
        /// Constructor cho BaseRepository.
        /// Khởi tạo factory kết nối và xác định tên bảng/entity.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="factory">Factory tạo MySqlConnection (bắt buộc, không được null)</param>
        /// <param name="idColumn">Tên cột khóa chính nếu không phải Id (ví dụ: CustomerId). Mặc định là null để dùng Id.</param>
        protected BaseRepository(MySqlConnectionFactory factory)
        {
            // Kiểm tra factory không null để tránh lỗi runtime
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            // Ưu tiên lấy tên bảng từ attribute [TableName("...")]
            var tableAttr = typeof(T).GetCustomAttribute<TableNameAttribute>();
            _tableName = tableAttr != null ? tableAttr.Name : "crm_" + ToSnakeCase(typeof(T).Name);

            RegisterTypeMap<T>();

            _softKeyDelete = "crm_" + ToSnakeCase(typeof(T).Name) + "_is_deleted";
            //Lấy tên Id từ property có attribute [Key]
            var keyProp = typeof(T)
                        .GetProperties()
                        .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
            {
                _idColumn = "crm_" + ToSnakeCase(typeof(T).Name) + "_id";
            }
            else
            {
                //Lấy ColumnName từ attribute nếu có
                var colAttr = keyProp.GetCustomAttribute<ColumnAttribute>();

                _idColumn = colAttr != null ? colAttr.Name : keyProp.Name;
            }
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

        public static void RegisterTypeMap<T>()
        {
            SqlMapper.SetTypeMap(
                typeof(T),
                new CustomPropertyTypeMap(
                    typeof(T),
                    (type, columnName) =>
                    {
                        return type.GetProperties()
                            .FirstOrDefault(prop =>
                            {
                                var attr = prop.GetCustomAttributes(typeof(ColumnAttribute), true)
                                              .OfType<ColumnAttribute>()
                                              .FirstOrDefault();
                                return attr != null && attr.Name == columnName;
                            });
                    })
            );
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
        /// Lấy tất cả entity.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <returns>Danh sách entity T.</returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            // Sử dụng using để tự động đóng connection sau khi dùng xong
            using var conn = Connection;
            // SQL chỉ lấy các record chưa xóa (is_deleted = 0)
            var sql = $"SELECT * FROM {_tableName} WHERE {_softKeyDelete} = 0";
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

            // Lấy tên cột thực trong DB qua ColumnAttribute
            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p =>
                    p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (prop == null)
                throw new Exception($"Property '{columnName}' không tồn tại trên entity.");

            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var dbColumn = colAttr != null ? colAttr.Name : columnName;

            var sql = $@" UPDATE {_tableName}
                          SET {dbColumn} = @Value
                          WHERE {_idColumn} IN @Ids
                          AND {_softKeyDelete} = 0;";

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
        /// Lấy tất cả entity với sort .
        /// Sử dụng whitelist từ GetSortableFields để tránh SQL injection.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="sortField">Tên cột sort (phải hợp lệ).</param>
        /// <param name="asc">True nếu ASC, false nếu DESC. Mặc định true.</param>
        /// <returns>Danh sách entity đã sort.</returns>
        public virtual async Task<List<T>> GetAllSortedAsync(string sortField, bool asc = true)
        {
            // Lấy whitelist sortable fields
            var valid = GetSortableFields();
            // Kiểm tra sortField hợp lệ
            if (string.IsNullOrWhiteSpace(sortField) || !valid.Contains(sortField))
                throw new ArgumentException("Không được để trống cột cần sắp xếp.");
            // Xây dựng ORDER BY
            var order = asc ? "ASC" : "DESC";
            using var conn = Connection;
            // SQL với ORDER BY và chỉ lấy chưa xóa
            var sql = $"SELECT * FROM {_tableName} WHERE {_softKeyDelete} = 0 ORDER BY {sortField} {order}";
            var res = await conn.QueryAsync<T>(sql);
            return res.ToList();
        }

        /// <summary>
        /// Lấy entity với paging và sort .
        /// Trả về tuple (Data, Total) theo quy ước API.
        /// Created by: TMHieu (05/12/2025)
        /// </summary>
        /// <param name="page">Số trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Kích thước trang (mặc định 20).</param>
        /// <param name="sortField">Tên cột sort (tùy chọn, phải hợp lệ).</param>
        /// <param name="asc">True nếu ASC, false nếu DESC. Mặc định true.</param>
        /// <returns>Tuple với List<T> và tổng số record.</returns>
        public virtual async Task<(List<T> Data, int Total)> GetPagingAsync(int page, int pageSize, string? sortField = null, bool asc = true)
        {
            // Đảm bảo page và pageSize hợp lệ
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            // Tính offset cho LIMIT/OFFSET
            var offset = (page - 1) * pageSize;
            // Khởi tạo order clause
            string orderClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(sortField))
            {
                // Kiểm tra sortField hợp lệ từ whitelist
                var valid = GetSortableFields();
                if (!valid.Contains(sortField))
                    throw new ArgumentException("Invalid sort field.");
                orderClause = $"ORDER BY {sortField} {(asc ? "ASC" : "DESC")}";
            }
            using var conn = Connection;
            // SQL lấy data với paging và chỉ lấy chưa xóa
            var sqlData = $"SELECT * FROM {_tableName} WHERE {_softKeyDelete} = 0 {orderClause} LIMIT @Limit OFFSET @Offset";
            // SQL đếm tổng chỉ tính chưa xóa
            var sqlCount = $"SELECT COUNT(1) FROM {_tableName} WHERE {_softKeyDelete} = 0";
            // Chạy parallel để tối ưu
            var taskData = conn.QueryAsync<T>(sqlData, new { Limit = pageSize, Offset = offset });
            var taskCount = conn.ExecuteScalarAsync<int>(sqlCount);
            await Task.WhenAll(taskData, taskCount);
            // Lấy kết quả
            var data = (await taskData).ToList();
            var total = await taskCount;
            return (data, total);
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
        /// <param name="columnName">Tên cột cần kiểm tra.</param>
        /// <param name="value">Giá trị cần kiểm tra.</param>
        /// <param name="ignoreId">ID cần bỏ qua (khi update để tránh trùng chính nó).</param>
        /// <returns>True nếu tồn tại, False nếu không.</returns>
        public async Task<bool> IsValueExistAsync(string propertyName, object value, Guid? ignoreId = null)
        {
            using var conn = Connection;
            // Lấy thông tin property từ T
            var prop = typeof(T).GetProperty(propertyName);
            if (prop == null)
                throw new Exception($"Property '{propertyName}' không tồn tại trong {typeof(T).Name}");

            // Lấy ColumnName từ attribute
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr == null)
                throw new Exception($"Property '{propertyName}' không có ColumnAttribute");

            var columnName = columnAttr.Name; // Tên cột trong DB
            var sql = $@"SELECT COUNT(1)
                         FROM {_tableName}
                         WHERE {columnName} = @Value
                         AND {_softKeyDelete} = 0
                         AND (@IgnoreId IS NULL OR {_idColumn} <> @IgnoreId);";

            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                Value = value,
                IgnoreId = ignoreId
            });

            return count > 0;
        }
    }
}