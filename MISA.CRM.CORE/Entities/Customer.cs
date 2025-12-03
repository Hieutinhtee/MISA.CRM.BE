using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.Entities
{
    /// <summary>
    /// Đối tượng khách hàng
    /// Created by: TMHieu  03/12/2025
    /// </summary>
    [Table("crm_customer")] // ← Map đúng bảng DB có sẵn
    public class Customer
    {
        /// <summary>
        /// ID khách hàng (UUID)
        /// </summary>
        [Key]
        [Column("crm_customer_id")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Loại khách hàng: NBH01, LKHA, VIP
        /// </summary>
        [Required]
        [Column("crm_customer_type")]
        [MaxLength(20)]
        public string CustomerType { get; set; }

        /// <summary>
        /// Mã khách hàng: KHyyyyMM + 6 số tăng mãi, không reset
        /// </summary>
        [Required]
        [Column("crm_customer_code")]
        [MaxLength(20)]
        public string CustomerCode { get; set; }

        /// <summary>
        /// Tên đầy đủ khách hàng (cá nhân hoặc công ty)
        /// </summary>
        [Required]
        [Column("crm_customer_name")]
        [MaxLength(500)]
        public string FullName { get; set; }

        /// <summary>
        /// Số điện thoại - duy nhất
        /// </summary>
        [Required]
        [Column("crm_customer_phone_number")]
        [MaxLength(50)]
        public string Phone { get; set; }

        /// <summary>
        /// Email - duy nhất
        /// </summary>
        [Required]
        [Column("crm_customer_email")]
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Địa chỉ liên hệ chính
        /// </summary>
        [Required]
        [Column("crm_customer_address")]
        [MaxLength(255)]
        public string Address { get; set; }

        /// <summary>
        /// Địa chỉ giao hàng
        /// </summary>
        [Required]
        [Column("crm_customer_shipping_address")]
        [MaxLength(255)]
        public string ShippingAddress { get; set; }

        /// <summary>
        /// Mã số thuế
        /// </summary>
        [Required]
        [Column("crm_customer_tax_code")]
        [MaxLength(20)]
        public string TaxCode { get; set; }

        /// <summary>
        /// Ngày mua hàng gần nhất
        /// </summary>
        [Column("crm_customer_last_purchase_date")]
        public DateTime? LastPurchaseDate { get; set; }

        /// <summary>
        /// Mã hàng hóa mua gần nhất (mock)
        /// </summary>
        [Column("crm_customer_purchased_item_code")]
        [MaxLength(20)]
        public string LastPurchasedItemCode { get; set; }

        /// <summary>
        /// Tên hàng hóa mua gần nhất
        /// </summary>
        [Column("crm_customer_purchased_item_name")]
        [MaxLength(100)]
        public string LastPurchasedItemName { get; set; }

        /// <summary>
        /// Đường dẫn ảnh đại diện
        /// </summary>
        [Column("crm_customer_image")]
        [MaxLength(255)]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Trạng thái xóa mềm
        /// </summary>
        [Required]
        [Column("crm_customer_is_deleted")]
        public bool IsDeleted { get; set; } = false;

        //sẽ thêm sau này
        //public DateTime CreatedDate { get; set; } = DateTime.Now;

        //public DateTime ModifiedDate { get; set; } = DateTime.Now;

        //public String CreatedBy { get; set; };

        //public String ModifiedBy { get; set; };
    }
}