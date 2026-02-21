using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using System.Text.Json;

namespace Infrastructure.Storage
{
    public class CloudflareR2Client
    {
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName;

        public CloudflareR2Client(string accountId, string accessKeyId, string secretAccessKey, string bucketName)
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(credentials, config);
            _bucketName = bucketName;
        }

        /// <summary>
        /// Downloads a file from Cloudflare R2 and returns its content as a string
        /// </summary>
        public async Task<string> DownloadFileAsStringAsync(string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Downloads a JSON file from Cloudflare R2 and deserializes it to the specified type
        /// </summary>
        public async Task<T?> DownloadJsonAsync<T>(string key)
        {
            var jsonContent = await DownloadFileAsStringAsync(key);
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Uploads a file to Cloudflare R2
        /// </summary>
        public async Task UploadFileAsync(string key, string content)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = content
            };

            await _s3Client.PutObjectAsync(request);
        }

        /// <summary>
        /// Lists all files in the bucket
        /// </summary>
        public async Task<List<string>> ListFilesAsync(string? prefix = null)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            var response = await _s3Client.ListObjectsV2Async(request);
            return response.S3Objects.Select(obj => obj.Key).ToList();
        }
    }
}
