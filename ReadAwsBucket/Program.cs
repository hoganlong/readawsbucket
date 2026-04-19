using Amazon.S3;
using Amazon.S3.Model;

namespace ReadAwsBucket;

class Program
{
    static async Task Main(string[] args)
    {
      /*
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ReadAwsBucket <bucket-name> [region]");
            Console.WriteLine("Example: ReadAwsBucket my-bucket-name us-east-1");
            return;
        }

        string bucketName = args[0];
        */
        string bucketName="keithlong-art-photos";
        string region = args.Length > 1 ? args[1] : "us-east-1";

        try
        {
            var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));

            Console.WriteLine($"Connecting to S3 bucket: {bucketName} in region: {region}");
            Console.WriteLine("Listing files...\n");

            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            ListObjectsV2Response response;
            int totalFiles = 0;

            do
            {
                response = await s3Client.ListObjectsV2Async(request);

                foreach (S3Object obj in response.S3Objects)
                {
                    totalFiles++;
                    Console.WriteLine($"Key: {obj.Key}");
                    Console.WriteLine($"  Size: {FormatBytes(obj.Size ?? 0)}");
                    Console.WriteLine($"  Last Modified: {obj.LastModified}");
                    Console.WriteLine($"  Storage Class: {obj.StorageClass}");
                    Console.WriteLine();
                }

                request.ContinuationToken = response.NextContinuationToken;

            } while (response.IsTruncated == true);

            Console.WriteLine($"Total files: {totalFiles}");
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"AWS S3 Error: {ex.Message}");
            Console.WriteLine($"Error Code: {ex.ErrorCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
