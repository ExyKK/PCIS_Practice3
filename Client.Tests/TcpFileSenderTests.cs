using Client;
using System.Text;
using Xunit;

public class TcpFileSenderTests
{
    [Fact]
    public async Task SendToStreamAsync_SendsCorrectFileData()
    {
        string tempFile = Path.GetTempFileName();
        string content = "Hello world";
        await File.WriteAllTextAsync(tempFile, content);

        var stream = new MemoryStream();
        var sender = new TcpFileSender(null, 0);

        await sender.SendFilesToStreamAsync(stream, [tempFile]);

        stream.Position = 0;

        // Проверим: сначала 4 байта — количество файлов
        byte[] intBuffer = new byte[4];
        await stream.ReadAsync(intBuffer, 0, 4);
        int fileCount = BitConverter.ToInt32(intBuffer);
        Assert.Equal(1, fileCount);

        // Имя файла
        await stream.ReadAsync(intBuffer, 0, 4);
        int nameLen = BitConverter.ToInt32(intBuffer);
        byte[] nameBytes = new byte[nameLen];
        await stream.ReadAsync(nameBytes, 0, nameLen);

        string sentFileName = Encoding.UTF8.GetString(nameBytes);
        Assert.Equal(Path.GetFileName(tempFile), sentFileName);

        // Данные файла
        await stream.ReadAsync(intBuffer, 0, 4);
        int dataLen = BitConverter.ToInt32(intBuffer);
        byte[] dataBytes = new byte[dataLen];
        await stream.ReadAsync(dataBytes, 0, dataLen);
        string data = Encoding.UTF8.GetString(dataBytes);

        Assert.Equal(content, data);
    }
}