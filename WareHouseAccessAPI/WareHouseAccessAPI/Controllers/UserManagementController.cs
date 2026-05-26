using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAccessAPI.Common;
using WarehouseAccessAPI.Dtos;
using WarehouseAccessAPI.Models;

namespace WarehouseAccessAPI.Controllers;

[ApiController]
[Route("WarehouseAccess/[controller]/[action]")]
public class UserManagementController : ControllerBase
{
    private const string DefaultUserTypeId = "01";
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;
    private const long MaxImportFileSizeBytes = 5 * 1024 * 1024;
    private readonly WarehouseAccessAPI.Data.WarehouseAccessDbContext _db;

    public UserManagementController(WarehouseAccessAPI.Data.WarehouseAccessDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult ExportUsersTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("UsersTemplate");
        worksheet.Cell(1, 1).Value = "UserCode";
        worksheet.Cell(1, 2).Value = "CardNumber";
        worksheet.Cell(1, 3).Value = "FullName";
        worksheet.Cell(1, 4).Value = "DeptCode";
        worksheet.Columns(1, 4).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"users-template-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<DepartmentLookupDto>>>> GetDepartments()
    {
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode) && !string.IsNullOrWhiteSpace(x.DeptName))
            .GroupBy(x => new { x.DeptCode, x.DeptName })
            .Select(x => new DepartmentLookupDto
            {
                DeptCode = x.Key.DeptCode!,
                DeptName = x.Key.DeptName!
            })
            .OrderBy(x => x.DeptName)
            .ToListAsync();

