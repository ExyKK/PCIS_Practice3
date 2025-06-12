using System.Net;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TcpFileServer server = new
            (
                host: IPAddress.Any, 
                port: 9000, 
                storageDirectory: "your_directory"
            );
            await server.Run();
        }
    }
}
