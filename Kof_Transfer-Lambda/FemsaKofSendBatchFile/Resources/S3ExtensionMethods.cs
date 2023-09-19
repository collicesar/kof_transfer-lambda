using Amazon.S3.Model;

namespace FemsaKofSendBatchFile.Resources
{
    public static class S3ExtensionMethods
    {        
        public static async Task<byte[]> ToBinaryDataAsync(this GetObjectResponse getObjectResponse)
        {
            using (var stream = getObjectResponse.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
