using Server.Components;
using System.Net.Sockets;
using System.Text;
using Xunit;

public class FileReceiverTests
{
    [Fact]
    public async Task ReceiveFilesAsync_ShouldSaveFile_WhenValidStreamProvided()
    {
        var fileName = "test.txt";
        var fileContent = "Hello from test file!";
        var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // 1. Кол-во файлов (1)
        writer.Write(1);

        // 2. Длина имени файла + имя
        writer.Write(fileNameBytes.Length);
        writer.Write(fileNameBytes);

        // 3. Длина содержимого файла + содержимое
        writer.Write(fileContentBytes.Length);
        writer.Write(fileContentBytes);

        writer.Flush();
        stream.Position = 0;

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var receiver = new FileReceiver(
            stream: stream,
            directory: tempDir,
            maxAllowedFileSize: 10 * 1024 * 1024,
            bufferSize: 1024
        );

        var savedPaths = await receiver.ReceiveFilesAsync();

        Assert.Single(savedPaths);
        string savedPath = savedPaths[0];
        Assert.True(File.Exists(savedPath));

        string savedContent = await File.ReadAllTextAsync(savedPath);
        Assert.Equal(fileContent, savedContent);

        File.Delete(savedPath);
        Directory.Delete(tempDir);
    }
}