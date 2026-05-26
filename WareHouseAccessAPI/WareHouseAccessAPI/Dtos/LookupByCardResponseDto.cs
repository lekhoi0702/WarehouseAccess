namespace WarehouseAccessAPI.Dtos;

public class LookupByCardResponseDto
{
    public string? CardNumber { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? DeptName { get; set; }
}
