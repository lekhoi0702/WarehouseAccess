namespace WarehouseAccessAPI.Dtos;

public class CreateAccessLogCheckInRequestDto
{
    public string? CardNumber { get; set; }
    public string? UserCode { get; set; }
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? ContactDept { get; set; }
    public string? Purpose { get; set; }
    public string? Photo { get; set; }
}
