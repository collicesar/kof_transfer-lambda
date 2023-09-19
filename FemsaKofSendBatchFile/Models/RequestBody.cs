using Newtonsoft.Json;

namespace FemsaKofSendBatchFile.Models
{
    public class RequestBody
    {
        [JsonProperty("file_name")]
        public string FileName { get; set; }
        [JsonProperty("file_type")]
        public string FileType { get; set; }
        [JsonProperty("sponsor_id")]
        public int SponsorId { get; set; }
        [JsonProperty("batch_id")]
        public int BatchId { get; set; }

        public RequestBody(string fileName, string fileType, int sponsorId, int batchId)
        {
            FileName = fileName;
            FileType = fileType;
            SponsorId = sponsorId;
            BatchId = batchId;
        }
    }
}
