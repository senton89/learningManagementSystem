using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfApp1.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TeacherId { get; set; }
    public virtual User? Teacher { get; set; }
    public virtual ICollection<User>? Students { get; set; }
    // public virtual ICollection<User>? Teachers { get; set; } 
    [JsonIgnore]
    public virtual ICollection<Module>? Modules { get; set; }

    public CourseAccessType AccessType { get; set; } = CourseAccessType.Open;
}
