namespace FemsaKofSendBatchFile.Validators.Abstractions
{
    public interface IValidEnvironmentVariables
    {
        bool IsValidEnvironmentVariables(string gravtyLoginUrl, string gravtyGetSignedUrl, string apiKey, string userName, string password);        
    }
}
