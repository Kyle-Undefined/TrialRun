using Microsoft.Extensions.Logging;
using Scripting.Interfaces;
using System.Management.Automation;

namespace Scripting.Services
{
    public class PowerShellService(ILogger<PowerShellService> logger) : IPowerShellService
    {
        public async Task<PowerShellResult> ExecuteFileScalarAsync(string file, PowerShellParameter[] parameters)
        {
            PowerShellResult result = new();

            try
            {
                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Started");
                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - File: '{File}'", file);

                using var ps = PowerShell.Create();

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Created PowerShell Instance");
                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Set Execution Policy");

                await ps.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("Scope", "Process")
                    .AddParameter("ExecutionPolicy", "Bypass")
                    .InvokeAsync();

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Reading File");

                var script = await File.ReadAllTextAsync(file);

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Adding Script");

                ps.AddScript(script);

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Adding Parameters");

                foreach (var parameter in parameters)
                {
                    ps.AddParameter(parameter.Name, parameter.Value);
                }

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Running");

                await ps.InvokeAsync();

                if (ps.Streams.Error.Count != 0)
                {
                    logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Getting Errors");

                    result.Error = true;
                    result.ErrorMessage = ps.Streams.Error.FirstOrDefault()!.ToString();
                }

                if (ps.Streams.Information.Count != 0)
                {
                    logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Getting Output");

                    result.Output = ps.Streams.Information.FirstOrDefault()!.ToString();
                }

                logger.LogInformation("PowerShellService: ExecuteFileScalarAsync - Finished");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError("PowerShellService: ExecuteFileScalarAsync - Error: {Message}", ex.Message);
                throw new PSNotImplementedException(file, ex);
            }
        }

        public async Task ExecuteScriptAsync(string script)
        {
            try
            {
                logger.LogInformation("PowerShellService: ExecuteScriptAsync - Started");

                await ExecuteScriptScalarAsync(script);

                logger.LogInformation("PowerShellService: ExecuteScriptAsync - Finished");
            }
            catch (Exception ex)
            {
                logger.LogError("PowerShellService: ExecuteScriptAsync - Error: {Message}", ex.Message);
                throw new PSNotImplementedException(script, ex);
            }
        }

        public async Task<PowerShellResult> ExecuteScriptScalarAsync(string script)
        {
            PowerShellResult result = new();

            try
            {
                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Started");
                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Script: '{Script}'", script);

                using var ps = PowerShell.Create();

                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Created PowerShell Instance");
                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Set Execution Policy");

                await ps.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("Scope", "Process")
                    .AddParameter("ExecutionPolicy", "Bypass")
                    .InvokeAsync();

                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Adding Script");

                await ps.AddScript(script)
                    .InvokeAsync();

                if (ps.Streams.Error.Count != 0)
                {
                    logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Getting Errors");

                    result.Error = true;
                    result.ErrorMessage = ps.Streams.Error.FirstOrDefault()!.ToString();
                }

                if (ps.Streams.Information.Count != 0)
                {
                    logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Getting Output");

                    result.Output = ps.Streams.Information.FirstOrDefault()!.ToString();
                }

                logger.LogInformation("PowerShellService: ExecuteScriptScalarAsync - Finished");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError("PowerShellService: ExecuteScriptScalarAsync - Error: {Message}", ex.Message);
                throw new PSNotImplementedException(script, ex);
            }
        }
    }
}