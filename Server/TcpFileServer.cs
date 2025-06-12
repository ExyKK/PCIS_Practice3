using Server.Components;
using Server.Models;
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
    public class TcpFileServer
    {
        private readonly IPAddress _host;
        private readonly int _port;
        private readonly string _storageDirectory;

        private readonly TcpListener _listener;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public TcpFileServer(IPAddress host, int port, string storageDirectory)
        {
            _host = host;
            _port = port;

            Directory.CreateDirectory(storageDirectory);
            _storageDirectory = storageDirectory;

            _listener = new TcpListener(_host, _port);
        }
        
        public async Task Run()
        {
            _listener.Start();
            Console.WriteLine($"[Сервер] Ожидание подключений на порту {_port}...");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"[Сервер] Подключен новый клиент: {client.Client.RemoteEndPoint}");
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            await HandleStreamAsync(stream);

            Console.WriteLine($"[Сервер] Клиент {client.Client.RemoteEndPoint} отключен");
            client.Close();
        }

        public async Task HandleStreamAsync(Stream stream)
        {
            try
            {
                FileReceiver receiver = new
                (
                    stream: stream,
                    directory: _storageDirectory,
                    maxAllowedFileSize: 100 * 1024 * 1024, // 100 MB
                    bufferSize: 64 * 1024 // 64 KB
                );
                List<string> savedFilePaths = await receiver.ReceiveFilesAsync();

                FileAnalyser analyser = new(savedFilePaths.ToArray());
                FileAnalysis[] results = await analyser.AnalyseAsync();
                string formattedResults = string.Join("\n\n", results.Select(r => r.ToString())) + "\n\n";

                string analysisResultPath = Path.Combine(_storageDirectory, "analysis_result.txt");

                await _writeLock.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(analysisResultPath, formattedResults);
                }
                finally
                {
                    _writeLock.Release();
                }

                byte[] response = Encoding.UTF8.GetBytes(formattedResults);
                await stream.WriteAsync(response, 0, response.Length);

                Console.WriteLine($"[Сервер] Результаты анализа отправлены клиенту.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Сервер] Ошибка: {ex.Message}");
            }
        }
    }
}
