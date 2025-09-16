using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StackOverFlowExtractionTool.Models;

public class StackOverflowResponse
{
    [JsonPropertyName("items")]
    public List<StackOverflowQuestion> Items { get; set; } = new();

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("quota_max")]
    public int QuotaMax { get; set; }

    [JsonPropertyName("quota_remaining")]
    public int QuotaRemaining { get; set; }
}