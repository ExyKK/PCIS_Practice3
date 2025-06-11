using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    /// <summary>
    /// Сервер принимает подключения по TCP и ожидает, что клиент отправит один или несколько файлов для анализа.
    /// 
    /// Протокол взаимодействия устроен следующим образом:
    /// 
    /// 1. Клиент сначала отправляет 4 байта (Int32) — количество файлов, которые будут переданы.
    /// 
    /// 2. Затем для каждого файла передаётся:
    ///     1. 4 байта (Int32) — длина имени файла в байтах;
    ///     2. N байт — имя файла в кодировке UTF-8;
    ///     3. 4 байта (Int32) — длина содержимого файла в байтах;
    ///     4. M байт — текстовое содержимое файла.
    /// 
    /// После получения всех файлов сервер:
    ///     - сохраняет каждый файл на диск с уникальным именем;
    ///     - проводит асинхронный анализ всех файлов (подсчёт строк, слов, символов);
    ///     - сохраняет результаты анализа в локальный файл analysis_result.txt;
    ///     - отправляет результат анализа обратно клиенту в виде строки в кодировке UTF-8.
    ///
    /// </summary>
    public class TcpServer
    {
        private readonly IPAddress _host;
        private readonly int _port;
        private readonly string _saveDirectory;

        private readonly TcpListener _listener;

        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public TcpServer(IPAddress host, int port, string saveDirectory)
        {
            _host = host;
            _port = port;

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            _saveDirectory = saveDirectory;

            _listener = new TcpListener(_host, _port);
        }
        
        public async Task Run()
        {
            _listener.Start();
            Console.WriteLine($"[Сервер] Ожидание подключений на порту {_port}...");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"[Сервер] Подключён новый клиент: {client.Client.RemoteEndPoint}");
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();

            try
            {
                int filesCount = await ReadLengthAsync(stream);

                if (filesCount <= 0)
                {
                    throw new InvalidOperationException("[Сервер] Недопустимое количество файлов.");
                }

                Console.WriteLine($"[Сервер] Клиент отправит файлов: {filesCount}");

                List<string> savedFilePaths = [];

                for (int i = 0; i < filesCount; i++)
                {
                    string fileName = await ReadFileNameAsync(stream);
                    string uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                    string filePath = Path.Combine(_saveDirectory, uniqueName);

                    await ReadFileContentAsync(stream, filePath);
                    savedFilePaths.Add(filePath);

                    Console.WriteLine($"[Сервер] Файл сохранён: {uniqueName}");
                }

                FilesReader reader = new(savedFilePaths.ToArray());
                FileAnalysis[] results = await reader.AnalyseAsync();

                string analysisResultPath = Path.Combine(_saveDirectory, "analysis_result.txt");
                string formatted = string.Join("\n\n", results.Select(r => r.ToString())) + "\n\n";

                await _writeLock.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(analysisResultPath, formatted);
                }
                finally
                {
                    _writeLock.Release();
                }

                byte[] response = Encoding.UTF8.GetBytes(formatted);
                await stream.WriteAsync(response, 0, response.Length);

                Console.WriteLine($"[Сервер] Результаты анализа отправлены клиенту.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Сервер] Ошибка: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine($"[Сервер] Клиент {client.Client} отключён.");
            }
        }

        private async Task<string> ReadFileNameAsync(NetworkStream stream)
        {
            int length = await ReadLengthAsync(stream);

            if (length <= 0)
            {
                throw new InvalidOperationException("[Сервер] Недопустимая длина имени файла.");
            }

            byte[] buffer = new byte[length];
            int read = 0;
            while (read < length)
            {
                read += await stream.ReadAsync(buffer, read, length - read);
            }

            return Encoding.UTF8.GetString(buffer);
        }

        private async Task ReadFileContentAsync(NetworkStream stream, string filePath)
        {
            const int MaxAllowedLength = 100 * 1024 * 1024; // 100 MB
            const int BufferSize = 64 * 1024; // 64 KB

            int length = await ReadLengthAsync(stream);

            if (length <= 0 || length > MaxAllowedLength)
            {
                throw new InvalidOperationException($"[Сервер] Недопустимый размер файла: {length} байт.");
            }

            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
            byte[] buffer = new byte[BufferSize];
            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesToRead = Math.Min(BufferSize, length - totalRead);
                int bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0)
                {
                    throw new IOException("[Сервер] Соединение прервано до окончания передачи файла.");
                }

                await fs.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
            }
        }

        private async Task<int> ReadLengthAsync(NetworkStream stream)
        {
            byte[] lengthBytes = new byte[4];
            int totalRead = 0;

            while (totalRead < 4)
            {
                int bytesRead = await stream.ReadAsync(lengthBytes, totalRead, 4 - totalRead);
                if (bytesRead == 0)
                {
                    throw new IOException("[Сервер] Не удалось прочитать длину. Соединение прервано.");
                }    
                totalRead += bytesRead;
            }

            int length = BitConverter.ToInt32(lengthBytes);
            return length;
        }
    }
}
