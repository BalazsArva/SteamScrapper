namespace SteamScrapper.BundleScanner.Options
{
    public class ScanBundleBatchOptions
    {
        public const string SectionName = "ScanBundleBatch";

        public int DegreeOfParallelism { get; set; }
    }
}