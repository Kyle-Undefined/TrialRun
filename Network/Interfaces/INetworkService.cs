using Network.Models;

namespace Network.Interfaces
{
    public interface INetworkService
    {
        Task<Client> GetClientAsync(string networkRoot, string networkFolder, string clientPath, string clientCode);

        Task<NetworkSetting> GetNetworkSettingAsync(string networkRoot, string networkFolder, string networkSettingsPath);

        Task<bool> IsNetworkPresentAsync(string networkRoot, string networkFolder);
    }
}