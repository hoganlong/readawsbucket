using Amazon.S3;
using Amazon.S3.Model;

namespace ReadAwsBucket;

class Program
{
    static async Task Main(string[] args)
    {
        string bucketName = "keithlong-art-photos";
        string region = "us-east-1";

        if (args.Any(a => a is "-h" or "--help" or "-?" or "/?" or "?"))
        {
            PrintUsage();
            return;
        }
        foreach (var a in args)
        {
            if ((a.StartsWith("-") || a.StartsWith("/")) && a is not ("--unique" or "--no-recurse"))
            {
                Console.WriteLine($"Unknown option: {a}");
                Console.WriteLine();
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }
        }

        bool unique = args.Any(a => a == "--unique");
        bool noRecurse = args.Any(a => a == "--no-recurse");
        var positional = args.Where(a => a != "--unique" && a != "--no-recurse").ToArray();

        if (positional.Length < 2)
        {
            PrintUsage();
            return;
        }

        string prefix = positional[0];
        string outputFile = positional[1];
        string format = positional.Length > 2 ? positional[2] : "<prefix><filename><ext>";

        try
        {
            var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));

            Console.WriteLine($"Bucket: {bucketName} ({region})");
            Console.WriteLine($"Prefix: {(string.IsNullOrEmpty(prefix) ? "(whole bucket)" : prefix)}");
            Console.WriteLine($"Format: {format}");
            Console.WriteLine($"Output: {outputFile}");
            Console.WriteLine();

            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            if (noRecurse) request.Delimiter = "/";

            using var writer = new StreamWriter(outputFile);
            var seen = unique ? new HashSet<string>() : null;
            int total = 0;
            int skipped = 0;
            ListObjectsV2Response response;

            do
            {
                response = await s3Client.ListObjectsV2Async(request);
                foreach (S3Object obj in response.S3Objects)
                {
                    string key = obj.Key;
                    if (key.EndsWith("/")) continue;

                    int slash = key.LastIndexOf('/');
                    string keyPrefix = slash >= 0 ? key[..(slash + 1)] : "";
                    string filename = slash >= 0 ? key[(slash + 1)..] : key;
                    int dot = filename.LastIndexOf('.');
                    string baseName = dot >= 0 ? filename[..dot] : filename;
                    string ext = dot >= 0 ? filename[dot..] : "";

                    string line = format
                        .Replace("<prefix>", keyPrefix)
                        .Replace("<filename>", baseName)
                        .Replace("<ext>", ext);

                    if (seen is not null && !seen.Add(line)) { skipped++; continue; }
                    writer.WriteLine(line);
                    total++;
                }
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated == true);

            if (unique && skipped > 0)
                Console.WriteLine($"Wrote {total} unique lines to {outputFile} ({skipped} duplicates dropped)");
            else
                Console.WriteLine($"Wrote {total} lines to {outputFile}");
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

    static void PrintUsage()
    {
        Console.WriteLine("Usage: ReadAwsBucket <prefix> <outputFile> [format] [--unique] [--no-recurse]");
        Console.WriteLine();
        Console.WriteLine("  prefix        S3 prefix to filter (e.g. \"scans/\"; use \"\" for whole bucket)");
        Console.WriteLine("  outputFile    text file to write the list to (one entry per line)");
        Console.WriteLine("  format        line template, default \"<prefix><filename><ext>\"");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --unique               drop duplicate lines (preserves first-seen order)");
        Console.WriteLine("  --no-recurse           list only files directly under the prefix; skip subdirectories");
        Console.WriteLine("  -h, --help, -?, /?, ?  show this help and exit");
        Console.WriteLine();
        Console.WriteLine("  Tokens replaced in format:");
        Console.WriteLine("    <prefix>    directory portion of key with trailing slash (e.g. \"scans/\")");
        Console.WriteLine("    <filename>  base filename without extension (e.g. \"KLA_1_1\")");
        Console.WriteLine("    <ext>       file extension with leading dot (e.g. \".tif\")");
        Console.WriteLine();
        Console.WriteLine("  Examples:");
        Console.WriteLine("    dotnet run -- scans/ scans.txt                                    # full keys, recursive");
        Console.WriteLine("    dotnet run -- scans/ scans.txt \"<filename>\"                       # base names");
        Console.WriteLine("    dotnet run -- scans/ scans.txt \"<filename>\" --unique --no-recurse # top-level deduped");
    }
}
