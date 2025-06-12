namespace Server.Models
{
    public class FileAnalysis
    {
        public string Filename { get; set; }
        public int LinesCount { get; set; }
        public int WordsCount { get; set; }
        public int SymbolsCount { get; set; }

        public FileAnalysis(string filename, int linesCount, int wordsCount, int symbolsCount)
        {
            Filename = filename;
            LinesCount = linesCount;
            WordsCount = wordsCount;
            SymbolsCount = symbolsCount;
        }

        public override string ToString()
        {
            return $"Имя файла: {Filename}\nСтрок: {LinesCount}, Слов: {WordsCount}, Символов: {SymbolsCount}";
        }
    }
}
