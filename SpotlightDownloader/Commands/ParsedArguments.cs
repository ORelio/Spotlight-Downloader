namespace SpotlightDownloader.Commands
{
    internal class ParsedArguments
    {
        public string Action { get; set; }
        public bool SingleImage { get; set; }
        public bool MaximumRes { get; set; }
        public bool? Portrait { get; set; }
        public string Locale { get; set; }
        public bool AllLocales { get; set; }
        public string OutputDir { get; set; } = ".";
        public string OutputName { get; set; } = "spotlight";
        public bool IntegrityCheck { get; set; } = true;
        public bool DownloadMany { get; set; }
        public int DownloadAmount { get; set; } = int.MaxValue;
        public int CacheSize { get; set; } = int.MaxValue;
        public bool Metadata { get; set; }
        public bool MetadataAllowInconsistent { get; set; }
        public bool EmbedMetadata { get; set; }
        public string FromFile { get; set; }
        public int ApiTryCount { get; set; } = 3;
        public Spotlight.ApiVersion ApiVersion { get; set; } = Spotlight.ApiVersion.v4;
        public bool Verbose { get; set; }
    }
}