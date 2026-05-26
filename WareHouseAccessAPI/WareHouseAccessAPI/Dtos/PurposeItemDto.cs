namespace WarehouseAccessAPI.Dtos;

public class PurposeItemDto
{
    public long PurposeId { get; set; }
    public string PurposeName { get; set; } = string.Empty;
    public string? RecordStatus { get; set; }
}
