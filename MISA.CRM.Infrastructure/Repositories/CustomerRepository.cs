using Dapper;
using MISA.CRM.CORE.Entities;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.Infrastructure.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.Infrastructure.Repositories
{
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(MySqlConnectionFactory factory)
        : base(factory)
        {
        }

        /// <summary>
        /// Lấy mã lớn nhất hiện tại trong database
        /// Created by: TMHieu (07/12/2025)
        /// </summary>
        /// <returns>Mã cuối trong db</returns>
        public async Task<string?> GetLastCodeAsync()
        {
            using var connection = Connection;

            string sql = @" SELECT crm_customer_code
                            FROM crm_customer
                            ORDER BY crm_customer_code DESC
                            LIMIT 1;";

            return await connection.QueryFirstOrDefaultAsync<string>(sql);
        }

        protected override HashSet<string> GetSortableFields()
        {
            return new HashSet<string>
            {
                //Nếu chinh sửa ở đây thì chỉ sort được các cột được thêm.
                //"crm_customer_code",
                //"crm_customer_name",
                //"crm_customer_email",
                //"crm_customer_phone_number",
                //"crm_customer_date_of_birth"
            };
        }

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
    }
}