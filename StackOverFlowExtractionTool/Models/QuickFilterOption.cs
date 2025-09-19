using System;

namespace StackOverFlowExtractionTool.Models;

public class QuickFilterOption
{
    public string DisplayName { get; set; } = string.Empty;
    public required Func<StackOverflowQuestion, bool> Filter { get; set; }
    public string Description { get; set; } = string.Empty;
}