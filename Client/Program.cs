using System.Net;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TcpFileSender sender = new(IPAddress.Loopback, 9000);

            string[] filesToSend =
            [
                "your_files.txt"
            ];

            await sender.SendFilesAsync(filesToSend);
        }
    }
}
