using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace FemsaKofSendBatchFile.Models
{
    public class RequestLogin
    {
        [JsonProperty("username")]
        public string UserName { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }

        public RequestLogin(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }
    }
}
