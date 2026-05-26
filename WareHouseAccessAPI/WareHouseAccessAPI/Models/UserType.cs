using System;
using System.Collections.Generic;

namespace WarehouseAccessAPI.Models;

public partial class UserType
{
    public string UserTypeId { get; set; } = null!;

    public string? UserTypeName { get; set; }

    public string? RecordStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
