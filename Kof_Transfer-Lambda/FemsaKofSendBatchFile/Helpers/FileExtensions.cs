namespace FemsaKofSendBatchFile.Helpers
{
    public static class Helper
    {
        public static readonly Dictionary<string, string> fileExtensions = new()
        {
            {"csv", "text/csv" },
            {"json", "application/json"}
        };
    };
}
