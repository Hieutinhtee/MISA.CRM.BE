using CsvHelper.Configuration;
using MISA.CRM.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.DTOs.Map
{
    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            //Map(c => c.CrmCustomerId).Name("Mã khách hàng (ID)"); Guid không cần import sẽ tự sinh trong Service
            Map(c => c.CrmCustomerType).Name("Loại khách hàng");
            Map(c => c.CrmCustomerCode).Name("Mã khách hàng");
            Map(c => c.CrmCustomerName).Name("Tên khách hàng");
            Map(c => c.CrmCustomerPhoneNumber).Name("Số điện thoại");
            Map(c => c.CrmCustomerEmail).Name("Email");
            Map(c => c.CrmCustomerTaxCode).Name("Mã số thuế");
            Map(c => c.CrmCustomerAddress).Name("Địa chỉ liên hệ");
            Map(c => c.CrmCustomerShippingAddress).Name("Địa chỉ (Giao hàng)");
            Map(c => c.CrmCustomerLastPurchaseDate).Name("Ngày mua hàng gần nhất");
            Map(c => c.CrmCustomerPurchasedItemCode).Name("Mã hàng hóa");
            Map(c => c.CrmCustomerPurchasedItemName).Name("Tên hàng hóa đã mua");
        }
    }
}