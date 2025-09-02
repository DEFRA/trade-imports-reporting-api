using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Defra.TradeImportsReportingApi.Api.Utils;

public static class MessageDeserializer
{
    public static T? Deserialize<T>(string message, string? contentEncoding)
    {
        if (contentEncoding != null && contentEncoding != "gzip, base64")
        {
            throw new NotImplementedException(
                "Only 'gzip, base64' content encoding is supported, passed: " + contentEncoding
            );
        }

        if (contentEncoding == null)
            return JsonSerializer.Deserialize<T>(message);

        var compressedBytes = Convert.FromBase64String(message);
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);

        return JsonSerializer.Deserialize<T>(reader.ReadToEnd());
    }
}
