using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MISA.CRM.CORE.Attributes;

namespace MISA.CRM.CORE.Entities
{
    /// <summary>
    /// Đối tượng khách hàng
    /// Created by: TMHieu 03/12/2025
    /// </summary>
    [TableName("crm_customer")]
    public class Customer
    {
        /// <summary>
        /// ID khách hàng (UUID)
        /// </summary>
        [Key]
        [Column("crm_customer_id")]
        public Guid CrmCustomerId { get; set; }

        /// <summary>
        /// Loại khách hàng
        /// </summary>

        [MaxLength(20)]
        [Column("crm_customer_type")]
        public string CrmCustomerType { get; set; }

        /// <summary>
        /// Mã khách hàng
        /// </summary>

        [MaxLength(20)]
        [Column("crm_customer_code")]
        public string CrmCustomerCode { get; set; }

        /// <summary>
        /// Tên đầy đủ khách hàng
        /// </summary>

        [MaxLength(500)]
        [Column("crm_customer_name")]
        public string CrmCustomerName { get; set; }

        /// <summary>
        /// Số điện thoại
        /// </summary>

        [MaxLength(50)]
        [Column("crm_customer_phone_number")]
        public string CrmCustomerPhoneNumber { get; set; }

        /// <summary>
        /// Email
        /// </summary>

        [MaxLength(100)]
        [Column("crm_customer_email")]
        public string CrmCustomerEmail { get; set; }

        /// <summary>
        /// Địa chỉ liên hệ chính
        /// </summary>

        [MaxLength(255)]
        [Column("crm_customer_address")]
        public string CrmCustomerAddress { get; set; }

        /// <summary>
        /// Địa chỉ giao hàng
        /// </summary>

        [MaxLength(255)]
        [Column("crm_customer_shipping_address")]
        public string CrmCustomerShippingAddress { get; set; }

        /// <summary>
        /// Mã số thuế
        /// </summary>

        [MaxLength(20)]
        [Column("crm_customer_tax_code")]
        public string CrmCustomerTaxCode { get; set; }

        /// <summary>
        /// Ngày mua gần nhất
        /// </summary>
        [Column("crm_customer_last_purchase_date")]
        public DateTime? CrmCustomerLastPurchaseDate { get; set; }

        /// <summary>
        /// Mã hàng hóa mua gần nhất
        /// </summary>
        [MaxLength(20)]
        [Column("crm_customer_purchased_item_code")]
        public string CrmCustomerPurchasedItemCode { get; set; }

        /// <summary>
        /// Tên hàng hóa mua gần nhất
        /// </summary>
        [MaxLength(100)]
        [Column("crm_customer_purchased_item_name")]
        public string CrmCustomerPurchasedItemName { get; set; }

        /// <summary>
        /// Đường dẫn ảnh đại diện
        /// </summary>
        [MaxLength(255)]
        [Column("crm_customer_image")]
        public string CrmCustomerImage { get; set; }

        /// <summary>
        /// Trạng thái xóa mềm
        /// </summary>

        [Column("crm_customer_is_deleted")]
        public bool CrmCustomerIsDeleted { get; set; } = false;
    }
}