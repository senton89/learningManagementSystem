using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WpfApp1.Models;

public class Module
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int CourseId { get; set; }
    [JsonIgnore]
    public virtual Course? Course { get; set; }
    public virtual ICollection<Content>? Contents { get; set; }
}
