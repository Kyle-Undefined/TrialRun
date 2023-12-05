using Microsoft.Extensions.Logging;
using Network.Interfaces;
using Network.Models;
using Tomlyn;

namespace Network.Services
{
    public class NetworkService(ILogger<NetworkService> logger) : INetworkService
    {
        public async Task<Client> GetClientAsync(string networkRoot, string networkFolder, string clientPath, string clientCode)
        {
            logger.LogInformation("NetworkService: GetClientAsync - Started");

            var path = $@"\\{networkRoot}\{networkFolder}\{clientPath}\";

            logger.LogInformation(@"NetworkService: GetClientAsync - Path: '\\{Root}\{Folder}\{Path}\'", networkRoot, networkFolder, clientPath);

            bool exists = await Task.Run(() => new FileInfo(path + clientCode + ".toml").Exists);
            Client? client = null;

            try
            {
                if (exists)
                {
                    logger.LogInformation("NetworkService: GetClientAsync - File Exists");
                    logger.LogInformation("NetworkService: GetClientAsync - Reading File");

                    var tomlString = await File.ReadAllTextAsync(path + clientCode + ".toml");

                    logger.LogInformation("NetworkService: GetClientAsync - Converting TOML String To Model");

                    client = Toml.ToModel<Client>(tomlString);
                }
                else
                {
                    logger.LogWarning("NetworkService: GetClientAsync - File Does Not Exist");
                }

                logger.LogInformation("NetworkService: GetClientAsync - Finished");
                return client!;
            }
            catch (Exception ex)
            {
                logger.LogError("NetworkService: GetClientAsync - Error: {Message}", ex.Message);
                return client!;
            }
        }

        public async Task<NetworkSetting> GetNetworkSettingAsync(string networkRoot, string networkFolder, string networkSettingsPath)
        {
            logger.LogInformation("NetworkService: GetNetworkSettingAsync - Started");

            var path = $@"\\{networkRoot}\{networkFolder}\{networkSettingsPath}\";

            logger.LogInformation(@"NetworkService: GetNetworkSettingAsync - Path: '\\{Root}\{Folder}\{Path}\'", networkRoot, networkFolder, networkSettingsPath);

            bool exists = await Task.Run(() => new FileInfo(path + "Settings.toml").Exists);
            NetworkSetting? networkSetting = null;

            try
            {
                if (exists)
                {
                    logger.LogInformation("NetworkService: GetNetworkSettingAsync - File Exists");
                    logger.LogInformation("NetworkService: GetNetworkSettingAsync - Reading File");

                    var tomlString = await File.ReadAllTextAsync(path + "Settings.toml");

                    logger.LogInformation("NetworkService: GetNetworkSettingAsync - Converting TOML String To Model");

                    networkSetting = Toml.ToModel<NetworkSetting>(tomlString);
                }
                else
                {
                    logger.LogWarning("NetworkService: GetNetworkSettingAsync - File Does Not Exist");
                }

                logger.LogInformation("NetworkService: GetNetworkSettingAsync - Finished");
                return networkSetting!;
            }
            catch (Exception ex)
            {
                logger.LogError("NetworkService: GetNetworkSettingAsync - Error: {Message}", ex.Message);
                return networkSetting!;
            }
        }

        public async Task<bool> IsNetworkPresentAsync(string networkRoot, string networkFolder)
        {
            logger.LogInformation("NetworkService: IsNetworkPresentAsync - Started");
            logger.LogInformation(@"NetworkService: IsNetworkPresentAsync - Path: '\\{Root}\{Folder}'", networkRoot, networkFolder);

            try
            {
                bool exists = await Task.Run(() => Directory.Exists($@"\\{networkRoot}\{networkFolder}"));

                if (exists)
                {
                    logger.LogInformation("NetworkService: IsNetworkPresentAsync - Directory Exists");
                }
                else
                {
                    logger.LogWarning("NetworkService: IsNetworkPresentAsync - Directory Does Not Exist");
                }

                logger.LogInformation("NetworkService: IsNetworkPresentAsync - Finished");

                return exists;
            }
            catch (Exception ex)
            {
                logger.LogError("NetworkService: IsNetworkPresentAsync - Error: {Message}", ex.Message);
                return false;
            }
        }
    }
}