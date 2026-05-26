namespace WarehouseAccessAPI.Dtos;

public class UserListItemDto
{
    public string UserCode { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string? FullName { get; set; }
    public string? DeptCode { get; set; }
    public string? DeptName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
