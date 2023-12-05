namespace Scripting.Interfaces
{
    public interface IPowerShellService
    {
        Task<PowerShellResult> ExecuteFileScalarAsync(string file, PowerShellParameter[] parameters);

        Task ExecuteScriptAsync(string script);

        Task<PowerShellResult> ExecuteScriptScalarAsync(string script);
    }
}