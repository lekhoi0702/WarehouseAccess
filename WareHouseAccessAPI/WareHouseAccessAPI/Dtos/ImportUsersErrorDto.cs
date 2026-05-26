namespace WarehouseAccessAPI.Dtos;

public class ImportUsersErrorDto
{
    public int RowNumber { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
