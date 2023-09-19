using Newtonsoft.Json;

namespace FemsaKofSendBatchFile.Models
{
    public class ResponseBody
    {
        [JsonProperty("signed_request")]
        public string? SignedRequest { get; set; }
        [JsonProperty("url")]
        public string? URL { get; set; }
    }
}
