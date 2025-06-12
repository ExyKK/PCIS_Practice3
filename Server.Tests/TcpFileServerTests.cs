using Server;
using System.Net;
using System.Text;
using Xunit;

public class TcpFileServerTests
{
    [Fact]
    public async Task HandleStreamAsync_CreatesAnalysisFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var server = new TcpFileServer(IPAddress.Any, 0, tempDir);

        var stream = new MemoryStream();
        string fileName = "sample.txt";
        string content = "Hello world\nNew line";

        await WriteFakeFileToStreamAsync(stream, fileName, content);
        stream.Position = 0;

        // Act
        await server.HandleStreamAsync(stream);
        stream.Position = 0;

        // Assert
        var reader = new StreamReader(stream);
        string response = reader.ReadToEnd();

        Assert.Contains("Слов: 4", response);
        Assert.Contains("Символов: " + content.Length, response);
    }

    private async Task WriteFakeFileToStreamAsync(Stream stream, string fileName, string content)
    {
        void WriteInt(int value) => stream.Write(BitConverter.GetBytes(value), 0, 4);

        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        byte[] fileBytes = Encoding.UTF8.GetBytes(content);

        WriteInt(1); // 1 файл
        WriteInt(fileNameBytes.Length);
        stream.Write(fileNameBytes);
        WriteInt(fileBytes.Length);
        stream.Write(fileBytes);

        await stream.FlushAsync();
    }
}