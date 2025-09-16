namespace StackOverFlowExtractionTool.Models;

public class PageSizeOption
{
    public int Value { get; set; }
    public string Display { get; set; } = string.Empty;
    
    public PageSizeOption(int value)
    {
        Value = value;
        Display = value.ToString();
    }
}