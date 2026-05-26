namespace WarehouseAccessAPI.Dtos;

public class AccessLogDetailDto
{
    public long LogId { get; set; }
    public string? CardNumber { get; set; }
    public DateTime? EventTime { get; set; }
    public string? DeptCode { get; set; }
    public string? DeptName { get; set; }
    public string? FullName { get; set; }
    public string? UserCode { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? ContactDept { get; set; }
    public string? Purpose { get; set; }
    public string? Photo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
