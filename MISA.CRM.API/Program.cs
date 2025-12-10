using MISA.CRM.API.Controllers;
using MISA.CRM.CORE.Interfaces.Repositories;
using MISA.CRM.CORE.Interfaces.Services;
using MISA.CRM.CORE.Services;
using MISA.CRM.Infrastructure.Connection;
using MISA.CRM.Infrastructure.Repositories;
using MySqlConnector;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Đăng ký MySQL connection factory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<MySqlConnectionFactory>(
    new MySqlConnectionFactory(connectionString)
);

// Đăng ký Repository

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Đăng ký Service
builder.Services.AddScoped<ICustomerService, CustomerService>();

//Cho phép gọi api không cần ktra
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

//set license code cho EPPlus
ExcelPackage.License.SetNonCommercialPersonal("thaiminhhieu");
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseMiddleware<ValidateExceptionMiddleware>();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();