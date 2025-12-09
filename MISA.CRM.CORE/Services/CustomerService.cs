using CsvHelper.Configuration;
using MISA.CRM.CORE.DTOs.Map;
using MISA.CRM.CORE.Entities;
using MISA.CRM.CORE.Exceptions;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.CORE.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISA.CRM.CORE.Services
{
    public class CustomerService : BaseServices<Customer>, ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;

        public CustomerService(ICustomerRepository repo) : base(repo)
        {
            _customerRepo = repo;
        }

        public async Task<string> NextCodeAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMM");

            string? last = await _customerRepo.GetLastCodeAsync();

            int next = 1;

            if (!string.IsNullOrEmpty(last))
            {
                string numStr = last[^6..];   // lấy 6 ký tự cuối
                next = int.Parse(numStr) + 1;
            }

            return $"KH{prefix}{next:D6}";
        }

        protected override async Task ValidateAsync(Customer customer, Guid? ignoreId = null)
        {
            if (customer == null)
                throw new ValidateException("Customer object is null", "Dữ liệu khách hàng không được để trống");

            // 1. Full name
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerName))
                throw new ValidateException("FullName is required", "Tên khách hàng không được để trống");

            // 2. Phone validate: chỉ số, 10-11 digits
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerPhoneNumber))
                throw new ValidateException("Phone required", "Số điện thoại không được để trống");

            if (!customer.CrmCustomerPhoneNumber.All(char.IsDigit))
                throw new ValidateException("Phone invalid", "Số điện thoại chỉ được chứa chữ số");

            if (customer.CrmCustomerPhoneNumber.Length < 10 || customer.CrmCustomerPhoneNumber.Length > 11)
                throw new ValidateException("Phone length invalid", "Số điện thoại phải dài 10-11 số");

            // 3. Email validate bằng regex
            if (string.IsNullOrWhiteSpace(customer.CrmCustomerEmail))
                throw new ValidateException("Email required", "Email không được để trống");

            var emailRegex = @"^[\w\.\-]+@([\w\-]+\.)+[\w\-]{2,4}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(customer.CrmCustomerEmail, emailRegex))
                throw new ValidateException("Invalid email format", "Email không đúng định dạng");

            // 4. Check unique - dùng repo
            if (await _repo.IsValueExistAsync("CrmCustomerPhoneNumber", customer.CrmCustomerPhoneNumber, ignoreId))
                throw new ValidateException("Phone duplicate", "Số điện thoại đã tồn tại");

            if (await _repo.IsValueExistAsync("CrmCustomerEmail", customer.CrmCustomerEmail, ignoreId))
                throw new ValidateException("Email duplicate", "Email đã tồn tại");
        }
    }
}