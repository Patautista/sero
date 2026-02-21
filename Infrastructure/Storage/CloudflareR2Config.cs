namespace Infrastructure.Storage
{
    public class CloudflareR2Config
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string ApiAccessFile { get; set; } = "ApiAccess.json";
    }
}
