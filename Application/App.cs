using Application.Configs;
using Application.Models;
using Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Network.Interfaces;
using Network.Models;
using Network.Services;
using Scripting;
using Scripting.Interfaces;
using Scripting.Services;
using Serilog;
using Serilog.Events;
using System.Text;
using Updater.Interfaces;
using Updater.Services;

namespace Application
{
    public static class App
    {
        private static AppSetting _appSettings = new();
        private static ApplicationDbContext? _context;
        private static IHost _host = Host.CreateDefaultBuilder().Build();
        private static ILogger? _logger;
        private static NetworkSetting _networkSetting = new();
        public static bool Error { get; set; }
        public static bool Initialized { get; set; }

        public static async Task<bool> CreateAsync(string clientCode, string trialName)
        {
            NetworkService network = ActivatorUtilities.GetServiceOrCreateInstance<NetworkService>(_host.Services);
            var client = await network.GetClientAsync(_appSettings.NetworkRoot!, _appSettings.NetworkFolder!, _networkSetting.ClientCodePath!, clientCode);

            if (client is not null)
            {
                var hdFileName = Guid.NewGuid().ToString().Replace("-", string.Empty)[..10];
                PowerShellService powershell = ActivatorUtilities.GetServiceOrCreateInstance<PowerShellService>(_host.Services);
                PowerShellParameter[] parameters = [new PowerShellParameter { Name = "Name", Value = trialName }, new PowerShellParameter { Name = "VMPath", Value = $@"\\{_appSettings.NetworkRoot!}\{_appSettings.NetworkFolder!}\{client.VirtualMachine}" }, new PowerShellParameter { Name = "HDPath", Value = $@"{Environment.CurrentDirectory}\{hdFileName}\" }];
                var importResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\Hyper-V\Import.ps1", parameters);

                if (importResult.Error)
                {
                    Error = true;
                    LogError("Application: CreateAsync - Error: ", importResult.ErrorMessage);
                    throw new NotSupportedException(importResult.ErrorMessage);
                }

                var dataResult = await AddTrialAsync(clientCode, trialName, hdFileName, importResult.Output);

                //RBS Spin Up
                parameters = [new PowerShellParameter { Name = "Name", Value = trialName + "RBS" }, new PowerShellParameter { Name = "BakPath", Value = $@"\\{_appSettings.NetworkRoot!}\{_appSettings.NetworkFolder!}\{client.RbsDatabase}" }];
                var rbsDbResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\SQL\Restore.ps1", parameters);

                if (rbsDbResult.Error)
                {
                    Error = true;
                    LogError("Application: CreateAsync - Error: ", rbsDbResult.ErrorMessage);
                    throw new NotSupportedException(rbsDbResult.ErrorMessage);
                }

                //RCS Spin Up
                parameters = [new PowerShellParameter { Name = "Name", Value = trialName + "RCS" }, new PowerShellParameter { Name = "BakPath", Value = $@"\\{_appSettings.NetworkRoot!}\{_appSettings.NetworkFolder!}\{client.RcsDatabase}" }];
                var rcsDbResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\SQL\Restore.ps1", parameters);

                if (rcsDbResult.Error)
                {
                    Error = true;
                    LogError("Application: CreateAsync - Error: ", rcsDbResult.ErrorMessage);
                    throw new NotSupportedException(rcsDbResult.ErrorMessage);
                }

                return dataResult && !importResult.Error && !rbsDbResult.Error && !rcsDbResult.Error;
            }
            else
            {
                return false;
            }
        }

        public static async Task DeleteAsync(string stringId)
        {
            int id = Convert.ToInt32(stringId);
            var trial = await GetTrialAsync(id);

            if (trial is not null)
            {
                PowerShellService powershell = ActivatorUtilities.GetServiceOrCreateInstance<PowerShellService>(_host.Services);
                PowerShellParameter[] parameters = [new PowerShellParameter { Name = "Id", Value = trial.VmId }];
                PowerShellResult turnoffResult;

                try
                {
                    turnoffResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\Hyper-V\TurnOff.ps1", parameters);
                }
                catch
                {
                    turnoffResult = new();
                }

                if (turnoffResult.Error)
                {
                    Error = true;
                    LogError("Application: RemoveTrialAsync - Error: ", turnoffResult.ErrorMessage);
                    throw new NotSupportedException(turnoffResult.ErrorMessage);
                }

                Directory.Delete($@"{Environment.CurrentDirectory}\{trial.HDFolderName}", true);

                //RBS Spin Down
                parameters = [new PowerShellParameter { Name = "Name", Value = $"{trial.Name}RBS" }];
                var rbsDbResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\SQL\Drop.ps1", parameters);

                if (rbsDbResult.Error)
                {
                    Error = true;
                    LogError("Application: CreateAsync - Error: ", rbsDbResult.ErrorMessage);
                    throw new NotSupportedException(rbsDbResult.ErrorMessage);
                }

                //RCS Spin Down
                parameters = [new PowerShellParameter { Name = "Name", Value = $"{trial.Name}RCS" }];
                var rcsDbResult = await powershell.ExecuteFileScalarAsync($@"{Environment.CurrentDirectory}\SQL\Drop.ps1", parameters);

                if (rcsDbResult.Error)
                {
                    Error = true;
                    LogError("Application: CreateAsync - Error: ", rcsDbResult.ErrorMessage);
                    throw new NotSupportedException(rcsDbResult.ErrorMessage);
                }

                await RemoveTrialAsync(id);
            }
        }

        public static async Task InitializeAsync()
        {
            try
            {
                _host = CreateHost();
                _logger = _host.Services.GetRequiredService<ILogger>();
                _context = _host.Services.GetRequiredService<ApplicationDbContext>();

                await _context.Database.EnsureCreatedAsync();

                var tasks = new List<Task>
                {
                    CheckVirtualizationAsync(),
                    CheckSQLAsync(),
                    GetAppSettingsAsync()
                };

                await Task.WhenAll(tasks);
                await GetNetworkSettingsAsync();
                await GetUpdateAsync();

                Initialized = true;
            }
            catch (Exception ex)
            {
                Error = true;
                LogError("Application: InitializeAsync - Error: ", ex.Message);
            }
        }

        public static async Task<string> ListTrialsAsync()
        {
            var str = new StringBuilder();
            str.AppendLine("Trials: ");

            if (_context is not null)
            {
                var trials = _context.Trials;

                foreach (var trial in trials)
                {
                    str.AppendLine($"{trial.Id} | {trial.Name} | {trial.ClientCode}");
                }
            }

            if (str.Length <= 10)
            {
                str.AppendLine("No Trials saved");
            }

            return await Task.FromResult(str.ToString());
        }

        private static async Task<bool> AddTrialAsync(string clientCode, string trialName, string hdFolderName, string vmId)
        {
            try
            {
                if (_context is not null)
                {
                    Trial trial = new()
                    {
                        ClientCode = clientCode,
                        HDFolderName = hdFolderName,
                        Name = trialName,
                        VmId = vmId
                    };

                    _ = await _context.AddAsync(trial);
                    return (await _context.SaveChangesAsync() == 1);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogError("Application: AddTrialAsync - Error: ", ex.Message);
                throw new NotSupportedException("Application: AddTrialAsync - Error: " + ex.Message);
            }
        }

        private static async Task CheckSQLAsync()
        {
            PowerShellService service = ActivatorUtilities.GetServiceOrCreateInstance<PowerShellService>(_host.Services);
            var result = await service.ExecuteScriptScalarAsync(@"Write-Host (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server').InstalledInstances");

            if (result.Error)
            {
                Error = true;
                LogError("Application: CheckSQLAsync - Error: ", result.ErrorMessage);
                throw new NotSupportedException("Application: CheckSQLAsync - Error: " + result.ErrorMessage);
            }
            else
            {
                if (!result.Output.Equals("MSSQLServer", StringComparison.CurrentCultureIgnoreCase))
                {
                    Error = true;
                    LogError("Application: CheckSQLAsync - Output: ", result.Output);
                    throw new NotSupportedException("Application: CheckSQLAsync - Output: " + result.Output);
                }
            }
        }

        private static async Task CheckVirtualizationAsync()
        {
            PowerShellService service = ActivatorUtilities.GetServiceOrCreateInstance<PowerShellService>(_host.Services);
            var result = await service.ExecuteScriptScalarAsync("Write-Host (Get-WindowsOptionalFeature -FeatureName Microsoft-Hyper-V-All -Online).State");

            if (result.Error)
            {
                Error = true;
                LogError("Application: CheckVirtualizationAsync - Error: ", result.ErrorMessage);
                throw new NotSupportedException("Application: CheckVirtualizationAsync - Error: " + result.ErrorMessage);
            }
            else
            {
                if (!result.Output.Equals("Enabled", StringComparison.CurrentCultureIgnoreCase))
                {
                    Error = true;
                    LogError("Application: CheckVirtualizationAsync - Output: ", result.Output);
                    throw new NotSupportedException("Application: CheckVirtualizationAsync - Output: " + result.Output);
                }
            }
        }

        private static IHost CreateHost() => Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.ConfigureOptions<AppSettingConfig>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<IUpdaterService, UpdaterService>();
            services.AddTransient<IPowerShellService, PowerShellService>();
            services.AddDbContext<ApplicationDbContext>();
        }).UseConsoleLifetime().UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.File($"{AppContext.BaseDirectory}/Logs/log-{DateTimeOffset.UtcNow:yyyy-MM-dd}.txt", restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.File($"{AppContext.BaseDirectory}/Logs/log-{DateTimeOffset.UtcNow:yyyy-MM-dd}-verbose.txt");
        }).Build();

