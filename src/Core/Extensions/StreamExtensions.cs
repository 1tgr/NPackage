using System.IO;

namespace NPackage.Core.Extensions
{
    public static class StreamExtensions
    {
        public static void CopyTo(this Stream inputStream, string outputFilename)
        {
            using (Stream outputStream = File.Create(outputFilename))
            {
                int count;
                byte[] chunk = new byte[4096];
                while ((count = inputStream.Read(chunk, 0, chunk.Length)) > 0)
                    outputStream.Write(chunk, 0, count);
            }
        }
    }
}