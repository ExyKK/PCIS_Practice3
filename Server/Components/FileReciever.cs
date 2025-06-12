using System.Net.Sockets;
using System.Text;

namespace Server.Components
{
    public class FileReceiver
    {
        private readonly Stream _stream;
        private readonly string _directory;

        private readonly int _maxAllowedFileSize;
        private readonly int _bufferSize;

        public FileReceiver(Stream stream, string directory, int maxAllowedFileSize, int bufferSize)
        {
            _stream = stream;
            _directory = directory;
            _maxAllowedFileSize = maxAllowedFileSize;
            _bufferSize = bufferSize;
        }

        public async Task<List<string>> ReceiveFilesAsync()
        {
            int count = await ReadIntAsync();

            if (count <= 0)
            {
                throw new InvalidOperationException("[Сервер] Недопустимое количество файлов.");
            }
            Console.WriteLine($"[Сервер] Клиент отправит файлов: {count}");

            List<string> paths = [];

            for (int i = 0; i < count; i++)
            {
                string fileName = await ReadStringAsync();
                string uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                string filePath = Path.Combine(_directory, uniqueName);

                await ReadToFileAsync(filePath);
                paths.Add(filePath);

                Console.WriteLine($"[Сервер] Файл сохранён: {uniqueName}");
            }

            return paths;
        }

        private async Task ReadToFileAsync(string path)
        {
            int length = await ReadIntAsync();
            if (length <= 0 || length > _maxAllowedFileSize)
            {
                throw new InvalidOperationException($"[Сервер] Недопустимый размер файла: {length} байт.");
            }

            using FileStream fs = new(path, FileMode.Create);
            byte[] buffer = new byte[_bufferSize];
            int total = 0;

            while (total < length)
            {
                int toRead = Math.Min(buffer.Length, length - total);
                int read = await _stream.ReadAsync(buffer, 0, toRead);

                if (read == 0)
                {
                    throw new IOException("[Сервер] Ошибка чтения: внезапный конец потока.");
                }

                await fs.WriteAsync(buffer, 0, read);
                total += read;
            }
        }

        private async Task<string> ReadStringAsync()
        {
            int length = await ReadIntAsync();
            if (length <= 0)
            {
                throw new InvalidOperationException("[Сервер] Недопустимое значение длины.");
            }

            byte[] buffer = new byte[length];
            await ReadExactAsync(buffer, length);

            return Encoding.UTF8.GetString(buffer);
        }

        private async Task<int> ReadIntAsync()
        {
            byte[] buffer = new byte[4];
            await ReadExactAsync(buffer, 4);
            return BitConverter.ToInt32(buffer);
        }

        private async Task ReadExactAsync(byte[] buffer, int length)
        {
            int offset = 0;
            while (offset < length)
            {
                int read = await _stream.ReadAsync(buffer, offset, length - offset);
                if (read == 0)
                {
                    throw new IOException("[Сервер] Ошибка чтения: внезапный конец потока.");
                }
                offset += read;
            }
        }
    }
}
