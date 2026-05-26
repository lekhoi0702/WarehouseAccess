using System;
using System.Collections.Generic;

namespace WarehouseAccessAPI.Models;

public partial class ContactDept
{
    public long ContactDeptId { get; set; }

    public string? ContactDeptName { get; set; }

    public string? RecordStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
