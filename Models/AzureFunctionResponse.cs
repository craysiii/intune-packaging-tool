using System.Text.Json.Serialization;

namespace IntunePackagingTool.Models;

public class AzureFunctionResponse
{
    [JsonPropertyName("responseCode")]
    public int ResponseCode { get; set; }
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}