        return Ok(new Response<List<DepartmentLookupDto>>(true, departments, "Success"));
    }

    [HttpGet]
    public async Task<ActionResult<Response<PagedResult<UserListItemDto>>>> GetUsers(
        [FromQuery] string? keyword,
        [FromQuery] string? deptCode,
        [FromQuery] int page = DefaultPage,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var normalizedPage = page <= 0 ? DefaultPage : page;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var normalizedDeptCode = string.IsNullOrWhiteSpace(deptCode) ? null : deptCode.Trim();

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(x =>
                x.UserCode.Contains(normalizedKeyword) ||
                (x.FullName != null && x.FullName.Contains(normalizedKeyword))
            );
        }

        if (!string.IsNullOrWhiteSpace(normalizedDeptCode))
        {
            query = query.Where(x => x.DeptCode == normalizedDeptCode);
        }

        var deptLookup = await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode) && !string.IsNullOrWhiteSpace(x.DeptName))
            .GroupBy(x => x.DeptCode)
            .Select(g => new { DeptCode = g.Key!, DeptName = g.Select(x => x.DeptName).FirstOrDefault()! })
            .ToDictionaryAsync(x => x.DeptCode, x => x.DeptName);

        var total = await query.CountAsync();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        if (totalPages > 0 && normalizedPage > totalPages)
        {
            normalizedPage = totalPages;
        }

        var skip = (normalizedPage - 1) * normalizedPageSize;

        var users = await query
            .OrderBy(x => x.UserCode)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new UserListItemDto
            {
                UserCode = x.UserCode,
                CardNumber = x.CardNumber,
                FullName = x.FullName,
                DeptCode = x.DeptCode,
                
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.DeptCode) && deptLookup.TryGetValue(user.DeptCode, out var deptName))
            {
                user.DeptName = deptName;
            }
        }

        var result = new PagedResult<UserListItemDto>
        {
            Items = users,
            Page = totalPages == 0 ? DefaultPage : normalizedPage,
            PageSize = normalizedPageSize,
            Total = total,
            TotalPages = totalPages
        };

        return Ok(new Response<PagedResult<UserListItemDto>>(true, result, "Success"));
    }

    [HttpPost]
    public async Task<ActionResult<Response<UserListItemDto>>> CreateUser([FromBody] CreateUserRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "Request body is required"));
        }

        var normalizedUserCode = NormalizeRequired(request.UserCode);
        var normalizedCardNumber = NormalizeOptional(request.CardNumber);
        var normalizedFullName = NormalizeRequired(request.FullName);
        var normalizedDeptCode = NormalizeRequired(request.DeptCode);
        

        if (normalizedUserCode is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "UserCode is required"));
        }

        if (normalizedFullName is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "FullName is required"));
        }

        if (normalizedDeptCode is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "DeptCode is required"));
        }

        var existing = await _db.Users.AsNoTracking().AnyAsync(x => x.UserCode == normalizedUserCode);
        if (existing)
        {
            return Conflict(new Response<UserListItemDto>(false, null, "UserCode already exists"));
        }

        var now = DateTime.Now;
        var user = new User
        {
            UserCode = normalizedUserCode,
            CardNumber = normalizedCardNumber,
            FullName = normalizedFullName,
            DeptCode = normalizedDeptCode,
            UserTypeId = DefaultUserTypeId,
            
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = await ToUserListItemAsync(user);
        return Ok(new Response<UserListItemDto>(true, dto, "Success"));
    }

    [HttpPut]
    public async Task<ActionResult<Response<UserListItemDto>>> UpdateUser([FromBody] UpdateUserRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "Request body is required"));
        }

        var normalizedUserCode = NormalizeRequired(request.UserCode);
        var normalizedCardNumber = NormalizeOptional(request.CardNumber);
        var normalizedFullName = NormalizeRequired(request.FullName);
        var normalizedDeptCode = NormalizeRequired(request.DeptCode);
        

        if (normalizedUserCode is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "UserCode is required"));
        }

        if (normalizedFullName is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "FullName is required"));
        }

        if (normalizedDeptCode is null)
        {
            return BadRequest(new Response<UserListItemDto>(false, null, "DeptCode is required"));
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserCode == normalizedUserCode);
        if (user is null)
        {
            return NotFound(new Response<UserListItemDto>(false, null, "User not found"));
        }

        user.FullName = normalizedFullName;
        user.DeptCode = normalizedDeptCode;
        user.CardNumber = normalizedCardNumber;
        
        user.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        var dto = await ToUserListItemAsync(user);
        return Ok(new Response<UserListItemDto>(true, dto, "Success"));
    }

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> DeleteUser([FromQuery] string? userCode)
    {
        var normalizedUserCode = NormalizeRequired(userCode);
        if (normalizedUserCode is null)
        {
            return BadRequest(new Response<bool>(false, false, "userCode is required"));
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserCode == normalizedUserCode);
        if (user is null)
        {
            return NotFound(new Response<bool>(false, false, "User not found"));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new Response<bool>(true, true, "Success"));
    }

    [HttpPost]
    [RequestSizeLimit(MaxImportFileSizeBytes)]
    public async Task<ActionResult<Response<ImportUsersResultDto>>> ImportUsers([FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "Excel file is required"));
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "Only .xlsx file is supported"));
        }

        if (file.Length > MaxImportFileSizeBytes)
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "File size exceeds 5MB limit"));
        }

        var result = new ImportUsersResultDto();

        await using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "Worksheet not found"));
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "File is empty"));
        }

        var headerRow = usedRange.FirstRow();
        var headers = headerRow.Cells().Select((cell, index) => new
        {
            Name = (cell.GetString() ?? string.Empty).Trim(),
            Index = index + 1
        }).ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

        var hasDeptCode = headers.TryGetValue("DeptCode", out var deptCodeCol);
        var hasDeptName = headers.TryGetValue("DeptName", out var deptNameCol);

        var hasCardNumber = headers.TryGetValue("CardNumber", out var cardNumberCol);

        if (!headers.TryGetValue("UserCode", out var userCodeCol) || !headers.TryGetValue("FullName", out var fullNameCol) || (!hasDeptCode && !hasDeptName))
        {
            return BadRequest(new Response<ImportUsersResultDto>(false, null, "Required headers: UserCode, FullName, DeptCode (or DeptName). Optional: CardNumber"));
        }

        var departmentMap = await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode) && !string.IsNullOrWhiteSpace(x.DeptName))
            .GroupBy(x => x.DeptName)
            .Select(x => new { DeptName = x.Key!, DeptCode = x.Select(d => d.DeptCode).FirstOrDefault()! })
            .ToDictionaryAsync(x => x.DeptName, x => x.DeptCode, StringComparer.OrdinalIgnoreCase);

        var existingUserCodesList = await _db.Users.AsNoTracking().Select(x => x.UserCode).ToListAsync();
        var existingUserCodes = new HashSet<string>(existingUserCodesList, StringComparer.OrdinalIgnoreCase);
        var batchUserCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var dataRows = usedRange.RowsUsed().Skip(1).ToList();
        result.TotalRows = dataRows.Count;

        foreach (var row in dataRows)
        {
            var rowNumber = row.RowNumber();
            var userCode = NormalizeRequired(row.Cell(userCodeCol).GetString());
            var cardNumber = hasCardNumber ? NormalizeOptional(row.Cell(cardNumberCol).GetString()) : null;
            var fullName = NormalizeRequired(row.Cell(fullNameCol).GetString());
            string? deptCode = null;

            if (hasDeptCode)
            {
                deptCode = NormalizeRequired(row.Cell(deptCodeCol).GetString());
            }
            else if (hasDeptName)
            {
                var deptName = NormalizeRequired(row.Cell(deptNameCol).GetString());
                if (deptName is not null && departmentMap.TryGetValue(deptName, out var mappedCode))
                {
                    deptCode = mappedCode;
                }
            }

            if (userCode is null)
            {
                result.Errors.Add(new ImportUsersErrorDto { RowNumber = rowNumber, UserCode = string.Empty, Message = "UserCode is required" });
                continue;
            }

            if (fullName is null)
            {
                result.Errors.Add(new ImportUsersErrorDto { RowNumber = rowNumber, UserCode = userCode, Message = "FullName is required" });
                continue;
            }

            if (deptCode is null)
            {
                result.Errors.Add(new ImportUsersErrorDto { RowNumber = rowNumber, UserCode = userCode, Message = "DeptCode is required or DeptName is invalid" });
                continue;
            }

            if (existingUserCodes.Contains(userCode) || batchUserCodes.Contains(userCode))
            {
                result.Errors.Add(new ImportUsersErrorDto { RowNumber = rowNumber, UserCode = userCode, Message = "UserCode already exists" });
                continue;
            }

            var now = DateTime.Now;
            _db.Users.Add(new User
            {
                UserCode = userCode,
                CardNumber = cardNumber,
                FullName = fullName,
                DeptCode = deptCode,
                UserTypeId = DefaultUserTypeId,
                CreatedAt = now,
                UpdatedAt = now
            });
            batchUserCodes.Add(userCode);
            result.InsertedCount++;
        }

        await _db.SaveChangesAsync();

        result.SkippedCount = result.TotalRows - result.InsertedCount;
        return Ok(new Response<ImportUsersResultDto>(true, result, "Success"));
    }

    private async Task<UserListItemDto> ToUserListItemAsync(User user)
    {
        string? deptName = null;
        if (!string.IsNullOrWhiteSpace(user.DeptCode))
        {
            deptName = await _db.Departments
                .AsNoTracking()
                .Where(x => x.DeptCode == user.DeptCode)
                .Select(x => x.DeptName)
                .FirstOrDefaultAsync();
        }

        return new UserListItemDto
        {
            UserCode = user.UserCode,
            CardNumber = user.CardNumber,
            FullName = user.FullName,
            DeptCode = user.DeptCode,
            DeptName = deptName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static string? NormalizeRequired(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

}
