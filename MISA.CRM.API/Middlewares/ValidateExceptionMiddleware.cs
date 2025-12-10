using MISA.CRM.CORE.Exceptions;
using System.Collections;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Middleware toàn cục có nhiệm vụ bắt (catch) các ngoại lệ (Exception) xảy ra trong luồng xử lý Request
/// <para/>Ngoại lệ sẽ được chuyển đổi thành cấu trúc phản hồi JSON chuẩn hóa và trả về cho Client
/// </summary>
/// Created by TMHieu - 7/12/2025
public class ValidateExceptionMiddleware
{
    #region Declaration

    private readonly RequestDelegate _next;

    #endregion Declaration

    #region Constructor

    /// <summary>
    /// Hàm khởi tạo Middleware
    /// </summary>
    /// <param name="next">Delegate trỏ đến Middleware tiếp theo trong pipeline</param>
    /// Created by TMHieu - 7/12/2025
    public ValidateExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion Constructor

    #region Method

    /// <summary>
    /// Thực thi Middleware: Bao bọc việc gọi Middleware tiếp theo trong khối try-catch để xử lý ngoại lệ
    /// </summary>
    /// <param name="context">Ngữ cảnh HTTP hiện tại</param>
    /// <returns>Task</returns>
    /// Created by TMHieu - 7/12/2025
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidateException ex)
        {
            // Xử lý lỗi nghiệp vụ/validate: Trả về 400 Bad Request
            await HandleExceptionAsync(context, 400, "Dữ liệu không hợp lệ", ex);
        }
        catch (NotFoundException ex)
        {
            // Xử lý lỗi không tìm thấy: Trả về 404 Not Found
            await HandleExceptionAsync(context, 404, "Không tìm thấy dữ liệu", ex);
        }
        catch (DuplicateException ex)
        {
            // Xử lý lỗi trùng lặp: Trả về 409 Conflict
            await HandleExceptionAsync(context, 409, "Dữ liệu bị trùng", ex);
        }
        catch (Exception ex)
        {
            // Xử lý tất cả các lỗi còn lại: Trả về 500 Internal Server Error
            await HandleExceptionAsync(context, 500, "Có lỗi xảy ra, vui lòng thử lại sau ít phút", ex);
        }
    }

    /// <summary>
    /// Hàm xử lý và chuẩn hóa ngoại lệ thành phản hồi JSON
    /// </summary>
    /// <param name="context">Ngữ cảnh HTTP</param>
    /// <param name="statusCode">Mã trạng thái HTTP</param>
    /// <param name="defaultUserMsg">Thông báo mặc định cho người dùng (nếu Exception không cung cấp UserMsg)</param>
    /// <param name="ex">Đối tượng ngoại lệ</param>
    /// <returns>Task</returns>
    /// Created by TMHieu - 7/12/2025
    private async Task HandleExceptionAsync(HttpContext context, int statusCode, string defaultUserMsg, Exception ex)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var baseEx = ex as BaseException;

        // Lấy frame phù hợp từ stacktrace - cố gắng tìm frame đầu tiên có thông tin file/line
        var st = new StackTrace(ex, true);
        StackFrame? targetFrame = null;
        for (int i = 0; i < st.FrameCount; i++)
        {
            var f = st.GetFrame(i);
            if (f != null && !string.IsNullOrEmpty(f.GetFileName()))
            {
                targetFrame = f;
                break;
            }
        }
        // Fallback: Lấy frame 0 nếu không tìm thấy frame nào có thông tin file
        targetFrame ??= st.GetFrame(0);

        string? file = targetFrame?.GetFileName();
        int? line = targetFrame?.GetFileLineNumber();

        // Lấy inner exceptions đệ quy
        List<object> GetInnerExceptions(Exception e)
        {
            var list = new List<object>();
            var inner = e.InnerException;
            while (inner != null)
            {
                list.Add(new
                {
                    message = inner.Message,
                    type = inner.GetType().FullName,
                    stack = inner.StackTrace
                });
                inner = inner.InnerException;
            }
            return list;
        }

        // Lấy ex.Data (nếu có) và chuyển đổi sang Dictionary để serialize
        IDictionary? dataDict = null;
        if (ex.Data != null && ex.Data.Count > 0)
        {
            dataDict = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in ex.Data)
            {
                try
                {
                    ((Dictionary<string, object?>)dataDict)[entry.Key?.ToString() ?? ""] = entry.Value;
                }
                catch
                {
                    // Bỏ qua các key không phải chuỗi
                }
            }
        }

        // Nếu BaseException có trường chứa chi tiết lỗi (ví dụ Errors hoặc Details), cố gắng lấy ra bằng Reflection
        object? validationErrors = null;
        if (baseEx != null)
        {
            // Các tên thuộc tính thường gặp cần kiểm tra
            var candidateNames = new[] { "Errors", "Details", "ValidationErrors", "MoreInfo" };
            var beType = baseEx.GetType();
            foreach (var name in candidateNames)
            {
                var prop = beType.GetProperty(name);
                if (prop != null)
                {
                    var val = prop.GetValue(baseEx);
                    if (val != null)
                    {
                        validationErrors = val;
                        break;
                    }
                }
            }
        }

        // Tạo đối tượng chứa thông tin chi tiết lỗi cho nhà phát triển (Dev Message)
        var devMsg = new
        {
            message = ex.Message,
            type = ex.GetType().FullName,
            file = file ?? "unknown",
            line = line,
            stack = ex.StackTrace?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries),
            inner = GetInnerExceptions(ex),
            data = dataDict,
            validationErrors = validationErrors,
            moreInfo = (baseEx?.MoreInfo)
        };

        // Tạo cấu trúc phản hồi cuối cùng theo chuẩn PagingResponse (có trường error)
        var response = new
        {
            data = (object?)null,
            meta = (object?)null,
            error = new
            {
                // Ưu tiên UserMsg trong Exception, nếu không có thì dùng defaultUserMsg
                userMsg = baseEx?.UserMsg ?? defaultUserMsg,
                devMsg,
                traceId = context.TraceIdentifier
            }
        };

        // Cấu hình JsonSerializer để format JSON (CamelCase, WriteIndented)
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        // Ghi phản hồi JSON vào HttpContext
        await context.Response.WriteAsJsonAsync(response, options);
    }

    #endregion Method
}