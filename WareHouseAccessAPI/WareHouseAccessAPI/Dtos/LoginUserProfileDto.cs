namespace WarehouseAccessAPI.Dtos;

public class LoginUserProfileDto
{
    public string UserCode { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? UserTypeId { get; set; }
    public string? RecordStatus { get; set; }
    public string? CardNumber { get; set; }
}

