using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.Infrastructure.Connection
{
    /// <summary>
    /// Factory giúp khởi tạo đối tượng kết nối đến Database MySQL
    /// <para/>Sử dụng trong tầng Infrastructure để cung cấp kết nối cho các Repository thực thi câu lệnh SQL
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class MySqlConnectionFactory
    {
        #region Declaration

        /// <summary>
        /// Chuỗi thông tin kết nối đến Database
        /// </summary>
        private readonly string _connectionString;

        #endregion Declaration

        #region Constructor

        /// <summary>
        /// Hàm khởi tạo đối tượng Factory với chuỗi kết nối cụ thể
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến database (Connection String)</param>
        /// Created by TMHieu - 7/12/2025
        public MySqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Khởi tạo và trả về một đối tượng kết nối Database
        /// <para/>Sử dụng khi cần mở một phiên kết nối mới để thao tác dữ liệu
        /// </summary>
        /// <returns>Đối tượng kết nối (IDbConnection) đã được khởi tạo</returns>
        /// Created by TMHieu - 7/12/2025
        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        #endregion Method
    }
}