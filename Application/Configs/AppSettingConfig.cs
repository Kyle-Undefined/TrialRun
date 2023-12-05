using Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Application.Configs
{
    internal class AppSettingConfig(IConfiguration configuration) : IConfigureOptions<AppSetting>
    {
        private const string SectionName = nameof(AppSetting);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public void Configure(AppSetting options)
        {
            configuration.GetSection(SectionName).Bind(options);
        }
    }
}