        private static async Task GetAppSettingsAsync()
        {
            await Task.Run(() =>
            {
                _appSettings = _host.Services.GetRequiredService<IOptions<AppSetting>>().Value;

                if (string.IsNullOrWhiteSpace(_appSettings.NetworkRoot))
                {
                    Error = true;
                    LogError("Application: GetAppSettingsAsync - Error: Network Root Is Empty");
                    throw new ArgumentException("Application: GetAppSettingsAsync - Error: Network Root Is Empty");
                }

                if (string.IsNullOrWhiteSpace(_appSettings.NetworkFolder))
                {
                    Error = true;
                    LogError("Application: GetAppSettingsAsync - Error: Network Folder Is Empty");
                    throw new ArgumentException("Application: GetAppSettingsAsync - Error: Network Folder Is Empty");
                }

                if (string.IsNullOrWhiteSpace(_appSettings.NetworkSettingsPath))
                {
                    Error = true;
                    LogError("Application: GetAppSettingsAsync - Error: Network Settings Path Is Empty");
                    throw new ArgumentException("Application: GetAppSettingsAsync - Error: Network Settings Path Is Empty");
                }

                if (string.IsNullOrWhiteSpace(_appSettings.ConnectionString))
                {
                    Error = true;
                    LogError("Application: GetAppSettingsAsync - Error: Connection String Is Empty");
                    throw new ArgumentException("Application: GetAppSettingsAsync - Error: Connection String Is Empty");
                }
            });
        }

