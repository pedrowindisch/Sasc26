using System.IO.Compression;

namespace Sasc26.Services;

public static class FileCompressionHelper
{
    public static byte[] GZipCompress(byte[] raw)
    {
        using var msOut = new MemoryStream();
        using (var gz = new GZipStream(msOut, CompressionLevel.Optimal, leaveOpen: true))
            gz.Write(raw, 0, raw.Length);
        return msOut.ToArray();
    }

    public static byte[] GZipDecompress(byte[] compressed)
    {
        using var msIn = new MemoryStream(compressed);
        using var gz = new GZipStream(msIn, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        gz.CopyTo(outMs);
        return outMs.ToArray();
    }
}
