using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using MISA.CRM.CORE.DTOs.Map;
using MISA.CRM.CORE.Entities;
using MISA.CRM.CORE.Exceptions;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.CORE.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.Services
{
    /// <summary>
    /// Service xử lý các nghiệp vụ và logic liên quan đến đối tượng Khách hàng
    /// <para/>Kế thừa từ BaseServices và triển khai interface ICustomerService
    /// </summary>
    /// Created by TMHieu - 7/12/2025
    public class CustomerService : BaseServices<Customer>, ICustomerService
    {
        #region Declaration

        private readonly ICustomerRepository _customerRepo;

        #endregion Declaration

        #region Constructor

        /// <summary>
        /// Hàm khởi tạo Service Khách hàng
        /// </summary>
        /// <param name="repo">Repository xử lý Khách hàng được tiêm vào (Dependency Injection)</param>
        /// Created by TMHieu - 7/12/2025
        public CustomerService(ICustomerRepository repo) : base(repo)
        {
            _customerRepo = repo;
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Tạo và trả về mã khách hàng mới tự động tăng theo quy tắc: KH + YYYYMM + 6 chữ số tự tăng
        /// <para/>Ví dụ: KH202512000001
        /// </summary>
        /// <returns>Mã khách hàng mới</returns>
        /// Created by TMHieu - 7/12/2025
        public async Task<string> NextCodeAsync()
        {
            // Lấy tiền tố là năm và tháng hiện tại (Ví dụ: 202512)
            string prefix = DateTime.Now.ToString("yyyyMM");

            // Lấy mã code khách hàng lớn nhất hiện có trong DB
            string? last = await _customerRepo.GetLastCodeAsync();

            int next = 1;

            if (!string.IsNullOrEmpty(last))
            {
                // Lấy 6 ký tự cuối cùng của mã code cũ (phần số tự tăng)
                string numStr = last[^6..];

                // Tăng giá trị lên 1
                next = int.Parse(numStr) + 1;
            }

            // Trả về mã code mới: KH + Tiền tố (YYYYMM) + Số tự tăng (D6 đảm bảo có 6 chữ số, có padding 0)
            return $"KH{prefix}{next:D6}";
        }

        /// <summary>
        /// Hàm map mặc định, dùng ClassMap của CsvHelper
        /// Con có thể override nếu header CSV khác
        /// Created By: TMHieu (07/12/2025)
        /// </summary>
        protected virtual ClassMap<Customer>? GetCsvClassMap()
        {
            return null; // default = null → CsvHelper tự map theo property
        }

        /// <summary>
        /// Import dữ liệu từ Stream CSV và lưu vào DB
        /// </summary>
        /// <param name="fileStream">Stream của file CSV</param>
        /// <returns>Số bản ghi insert thành công</returns>
        public async Task<int> ImportFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File rỗng.");

            var records = new List<Customer>();

            // Dùng EPPlus đọc Excel
            using var package = new OfficeOpenXml.ExcelPackage(file.OpenReadStream());
            var worksheet = package.Workbook.Worksheets.First(); // lấy sheet đầu tiên
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            if (rowCount < 2)
                return 0; // không có dữ liệu

            // Lặp qua từng dòng, bỏ qua header (dòng 1)
            for (int row = 2; row <= rowCount; row++)
            {
                var record = new Customer
                {
                    CrmCustomerType = colCount >= 1 ? worksheet.Cells[row, 1].Text : null,
                    CrmCustomerCode = colCount >= 2 ? worksheet.Cells[row, 2].Text : null,
                    CrmCustomerName = colCount >= 3 ? worksheet.Cells[row, 3].Text : null,
                    CrmCustomerTaxCode = colCount >= 4 ? worksheet.Cells[row, 4].Text : null,
                    CrmCustomerShippingAddress = colCount >= 5 ? worksheet.Cells[row, 5].Text : null,
                    CrmCustomerPhoneNumber = colCount >= 6 ? worksheet.Cells[row, 6].Text : null,
                    CrmCustomerLastPurchaseDate = colCount >= 7 && DateTime.TryParse(worksheet.Cells[row, 7].Text, out var dt) ? dt : (DateTime?)null,
                    CrmCustomerPurchasedItemCode = colCount >= 8 ? worksheet.Cells[row, 8].Text : null,
                    CrmCustomerPurchasedItemName = colCount >= 9 ? worksheet.Cells[row, 9].Text : null,
                    CrmCustomerEmail = colCount >= 10 ? worksheet.Cells[row, 10].Text : null,
                    CrmCustomerAddress = colCount >= 11 ? worksheet.Cells[row, 11].Text : null
                };

                records.Add(record);
            }

            if (!records.Any())
                return 0;

            // --- Bắt đầu gán Guid tự động ---
            var guidProperty = typeof(Customer).GetProperties()
                                    .FirstOrDefault(p => p.PropertyType == typeof(Guid));

            if (guidProperty != null)
            {
                foreach (var record in records)
                {
                    var currentValue = (Guid)guidProperty.GetValue(record);
                    if (currentValue == Guid.Empty) // chỉ gán nếu chưa có giá trị
                    {
                        guidProperty.SetValue(record, Guid.NewGuid());
                    }
                }
            }
            else
            {
                throw new Exception("Entity không có property CrmCustomerId kiểu Guid để cấp tự động.");
            }
            // --- Kết thúc gán Guid ---

            var insertedRows = 0;
            var errRows = 0;
            foreach (var customer in records)
            {
                try
                {
                    // --- Sinh CrmCustomerCode tự động ---
                    customer.CrmCustomerCode = await NextCodeAsync();
                    await CreateAsync(customer);
                    Console.WriteLine("Inserted " + customer.CrmCustomerName);
                    insertedRows++;
                }
                catch (ValidateException ex)
                {
                    errRows++;
                }
            }

            return insertedRows;
        }

        /// <summary>
        /// Thực hiện validate các thông tin bắt buộc và các quy tắc nghiệp vụ của đối tượng Khách hàng
        /// </summary>
        /// <param name="customer">Đối tượng Khách hàng cần validate</param>
        /// <param name="ignoreId">ID của bản ghi (entity) cần bỏ qua khi kiểm tra trùng lặp (dùng trong trường hợp sửa)</param>
        /// <returns>Task</returns>
        /// <exception cref="ValidateException">Ném ra ngoại lệ ValidateException nếu có lỗi</exception>
        /// Created by TMHieu - 7/12/2025
        protected override async Task ValidateAsync(Customer customer, Guid? ignoreId = null)
        {
            if (customer == null)
                throw new ValidateException("Customer object is null", "Dữ liệu khách hàng không được để trống");

            // 1. Full name: Kiểm tra bắt buộc nhập
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerName))
                throw new ValidateException("FullName is required", "Tên khách hàng không được để trống");

            // 2. Phone validate: Kiểm tra bắt buộc nhập, chỉ chứa số, độ dài 10-11
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerPhoneNumber))
                throw new ValidateException("Phone required", "Số điện thoại không được để trống");

            if (!customer.CrmCustomerPhoneNumber.All(char.IsDigit))
                throw new ValidateException("Phone invalid", "Số điện thoại chỉ được chứa chữ số");

            if (customer.CrmCustomerPhoneNumber.Length < 10 || customer.CrmCustomerPhoneNumber.Length > 11)
                throw new ValidateException("Phone length invalid", "Số điện thoại phải dài 10-11 số");

            // 3. Email validate: Kiểm tra bắt buộc nhập, đúng định dạng Regex
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerEmail))
                throw new ValidateException("Email required", "Email không được để trống");

            var emailRegex = @"^[\w\.\-]+@([\w\-]+\.)+[\w\-]{2,4}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(customer.CrmCustomerEmail, emailRegex))
                throw new ValidateException("Invalid email format", "Email không đúng định dạng");

            // 4. Check unique: Kiểm tra số điện thoại và email đã tồn tại trong hệ thống chưa (trừ chính bản ghi đang sửa)
            if (await _customerRepo.IsValueExistAsync("CrmCustomerPhoneNumber", customer.CrmCustomerPhoneNumber, ignoreId))
                throw new ValidateException("Phone duplicate", "Số điện thoại đã tồn tại");

            if (await _customerRepo.IsValueExistAsync("CrmCustomerEmail", customer.CrmCustomerEmail, ignoreId))
                throw new ValidateException("Email duplicate", "Email đã tồn tại");
        }

        #endregion Method
    }
}