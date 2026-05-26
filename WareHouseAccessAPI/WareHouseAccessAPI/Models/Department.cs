using System;
using System.Collections.Generic;

namespace WarehouseAccessAPI.Models;

public partial class Department
{
    public string? DeptCode { get; set; }

    public string? DeptName { get; set; }

    public string? RecordStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
