using System;

namespace StackOverFlowExtractionTool.Models;

public class Subscription
{
    public string Tag { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; } = DateTime.Now;
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool IsActive { get; set; } = true;
}