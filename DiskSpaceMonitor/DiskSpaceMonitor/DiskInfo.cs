namespace DiskSpaceMonitor
{
    public class DiskInfo
    {
        public string Name { get; set; }
        public string DriveType { get; set; }
        public string TotalSizeGB { get; set; }
        public string FreeSpaceGB { get; set; }
        public string UsedSpaceGB { get; set; }
        public double FreePercent { get; set; }
        public string RootDirectory { get; set; }
    }
}
