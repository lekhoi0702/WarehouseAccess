using System;
using System.Collections.Generic;

namespace WarehouseAccessAPI.Models;

public partial class Purpose
{
    public long PurposeId { get; set; }

    public string? PurposeName { get; set; }

    public string? RecordStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
