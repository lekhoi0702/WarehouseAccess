namespace WarehouseAccessAPI.Dtos;

public class UpdateUserRequestDto
{
    public string? UserCode { get; set; }
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? CardNumber { get; set; }
}
