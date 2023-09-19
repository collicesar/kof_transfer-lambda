using FemsaKofSendBatchFile.Validators.Abstractions;

namespace FemsaKofSendBatchFile.Validators
{
    public class ValidEnvironmentVariables : IValidEnvironmentVariables
    {
        bool IValidEnvironmentVariables.IsValidEnvironmentVariables(string gravtyLoginUrl, string gravtyGetSignedUrl, string apiKey, string userName, string password)
        {
            if (string.IsNullOrEmpty(gravtyLoginUrl) || !Uri.IsWellFormedUriString(gravtyLoginUrl, UriKind.Absolute))
                return false;
            if (string.IsNullOrEmpty(gravtyGetSignedUrl) || !Uri.IsWellFormedUriString(gravtyGetSignedUrl, UriKind.Absolute))
                return false;
            if(string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return false;
            return true;
        }
    }
}
