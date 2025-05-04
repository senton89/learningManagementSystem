namespace WpfApp1.Models;

public class Content
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string Data { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int ModuleId { get; set; }
    public virtual Module? Module { get; set; }
}

public enum ContentType
{
    Text,
    Video,
    Quiz,
    Assignment
}