        private static async Task GetNetworkSettingsAsync()
        {
            NetworkService service = ActivatorUtilities.GetServiceOrCreateInstance<NetworkService>(_host.Services);

            var present = await service.IsNetworkPresentAsync(_appSettings.NetworkRoot!, _appSettings.NetworkFolder!);

            if (!present)
            {
                Error = true;
                LogError("Application: GetNetworkSettingsAsync - Error: Network Is Not Present");
                throw new AccessViolationException("Application: GetNetworkSettingsAsync - Error: Network Is Not Present");
            }

            var networkSetting = await service.GetNetworkSettingAsync(_appSettings.NetworkRoot!, _appSettings.NetworkFolder!, _appSettings.NetworkSettingsPath!);

            if (networkSetting is null)
            {
                Error = true;
                LogError("Application: GetNetworkSettingsAsync - Error: Network Settings Are Not Present");
                throw new ArgumentException("Application: GetNetworkSettingsAsync - Error: Network Settings Are Not Present");
            }

            if (string.IsNullOrWhiteSpace(networkSetting.ClientCodePath))
            {
                Error = true;
                LogError("Application: GetNetworkSettingsAsync - Error: Client Code Path Is Empty");
                throw new ArgumentException("Application: GetNetworkSettingsAsync - Error: Client Code Path Is Empty");
            }

            if (string.IsNullOrWhiteSpace(networkSetting.DocumentPath))
            {
                Error = true;
                LogError("Application: GetNetworkSettingsAsync - Error: Document Path Is Empty");
                throw new ArgumentException("Application: GetNetworkSettingsAsync - Error: Document Path Is Empty");
            }

            if (string.IsNullOrWhiteSpace(networkSetting.ReleasePath))
            {
                Error = true;
                LogError("Application: GetNetworkSettingsAsync - Error: Release Path Is Empty");
                throw new ArgumentException("Application: GetNetworkSettingsAsync - Error: Release Path Is Empty");
            }

            _networkSetting = networkSetting;
        }

        private static async Task<Trial> GetTrialAsync(int id)
        {
            try
            {
                if (_context is not null)
                {
                    var trial = await _context.Trials.FindAsync(id);

                    if (trial is not null)
                    {
                        return trial;
                    }

                    return new();
                }
                else
                {
                    return new();
                }
            }
            catch (Exception ex)
            {
                LogError("Application: GetTrialAsync - Error: ", ex.Message);
                throw new NotSupportedException("Application: GetTrialAsync - Error: " + ex.Message);
            }
        }

        private static async Task GetUpdateAsync()
        {
            UpdaterService service = ActivatorUtilities.GetServiceOrCreateInstance<UpdaterService>(_host.Services);
            var path = @$"\\{_appSettings.NetworkRoot}\{_appSettings.NetworkFolder}\{_networkSetting.ReleasePath}\";

            await service.InitializeAsync(path);

            if (service.Updated)
            {
                LogWarning("Application: GetUpdateAsync - Program Updated. Please Restart.");
            }
        }

        private static void LogError(string messageTemplate)
        {
            _logger?.Error(messageTemplate);
        }

        private static void LogError(string messageTemplate, string propertyValue)
        {
            _logger?.Error(messageTemplate, propertyValue);
        }

        private static void LogWarning(string messageTemplate)
        {
            _logger?.Warning(messageTemplate);
        }

        private static async Task<bool> RemoveTrialAsync(int id)
        {
            try
            {
                if (_context is not null)
                {
                    var trial = await _context.Trials.FindAsync(id);

                    if (trial is not null)
                    {
                        _context.Remove(trial);
                        return (await _context.SaveChangesAsync() == 1);
                    }

                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError("Application: RemoveTrialAsync - Error: ", ex.Message);
                throw new NotSupportedException("Application: RemoveTrialAsync - Error: " + ex.Message);
            }
        }
    }
}