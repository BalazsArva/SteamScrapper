namespace SteamScrapper.AppScanner.Options
{
    public class ProcessAppBatchOptions
    {
        public const string SectionName = "ProcessAppBatch";

        public int DegreeOfParallelism { get; set; }
    }
}