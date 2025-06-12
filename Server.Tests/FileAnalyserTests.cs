using Server.Components;
using Server.Models;
using Xunit;

public class FileAnalyserTests
{
    [Fact]
    public async Task AnalyseAsync_ReturnsCorrectCounts()
    {
        string tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello world\nSecond line\nThird line!");
        var analyser = new FileAnalyser([tempFile]);

        FileAnalysis[] result = await analyser.AnalyseAsync();

        Assert.Single(result);
        Assert.Equal(Path.GetFileName(tempFile), result[0].Filename);
        Assert.Equal(3, result[0].LinesCount);
        Assert.Equal(6, result[0].WordsCount);
        Assert.Equal((await File.ReadAllTextAsync(tempFile)).Length, result[0].SymbolsCount);

        File.Delete(tempFile);
    }

    [Fact]
    public void Constructor_ShouldThrow_IfFileNotFound()
    {
        string nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent.txt");

        var ex = Assert.Throws<FileNotFoundException>(() =>
        {
            var analyser = new FileAnalyser([nonExistentFile]);
        });

        Assert.Contains("Файлы не найдены", ex.Message);
    }
}