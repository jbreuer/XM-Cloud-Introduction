namespace LayoutService;

public class ComponentConfig
{
    public Dictionary<string, (object newValue, FieldType fieldType)> Updates { get; set; } = new Dictionary<string, (object newValue, FieldType fieldType)>();
}