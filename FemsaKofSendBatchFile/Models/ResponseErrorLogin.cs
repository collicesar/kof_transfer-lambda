using Newtonsoft.Json;

namespace FemsaKofSendBatchFile.Models
{
    public class ResponseErrorLogin
    {
        public ErrorDetails ErrorDetails { get; set; }       
    }

    public class ErrorDetails
    {
        [JsonProperty("message")]
        public string? Message { get; set; }
        [JsonProperty("code")]
        public string? Code { get; set; }
        [JsonProperty("type")]
        public string? Type { get; set; }
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
        [JsonProperty("data")]
        public string? Data { get; set; }
    }
}
