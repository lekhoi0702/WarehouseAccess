namespace WarehouseAccessAPI.Dtos;

public class CreateUserRequestDto
{
    public string? UserCode { get; set; }
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? CardNumber { get; set; }
}
