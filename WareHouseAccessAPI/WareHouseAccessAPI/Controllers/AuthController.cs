using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAccessAPI.Common;
using WarehouseAccessAPI.Dtos;

namespace WarehouseAccessAPI.Controllers;

[ApiController]
[Route("WarehouseAccess/[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly WarehouseAccessAPI.Data.WarehouseAccessDbContext _db;

    public AuthController(WarehouseAccessAPI.Data.WarehouseAccessDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<Response<LoginUserProfileDto>>> LoginByCard([FromBody] LoginByCardRequestDto request)
    {
        var cardNumber = request?.CardNumber?.Trim();
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return BadRequest(new Response<LoginUserProfileDto>(false, null, "CardNumber is required"));
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(x => x.CardNumber == cardNumber)
            .Select(x => new LoginUserProfileDto
            {
                UserCode = x.UserCode,
                FullName = x.FullName,
                DeptCode = x.DeptCode,
                UserTypeId = x.UserTypeId,
                RecordStatus = x.RecordStatus,
                CardNumber = x.CardNumber
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(new Response<LoginUserProfileDto>(false, null, "Card not found"));
        }

        if (!string.Equals(user.RecordStatus?.Trim(), "2", StringComparison.Ordinal))
        {
            return Unauthorized(new Response<LoginUserProfileDto>(false, null, "User is not allowed to login"));
        }

        return Ok(new Response<LoginUserProfileDto>(true, user, "Login success"));
    }
}

