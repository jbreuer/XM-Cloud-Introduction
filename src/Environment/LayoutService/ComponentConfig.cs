namespace LayoutService;

public class ComponentConfig
{
    public bool UseSsr { get; set; }
    public Dictionary<string, (object newValue, FieldType fieldType)> Updates { get; set; } = new Dictionary<string, (object newValue, FieldType fieldType)>();
}