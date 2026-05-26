using System;
using System.Collections.Generic;

namespace WarehouseAccessAPI.Models;

public partial class User
{
    public string UserCode { get; set; } = null!;

    public string? FullName { get; set; }

    public string? DeptCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UserTypeId { get; set; }

    public string? CardNumber { get; set; }

    public string? RecordStatus { get; set; }
}
