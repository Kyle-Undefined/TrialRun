namespace Application.Models
{
    public class AppSetting : BaseEntity
    {
        public string? ConnectionString { get; set; }
        public string? NetworkFolder { get; set; }
        public string? NetworkRoot { get; set; }
        public string? NetworkSettingsPath { get; set; }
    }
}