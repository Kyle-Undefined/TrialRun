namespace Updater.Models
{
    internal class UpdaterSetting : BaseEntity
    {
        public string? InstallerVersion { get; set; }
        public string? Version { get; set; }
    }
}