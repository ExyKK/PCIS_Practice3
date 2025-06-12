using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class TcpFileSender
    {
        private readonly IPAddress _serverIp;
        private readonly int _serverPort;

        public TcpFileSender(IPAddress serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task SendFilesAsync(string[] filePaths)
        {
            using TcpClient client = new();
            await client.ConnectAsync(_serverIp, _serverPort);
            using NetworkStream stream = client.GetStream();

            await SendFilesToStreamAsync(stream, filePaths);
            await ReadServerResponseAsync(stream);
        }

        public async Task SendFilesToStreamAsync(Stream stream, string[] filePaths)
        {
            await WriteIntAsync(stream, filePaths.Length);

            foreach (string path in filePaths)
            {
                string fileName = Path.GetFileName(path);
                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                byte[] fileBytes = await File.ReadAllBytesAsync(path);

                await WriteIntAsync(stream, nameBytes.Length);
                await stream.WriteAsync(nameBytes);

                await WriteIntAsync(stream, fileBytes.Length);
                await stream.WriteAsync(fileBytes);

                Console.WriteLine($"[Клиент] Отправлен файл: {fileName}");
            }
        }

        private async Task ReadServerResponseAsync(Stream stream)
        {
            byte[] buffer = new byte[8192];
            StringBuilder response = new();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                response.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }

            Console.WriteLine("[Клиент] Ответ от сервера:\n" + response);
        }

        private async Task WriteIntAsync(Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
