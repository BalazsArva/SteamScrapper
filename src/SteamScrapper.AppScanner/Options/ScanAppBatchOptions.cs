namespace SteamScrapper.AppScanner.Options
{
    public class ScanAppBatchOptions
    {
        public const string SectionName = "ScanAppBatch";

        public int DegreeOfParallelism { get; set; }
    }
}