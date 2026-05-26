namespace WarehouseAccessAPI.Dtos;

public class DepartmentItemDto
{
    public string DeptCode { get; set; } = string.Empty;
    public string DeptName { get; set; } = string.Empty;
    public string? RecordStatus { get; set; }
}
