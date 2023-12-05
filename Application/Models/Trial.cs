namespace Application.Models
{
    public class Trial : BaseEntity
    {
        public string ClientCode { get; set; } = string.Empty;
        public string HDFolderName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VmId { get; set; } = string.Empty;
    }
}