using System.Text.Json.Serialization;

namespace StackOverFlowExtractionTool.Models;

public class Owner
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("reputation")]
    public int Reputation { get; set; }

    [JsonPropertyName("profile_image")]
    public string ProfileImage { get; set; } = string.Empty;
}