using System.Data;
using Newtonsoft.Json;

namespace FemsaKofSendBatchFile.Models
{
    public class ResponseLogin
    {
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
