using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAccessAPI.Common;
using WarehouseAccessAPI.Dtos;
using WarehouseAccessAPI.Models;

namespace WarehouseAccessAPI.Controllers;

[ApiController]
[Route("WarehouseAccess/[controller]/[action]")]
public class MasterDataController : ControllerBase
{
    private const string ActiveRecordStatus = "1";
    private readonly WarehouseAccessAPI.Data.WarehouseAccessDbContext _db;

    public MasterDataController(WarehouseAccessAPI.Data.WarehouseAccessDbContext db)
    {
        _db = db;
    }
    [HttpGet]
    public async Task<ActionResult<Response<List<DepartmentItemDto>>>> GetDepartments()
    {
        var items = await _db.Departments
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.DeptCode))
            .GroupBy(x => new { x.DeptCode, x.DeptName, x.RecordStatus })
            .Select(g => new DepartmentItemDto
            {
                DeptCode = g.Key.DeptCode!,
                DeptName = g.Key.DeptName ?? string.Empty,
                RecordStatus = g.Key.RecordStatus
            })
            .OrderBy(x => x.DeptCode)
            .ToListAsync();

        return Ok(new Response<List<DepartmentItemDto>>(true, items, "Success"));
    }

    [HttpPost]
    public async Task<ActionResult<Response<DepartmentItemDto>>> CreateDepartment([FromBody] DepartmentUpsertRequestDto request)
    {
        var deptCode = NormalizeRequired(request?.DeptCode);
        var deptName = NormalizeRequired(request?.DeptName);
        if (deptCode is null || deptName is null)
        {
            return BadRequest(new Response<DepartmentItemDto>(false, null, "DeptCode and DeptName are required"));
        }

        var existed = await _db.Departments.AsNoTracking().AnyAsync(x => x.DeptCode == deptCode);
        if (existed)
        {
            return Conflict(new Response<DepartmentItemDto>(false, null, "DeptCode already exists"));
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Department (DeptCode, DeptName, RecordStatus, CreatedAt, UpdatedAt)
VALUES ({deptCode}, {deptName}, {ActiveRecordStatus}, {DateTime.Now}, {DateTime.Now})");

        return Ok(new Response<DepartmentItemDto>(true, new DepartmentItemDto
        {
            DeptCode = deptCode,
            DeptName = deptName,
            RecordStatus = ActiveRecordStatus
        }, "Success"));
    }

    [HttpPut]
    public async Task<ActionResult<Response<DepartmentItemDto>>> UpdateDepartment([FromBody] DepartmentUpsertRequestDto request)
    {
        var deptCode = NormalizeRequired(request?.DeptCode);
        var deptName = NormalizeRequired(request?.DeptName);
        if (deptCode is null || deptName is null)
        {
            return BadRequest(new Response<DepartmentItemDto>(false, null, "DeptCode and DeptName are required"));
        }

        var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Department
SET DeptName = {deptName}, UpdatedAt = {DateTime.Now}
WHERE DeptCode = {deptCode}");

        if (affectedRows == 0)
        {
            return NotFound(new Response<DepartmentItemDto>(false, null, "Department not found"));
        }

        return Ok(new Response<DepartmentItemDto>(true, new DepartmentItemDto
        {
            DeptCode = deptCode,
            DeptName = deptName,
            RecordStatus = ActiveRecordStatus
        }, "Success"));
    }

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> DeleteDepartment([FromQuery] string? deptCode)
    {
        var normalizedCode = NormalizeRequired(deptCode);
        if (normalizedCode is null)
        {
            return BadRequest(new Response<bool>(false, false, "deptCode is required"));
        }

        var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
DELETE FROM Department WHERE DeptCode = {normalizedCode}");

        if (affectedRows == 0)
        {
            return NotFound(new Response<bool>(false, false, "Department not found"));
        }

        return Ok(new Response<bool>(true, true, "Success"));
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<ContactDeptItemDto>>>> GetContactDepts()
    {
        var items = await _db.ContactDepts
            .AsNoTracking()
            .OrderBy(x => x.ContactDeptId)
            .Select(x => new ContactDeptItemDto
            {
                ContactDeptId = x.ContactDeptId,
                ContactDeptName = x.ContactDeptName ?? string.Empty,
                RecordStatus = x.RecordStatus
            })
            .ToListAsync();

        return Ok(new Response<List<ContactDeptItemDto>>(true, items, "Success"));
    }

    [HttpPost]
    public async Task<ActionResult<Response<ContactDeptItemDto>>> CreateContactDept([FromBody] ContactDeptUpsertRequestDto request)
    {
        var contactDeptName = NormalizeRequired(request?.ContactDeptName);
        if (contactDeptName is null)
        {
            return BadRequest(new Response<ContactDeptItemDto>(false, null, "ContactDeptName is required"));
        }

        var newId = (await _db.ContactDepts.MaxAsync(x => (long?)x.ContactDeptId) ?? 0) + 1;
        var now = DateTime.Now;
        var entity = new ContactDept
        {
            ContactDeptId = newId,
            ContactDeptName = contactDeptName,
            RecordStatus = ActiveRecordStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ContactDepts.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new Response<ContactDeptItemDto>(true, new ContactDeptItemDto
        {
            ContactDeptId = entity.ContactDeptId,
            ContactDeptName = entity.ContactDeptName ?? string.Empty,
            RecordStatus = entity.RecordStatus
        }, "Success"));
    }

    [HttpPut]
    public async Task<ActionResult<Response<ContactDeptItemDto>>> UpdateContactDept([FromBody] ContactDeptUpsertRequestDto request)
    {
        if (request?.ContactDeptId is null || request.ContactDeptId <= 0)
        {
            return BadRequest(new Response<ContactDeptItemDto>(false, null, "ContactDeptId is required"));
        }

        var contactDeptName = NormalizeRequired(request.ContactDeptName);
        if (contactDeptName is null)
        {
            return BadRequest(new Response<ContactDeptItemDto>(false, null, "ContactDeptName is required"));
        }

        var entity = await _db.ContactDepts.FirstOrDefaultAsync(x => x.ContactDeptId == request.ContactDeptId.Value);
        if (entity is null)
        {
            return NotFound(new Response<ContactDeptItemDto>(false, null, "ContactDept not found"));
        }

        entity.ContactDeptName = contactDeptName;
        entity.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Ok(new Response<ContactDeptItemDto>(true, new ContactDeptItemDto
        {
            ContactDeptId = entity.ContactDeptId,
            ContactDeptName = entity.ContactDeptName ?? string.Empty,
            RecordStatus = entity.RecordStatus
        }, "Success"));
    }

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> DeleteContactDept([FromQuery] long contactDeptId)
    {
        if (contactDeptId <= 0)
        {
            return BadRequest(new Response<bool>(false, false, "contactDeptId is required"));
        }

        var entity = await _db.ContactDepts.FirstOrDefaultAsync(x => x.ContactDeptId == contactDeptId);
        if (entity is null)
        {
            return NotFound(new Response<bool>(false, false, "ContactDept not found"));
        }

        _db.ContactDepts.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new Response<bool>(true, true, "Success"));
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<PurposeItemDto>>>> GetPurposes()
    {
        var items = await _db.Purposes
            .AsNoTracking()
            .OrderBy(x => x.PurposeId)
            .Select(x => new PurposeItemDto
            {
                PurposeId = x.PurposeId,
                PurposeName = x.PurposeName ?? string.Empty,
                RecordStatus = x.RecordStatus
            })
            .ToListAsync();

        return Ok(new Response<List<PurposeItemDto>>(true, items, "Success"));
    }

    [HttpPost]
    public async Task<ActionResult<Response<PurposeItemDto>>> CreatePurpose([FromBody] PurposeUpsertRequestDto request)
    {
        var purposeName = NormalizeRequired(request?.PurposeName);
        if (purposeName is null)
        {
            return BadRequest(new Response<PurposeItemDto>(false, null, "PurposeName is required"));
        }

        var newId = (await _db.Purposes.MaxAsync(x => (long?)x.PurposeId) ?? 0) + 1;
        var now = DateTime.Now;
        var entity = new Purpose
        {
            PurposeId = newId,
            PurposeName = purposeName,
            RecordStatus = ActiveRecordStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Purposes.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new Response<PurposeItemDto>(true, new PurposeItemDto
        {
            PurposeId = entity.PurposeId,
            PurposeName = entity.PurposeName ?? string.Empty,
            RecordStatus = entity.RecordStatus
        }, "Success"));
    }

    [HttpPut]
    public async Task<ActionResult<Response<PurposeItemDto>>> UpdatePurpose([FromBody] PurposeUpsertRequestDto request)
    {
        if (request?.PurposeId is null || request.PurposeId <= 0)
        {
            return BadRequest(new Response<PurposeItemDto>(false, null, "PurposeId is required"));
        }

        var purposeName = NormalizeRequired(request.PurposeName);
        if (purposeName is null)
        {
            return BadRequest(new Response<PurposeItemDto>(false, null, "PurposeName is required"));
        }

        var entity = await _db.Purposes.FirstOrDefaultAsync(x => x.PurposeId == request.PurposeId.Value);
        if (entity is null)
        {
            return NotFound(new Response<PurposeItemDto>(false, null, "Purpose not found"));
        }

        entity.PurposeName = purposeName;
        entity.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Ok(new Response<PurposeItemDto>(true, new PurposeItemDto
        {
            PurposeId = entity.PurposeId,
            PurposeName = entity.PurposeName ?? string.Empty,
            RecordStatus = entity.RecordStatus
        }, "Success"));
    }

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> DeletePurpose([FromQuery] long purposeId)
    {
        if (purposeId <= 0)
        {
            return BadRequest(new Response<bool>(false, false, "purposeId is required"));
        }

        var entity = await _db.Purposes.FirstOrDefaultAsync(x => x.PurposeId == purposeId);
        if (entity is null)
        {
            return NotFound(new Response<bool>(false, false, "Purpose not found"));
        }

        _db.Purposes.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new Response<bool>(true, true, "Success"));
    }

    private static string? NormalizeRequired(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
