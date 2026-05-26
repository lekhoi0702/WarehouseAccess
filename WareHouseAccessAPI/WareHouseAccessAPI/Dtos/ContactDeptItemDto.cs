namespace WarehouseAccessAPI.Dtos;

public class ContactDeptItemDto
{
    public long ContactDeptId { get; set; }
    public string ContactDeptName { get; set; } = string.Empty;
    public string? RecordStatus { get; set; }
}
