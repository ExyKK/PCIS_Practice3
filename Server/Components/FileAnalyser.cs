using Server.Models;
using System.Collections.Concurrent;

namespace Server.Components
{
    public class FileAnalyser
    {
        private readonly string[] paths;

        public FileAnalyser(string[] paths)
        {
            this.paths = ValidateFilePaths(paths);
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

            var tasks = paths.Select(async file =>
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
