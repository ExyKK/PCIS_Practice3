using System.Collections.Concurrent;

namespace Server
{
    public class FilesReader
    {
        private readonly string[] filePaths;

        public FilesReader(string[] filePaths)
        {
            this.filePaths = ValidateFilePaths(filePaths);
        }

        private string[] ValidateFilePaths(string[] inputFiles)
        {
            List<string> validFiles = [];
            List<string> skipped = [];

            foreach (var file in inputFiles)
            {
                if (File.Exists(file))
                {
                    validFiles.Add(file);
                }
                else
                {
                    skipped.Add(file);
                }
            }
            if (skipped.Count > 0)
            {
                throw new FileNotFoundException($"Файлы не найдены:\n{string.Join("\n", skipped)}");
            }

            return validFiles.ToArray();
        }

        public async Task<FileAnalysis[]> AnalyseAsync()
        {
            char[] delim = " ,.:;?!()`\n\r\t".ToCharArray();
            ConcurrentBag<FileAnalysis> results = [];

            var tasks = filePaths.Select(async file =>
            {
                string text = await File.ReadAllTextAsync(file);

                var analysis = await Task.Run(() =>
                {
                    string[] words = text.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    int linesCount = text.Split('\n').Length;
                    return new FileAnalysis(Path.GetFileName(file), linesCount, words.Length, text.Length);
                });

                results.Add(analysis);
            });

            await Task.WhenAll(tasks);

            return results.ToArray();
        }
    }
}
