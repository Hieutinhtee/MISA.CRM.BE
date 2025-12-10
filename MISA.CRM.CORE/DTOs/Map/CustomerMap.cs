using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MISA.CRM.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Map
{
    /// <summary>
    /// Class dùng để cấu hình ánh xạ (mapping) giữa thuộc tính của Entity Customer và tiêu đề cột trong file CSV
    /// <para/>Sử dụng trong nghiệp vụ Import/Export dữ liệu Khách hàng với file CSV
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class CustomerMap : ClassMap<Customer>
    {
        #region Constructor

        /// <summary>
        /// Hàm khởi tạo, định nghĩa các quy tắc ánh xạ
        /// </summary>
        /// Created by TMHieu - 7/12/2025
        public CustomerMap()
        {
            //Map(c => c.CrmCustomerId).Name("Mã khách hàng (ID)"); // Guid không cần import sẽ tự sinh trong Service

            Map(c => c.CrmCustomerType).Index(0);
            Map(c => c.CrmCustomerCode).Index(1);
            Map(c => c.CrmCustomerName).Index(2);
            Map(c => c.CrmCustomerTaxCode).Index(3);
            Map(c => c.CrmCustomerShippingAddress).Index(4);
            Map(c => c.CrmCustomerPhoneNumber).Index(5);
            Map(c => c.CrmCustomerLastPurchaseDate).Index(6).TypeConverterOption.Format("d/M/yyyy");
            Map(c => c.CrmCustomerPurchasedItemCode).Index(7);
            Map(c => c.CrmCustomerPurchasedItemName).Index(8);
            Map(c => c.CrmCustomerEmail).Index(9);
            Map(c => c.CrmCustomerAddress).Index(10);
        }

        #endregion Constructor
    }
}