using System.Net;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TcpServer server = new(IPAddress.Any, 9000, "your_directory");
            await server.Run();
        }
    }
}
