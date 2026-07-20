using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace PortfolioFunctions.Models
{
    public class WorkExperience
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("company")]
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonProperty("companyWebsite")]
        [JsonPropertyName("companyWebsite")]
        public string? CompanyWebsite { get; set; }

        [JsonProperty("totalDuration")]
        [JsonPropertyName("totalDuration")]
        public string? TotalDuration { get; set; }

        [JsonProperty("roles")]
        [JsonPropertyName("roles")]
        public Role[]? Roles { get; set; }
    }

    public class Role
    {
        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonProperty("period")]
        [JsonPropertyName("period")]
        public string? Period { get; set; }

        [JsonProperty("location")]
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonProperty("bullets")]
        [JsonPropertyName("bullets")]
        public string[]? Bullets { get; set; }
    }
}
