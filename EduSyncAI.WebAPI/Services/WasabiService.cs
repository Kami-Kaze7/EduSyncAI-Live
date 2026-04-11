using Amazon.S3;
using Amazon.S3.Model;

namespace EduSyncAI.WebAPI.Services
{
    public class WasabiService
    {
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName;

        public WasabiService(IConfiguration configuration)
        {
            var wasabiConfig = configuration.GetSection("Wasabi");
            var accessKey = wasabiConfig["AccessKey"] ?? throw new Exception("Wasabi AccessKey not configured");
            var secretKey = wasabiConfig["SecretKey"] ?? throw new Exception("Wasabi SecretKey not configured");
            _bucketName = wasabiConfig["BucketName"] ?? "edusyncai-videos";
            var serviceUrl = wasabiConfig["ServiceUrl"] ?? "https://s3.eu-west-1.wasabisys.com";
            var region = wasabiConfig["Region"] ?? "eu-west-1";

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = region
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        /// <summary>
        /// Generate a pre-signed URL for the browser to upload a file directly to Wasabi.
        /// </summary>
        public string GenerateUploadUrl(string objectKey, string contentType, int expirationMinutes = 30)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                ContentType = contentType
            };

            return _s3Client.GetPreSignedURL(request);
        }

        /// <summary>
        /// Generate a pre-signed URL for streaming/downloading a video.
        /// </summary>
        public string GenerateStreamUrl(string objectKey, int expirationHours = 6)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(expirationHours)
            };

            return _s3Client.GetPreSignedURL(request);
        }

        /// <summary>
        /// Generate a pre-signed URL for downloading with proper Content-Disposition header.
        /// </summary>
        public string GenerateDownloadUrl(string objectKey, string fileName, int expirationHours = 6)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{fileName}\""
                }
            };

            return _s3Client.GetPreSignedURL(request);
        }

        /// <summary>
        /// Delete a video from the Wasabi bucket.
        /// </summary>
        public async Task DeleteObjectAsync(string objectKey)
        {
            await _s3Client.DeleteObjectAsync(_bucketName, objectKey);
        }

        /// <summary>
        /// Check if an object exists in the bucket.
        /// </summary>
        public async Task<bool> ObjectExistsAsync(string objectKey)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_bucketName, objectKey);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
