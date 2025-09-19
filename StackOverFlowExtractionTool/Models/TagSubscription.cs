using System;

namespace StackOverFlowExtractionTool.Models;

public class TagSubscription
{
    public string Tag { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; } = DateTime.Now.AddMinutes(-5); // Force immediate check
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(2);
    public bool IsActive { get; set; } = true;
    public int LastQuestionId { get; set; }
}