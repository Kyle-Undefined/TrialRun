namespace Network.Models
{
    public class NetworkSetting : BaseEntity
    {
        public string? ClientCodePath { get; set; }
        public string? DocumentPath { get; set; }
        public string? ReleasePath { get; set; }
    }
}