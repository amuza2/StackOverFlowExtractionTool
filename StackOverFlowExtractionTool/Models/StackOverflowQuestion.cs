using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StackOverFlowExtractionTool.Models;

public class StackOverflowQuestion
{
    [JsonPropertyName("question_id")]
    public int QuestionId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("is_answered")]
    public bool IsAnswered { get; set; }

    [JsonPropertyName("view_count")]
    public int ViewCount { get; set; }

    [JsonPropertyName("answer_count")]
    public int AnswerCount { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("last_activity_date")]
    public long LastActivityDate { get; set; }

    [JsonPropertyName("creation_date")]
    public long CreationDate { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; }
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    public string OwnerDisplayName => Owner?.DisplayName ?? "Unknown";

    public DateTime CreationDateTime => DateTimeOffset.FromUnixTimeSeconds(CreationDate).DateTime;
    public DateTime LastActivityDateTime => DateTimeOffset.FromUnixTimeSeconds(LastActivityDate).DateTime;
    public string FormattedViewCount
    {
        get
        {
            if (ViewCount >= 1000000)
                return $"{ViewCount / 1000000.0:F1}M";
            if (ViewCount >= 1000)
                return $"{ViewCount / 1000.0:F1}K";
            return ViewCount.ToString();
        }
    }
}