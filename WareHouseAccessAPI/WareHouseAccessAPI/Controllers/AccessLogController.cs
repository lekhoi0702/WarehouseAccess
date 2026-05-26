using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using WarehouseAccessAPI.Common;
using WarehouseAccessAPI.Dtos;
using WarehouseAccessAPI.Models;

namespace WarehouseAccessAPI.Controllers;

[ApiController]
[Route("WarehouseAccess/[controller]/[action]")]
public class AccessLogController : ControllerBase
{
    private const string ActiveRecordStatus = "1";
    private readonly WarehouseAccessAPI.Data.WarehouseAccessDbContext _db;

    public AccessLogController(WarehouseAccessAPI.Data.WarehouseAccessDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<Response<LookupByCardResponseDto>>> LookupByCard([FromBody] LookupByCardRequestDto request)
    {
        var cardNumber = request?.CardNumber?.Trim();
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return BadRequest(new Response<LookupByCardResponseDto>(false, null, "CardNumber is required"));
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(x => x.CardNumber == cardNumber)
            .Select(x => new LookupByCardResponseDto
            {
                CardNumber = x.CardNumber,
                UserCode = x.UserCode,
                FullName = x.FullName,
                DeptCode = x.DeptCode,
                DeptName = _db.Departments
                    .Where(d => d.DeptCode == x.DeptCode)
                    .Select(d => d.DeptName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(new Response<LookupByCardResponseDto>(false, null, "Card not found"));
        }

        return Ok(new Response<LookupByCardResponseDto>(true, user, "Success"));
    }
        
    [HttpPost]
    public async Task<ActionResult<Response<AccessLogDetailDto>>> CreateCheckIn([FromBody] CreateAccessLogCheckInRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "Request body is required"));
        }

        var normalizedCardNumber = NormalizeString(request.CardNumber);
        if (string.IsNullOrWhiteSpace(normalizedCardNumber))
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "CardNumber is required"));
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CardNumber == normalizedCardNumber);

        if (user is null)
        {
            return NotFound(new Response<AccessLogDetailDto>(false, null, "Card not found"));
        }

        if (string.IsNullOrWhiteSpace(user.UserCode))
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "UserCode is missing for this card"));
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "FullName is missing for this card"));
        }

        var now = DateTime.Now;
        var newLogId = await GenerateNextLogIdAsync();

        var accessLog = new AccessLog
        {
            LogId = newLogId,
            EventTime = now,
            UserCode = user.UserCode,
            FullName = user.FullName,
            DeptCode = NormalizeString(user.DeptCode),
            ContactDept = NormalizeString(request.ContactDept),
            Purpose = NormalizeString(request.Purpose),
            Photo = NormalizeBase64Photo(request.Photo),
            RecordStatus = ActiveRecordStatus,
            CheckInTime = now,
            CheckOutTime = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AccessLogs.Add(accessLog);
        await _db.SaveChangesAsync();

        var result = new AccessLogDetailDto
        {
            LogId = accessLog.LogId,
            CardNumber = user.CardNumber,
            EventTime = accessLog.EventTime,
            DeptCode = accessLog.DeptCode,
            DeptName = await GetDepartmentNameAsync(accessLog.DeptCode),
            FullName = accessLog.FullName,
            UserCode = accessLog.UserCode,
            CheckInTime = accessLog.CheckInTime,
            CheckOutTime = accessLog.CheckOutTime,
            ContactDept = accessLog.ContactDept,
            Purpose = accessLog.Purpose,
            Photo = accessLog.Photo,
            CreatedAt = accessLog.CreatedAt,
            UpdatedAt = accessLog.UpdatedAt
        };

        return Ok(new Response<AccessLogDetailDto>(true, result, "Success"));
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<AccessLogDetailDto>>>> GetLiveMonitor(
        [FromQuery] string? keyword,
        [FromQuery] int take = 100)
    {
        var normalizedTake = take <= 0 ? 100 : Math.Min(take, 500);
        var normalizedKeyword = NormalizeString(keyword);

        var query = _db.AccessLogs
            .AsNoTracking()
            .Where(x => x.CheckOutTime == null);

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(x =>
                (x.UserCode != null && x.UserCode.Contains(normalizedKeyword)) ||
                (x.FullName != null && x.FullName.Contains(normalizedKeyword)) ||
                (x.DeptCode != null && x.DeptCode.Contains(normalizedKeyword)) ||
                (x.Purpose != null && x.Purpose.Contains(normalizedKeyword)));
        }

        var departmentLookup = await GetDepartmentLookupAsync();
        var cardLookup = await _db.Users
            .AsNoTracking()
            .Where(x => x.UserCode != null)
            .ToDictionaryAsync(x => x.UserCode, x => x.CardNumber);

        var items = await query
            .OrderByDescending(x => x.CheckInTime)
            .Take(normalizedTake)
            .Select(x => new AccessLogDetailDto
            {
                LogId = x.LogId,
                EventTime = x.EventTime,
                DeptCode = x.DeptCode,
                FullName = x.FullName,
                UserCode = x.UserCode,
                CheckInTime = x.CheckInTime,
                CheckOutTime = x.CheckOutTime,
                ContactDept = x.ContactDept,
                Purpose = x.Purpose,
                Photo = x.Photo,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.DeptCode) && departmentLookup.TryGetValue(item.DeptCode, out var deptName))
            {
                item.DeptName = deptName;
            }
            if (!string.IsNullOrWhiteSpace(item.UserCode) && cardLookup.TryGetValue(item.UserCode, out var cardNumber))
            {
                item.CardNumber = cardNumber;
            }
        }

        return Ok(new Response<List<AccessLogDetailDto>>(true, items, "Success"));
    }
    [HttpGet]
    public async Task<ActionResult<Response<List<AccessLogDetailDto>>>> GetHistory(
        [FromQuery] string? keyword,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int take = 200)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new Response<List<AccessLogDetailDto>>(false, null, "fromDate must be less than or equal to toDate"));
        }

        var normalizedTake = take <= 0 ? 200 : Math.Min(take, 1000);
        var normalizedKeyword = NormalizeString(keyword);

        var query = _db.AccessLogs.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CheckInTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var inclusiveToDate = toDate.Value;
            query = query.Where(x => x.CheckInTime <= inclusiveToDate);
        }

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(x =>
                (x.UserCode != null && x.UserCode.Contains(normalizedKeyword)) ||
                (x.FullName != null && x.FullName.Contains(normalizedKeyword)) ||
                (x.DeptCode != null && x.DeptCode.Contains(normalizedKeyword)) ||
                (x.Purpose != null && x.Purpose.Contains(normalizedKeyword)));
        }

        var departmentLookup = await GetDepartmentLookupAsync();
        var cardLookup = await _db.Users
            .AsNoTracking()
            .Where(x => x.UserCode != null)
            .ToDictionaryAsync(x => x.UserCode, x => x.CardNumber);

        var items = await query
            .OrderByDescending(x => x.CheckInTime)
            .Take(normalizedTake)
            .Select(x => new AccessLogDetailDto
            {
                LogId = x.LogId,
                EventTime = x.EventTime,
                DeptCode = x.DeptCode,
                FullName = x.FullName,
                UserCode = x.UserCode,
                CheckInTime = x.CheckInTime,
                CheckOutTime = x.CheckOutTime,
                ContactDept = x.ContactDept,
                Purpose = x.Purpose,
                Photo = x.Photo,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.DeptCode) && departmentLookup.TryGetValue(item.DeptCode, out var deptName))
            {
                item.DeptName = deptName;
            }
            if (!string.IsNullOrWhiteSpace(item.UserCode) && cardLookup.TryGetValue(item.UserCode, out var cardNumber))
            {
                item.CardNumber = cardNumber;
            }
        }

        return Ok(new Response<List<AccessLogDetailDto>>(true, items, "Success"));
    }

    [HttpPost]
    public async Task<ActionResult<Response<AccessLogDetailDto>>> ConfirmCheckOut([FromBody] ConfirmCheckOutRequestDto request)
    {
        if (request is null || request.LogId <= 0)
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "LogId is required"));
        }

        var accessLog = await _db.AccessLogs.FirstOrDefaultAsync(x => x.LogId == request.LogId);
        if (accessLog is null)
        {
            return NotFound(new Response<AccessLogDetailDto>(false, null, "AccessLog not found"));
        }

        if (accessLog.CheckOutTime != null)
        {
            return BadRequest(new Response<AccessLogDetailDto>(false, null, "This record is already checked out"));
        }
        var now = DateTime.Now;
        accessLog.CheckOutTime = now;
        if (!string.IsNullOrWhiteSpace(request.ExitPhoto))
        {
            accessLog.Photo = NormalizeBase64Photo(request.ExitPhoto);
        }
        accessLog.UpdatedAt = now;

        await _db.SaveChangesAsync();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserCode == accessLog.UserCode);
        var result = new AccessLogDetailDto
        {
            LogId = accessLog.LogId,
            CardNumber = user?.CardNumber,
            EventTime = accessLog.EventTime,
            DeptCode = accessLog.DeptCode,
            DeptName = await GetDepartmentNameAsync(accessLog.DeptCode),
            FullName = accessLog.FullName,
            UserCode = accessLog.UserCode,
            CheckInTime = accessLog.CheckInTime,
            CheckOutTime = accessLog.CheckOutTime,
            ContactDept = accessLog.ContactDept,
            Purpose = accessLog.Purpose,
            Photo = accessLog.Photo,
            CreatedAt = accessLog.CreatedAt,
            UpdatedAt = accessLog.UpdatedAt
        };

        return Ok(new Response<AccessLogDetailDto>(true, result, "Success"));
    }

    [HttpGet]
    public async Task<IActionResult> ExportHistoryExcel(
        [FromQuery] string? keyword,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new Response<string>(false, null, "fromDate must be less than or equal to toDate"));
        }

        var normalizedKeyword = NormalizeString(keyword);
        var query = _db.AccessLogs.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CheckInTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CheckInTime <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(x =>
                (x.UserCode != null && x.UserCode.Contains(normalizedKeyword)) ||
                (x.FullName != null && x.FullName.Contains(normalizedKeyword)) ||
                (x.DeptCode != null && x.DeptCode.Contains(normalizedKeyword)) ||
                (x.Purpose != null && x.Purpose.Contains(normalizedKeyword)));
        }

        var records = await query
            .OrderByDescending(x => x.CheckInTime)
            .Select(x => new
            {
                x.CheckInTime,
                x.DeptCode,
                x.FullName,
                x.UserCode,
                x.CheckOutTime,
                x.Purpose
            })
            .ToListAsync();

        var departmentLookup = await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode))
            .GroupBy(x => x.DeptCode!)
            .Select(g => new
            {
                DeptCode = g.Key,
                DeptName = g.Select(x => x.DeptName).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.DeptCode, x => x.DeptName ?? string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("AccessLogs");

        worksheet.Range("A1:I1").Merge();
        worksheet.Cell("A1").Value = "ĐĂNG KÝ RA VÀO KHO THÀNH PHẨM";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 24;
        worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(1).Height = 34;

        var headerRow = 3;
        worksheet.Cell(headerRow, 1).Value = "STT";
        worksheet.Cell(headerRow, 2).Value = "Ngày/tháng/năm";
        worksheet.Cell(headerRow, 3).Value = "Đơn vị/ Bộ phận";
        worksheet.Cell(headerRow, 4).Value = "Họ và tên";
        worksheet.Cell(headerRow, 5).Value = "MSNV";
        worksheet.Cell(headerRow, 6).Value = "Giờ vào";
        worksheet.Cell(headerRow, 7).Value = "Giờ ra";
        worksheet.Cell(headerRow, 8).Value = "Mục đích liên hệ";
        worksheet.Cell(headerRow, 9).Value = "Kiểm tra của quản lý";

        var currentRow = headerRow + 1;
        var stt = 1;
        foreach (var item in records)
        {
            worksheet.Cell(currentRow, 1).Value = stt++;
            worksheet.Cell(currentRow, 2).Value = item.CheckInTime?.ToString("dd/MM/yyyy") ?? string.Empty;
            var departmentLabel = !string.IsNullOrWhiteSpace(item.DeptCode) && departmentLookup.TryGetValue(item.DeptCode, out var deptName)
                ? (string.IsNullOrWhiteSpace(deptName) ? item.DeptCode : deptName)
                : (item.DeptCode ?? string.Empty);
            worksheet.Cell(currentRow, 3).Value = departmentLabel;
            worksheet.Cell(currentRow, 4).Value = item.FullName ?? string.Empty;
            worksheet.Cell(currentRow, 5).Value = item.UserCode ?? string.Empty;
            worksheet.Cell(currentRow, 6).Value = item.CheckInTime?.ToString("HH:mm:ss") ?? string.Empty;
            worksheet.Cell(currentRow, 7).Value = item.CheckOutTime?.ToString("HH:mm:ss") ?? string.Empty;
            worksheet.Cell(currentRow, 8).Value = item.Purpose ?? string.Empty;
            worksheet.Cell(currentRow, 9).Value = string.Empty;
            currentRow++;
        }

        var lastRow = Math.Max(headerRow + 1, currentRow - 1);
        var dataRange = worksheet.Range(headerRow, 1, lastRow, 9);
        dataRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Font.FontSize = 12;
        worksheet.Range(headerRow, 1, headerRow, 9).Style.Font.Bold = true;
        worksheet.Range(headerRow, 1, headerRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(headerRow, 1, lastRow, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 16;
        worksheet.Column(3).Width = 22;
        worksheet.Column(4).Width = 24;
        worksheet.Column(5).Width = 14;
        worksheet.Column(6).Width = 14;
        worksheet.Column(7).Width = 14;
        worksheet.Column(8).Width = 28;
        worksheet.Column(9).Width = 24;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"access-log-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task<long> GenerateNextLogIdAsync()
    {
        var maxId = await _db.AccessLogs.MaxAsync(x => (long?)x.LogId) ?? 0;
        return maxId + 1;
    }

    private static string? NormalizeString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    private async Task<Dictionary<string, string?>> GetDepartmentLookupAsync()
    {
        return await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode))
            .GroupBy(x => x.DeptCode!)
            .Select(g => new { DeptCode = g.Key, DeptName = g.Select(x => x.DeptName).FirstOrDefault() })
            .ToDictionaryAsync(x => x.DeptCode, x => x.DeptName);
    }

    private async Task<string?> GetDepartmentNameAsync(string? deptCode)
    {
        if (string.IsNullOrWhiteSpace(deptCode))
        {
            return null;
        }
        return await _db.Departments
            .AsNoTracking()
            .Where(x => x.DeptCode == deptCode)
            .Select(x => x.DeptName)
            .FirstOrDefaultAsync();
    }
    private static string? NormalizeBase64Photo(string? value)
    {
        var trimmed = NormalizeString(value);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var marker = "base64,";
        var markerIndex = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex >= 0)
        {
            return trimmed[(markerIndex + marker.Length)..];
        }

        return trimmed;
    }
}
