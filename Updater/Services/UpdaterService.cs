using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Tomlyn;
using Updater.Interfaces;
using Updater.Models;

namespace Updater.Services
{
    public partial class UpdaterService(ILogger<UpdaterService> logger) : IUpdaterService
    {
        public bool Updated { get; set; } = false;

        public async Task InitializeAsync(string path)
        {
            logger.LogInformation("UpdaterService: InitializeAsync - Started");
            logger.LogInformation("UpdaterService: InitializeAsync - Cleaning Files");

            await CleanAsync();

            logger.LogInformation("UpdaterService: InitializeAsync - Path: '{Path}'", path);

            bool exists = await Task.Run(() => new FileInfo(path + "Version.toml").Exists);
            UpdaterSetting? updaterSetting = null;

            try
            {
                if (exists)
                {
                    logger.LogInformation("UpdaterService: InitializeAsync - Version File Exists");
                    logger.LogInformation("UpdaterService: InitializeAsync - Reading File");

                    var tomlString = await File.ReadAllTextAsync(path + "Version.toml");

                    logger.LogInformation("UpdaterService: InitializeAsync - Converting TOML String To Model");

                    updaterSetting = Toml.ToModel<UpdaterSetting>(tomlString);
                }
                else
                {
                    logger.LogWarning("UpdaterService: InitializeAsync - Version File Does Not Exist");
                }

                if (updaterSetting is not null)
                {
                    logger.LogInformation("UpdaterService: InitializeAsync - Updater Settings Retrieved");
                    logger.LogInformation("UpdaterService: InitializeAsync - Getting Current Version");

                    var version = await GetCurrentVersionAsync();

                    logger.LogInformation("UpdaterService: InitializeAsync - Checking Latest Version");

                    if (!version.Equals(updaterSetting.Version, StringComparison.CurrentCultureIgnoreCase))
                    {
                        logger.LogInformation("UpdaterService: InitializeAsync - Newer Version Available");

                        await UpdateAsync(path, version);
                    }
                    else
                    {
                        logger.LogInformation("UpdaterService: InitializeAsync - Running Latest Version");
                    }
                }
                else
                {
                    logger.LogWarning("UpdaterService: InitializeAsync - Updater Settings Not Retrieved");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("UpdaterService: InitializeAsync - Error: {Message}", ex.Message);
            }

            logger.LogInformation("UpdaterService: InitializeAsync - Finished");
        }

        [GeneratedRegex(@"\.old.*")]
        private static partial Regex OldVersionRegex();

        private async Task CleanAsync()
        {
            logger.LogInformation("UpdaterService: CleanAsync - Started");

            await Task.Run(() =>
            {
                var localPath = Path.GetDirectoryName(GetType().Assembly.Location);

                foreach (var file in Directory.EnumerateFiles(localPath!).Where(x => OldVersionRegex().Match(x).Success))
                {
                    try
                    {
                        logger.LogInformation("UpdaterService: CleanAsync - Deleting Old File: {File}", Path.GetFileName(file));
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("UpdaterService: CleanAsync - Error: {Message}", ex.Message);
                    }
                }
            });

            logger.LogInformation("UpdaterService: CleanAsync - Finished");
        }

        private async Task<string> GetCurrentVersionAsync()
        {
            logger.LogInformation("UpdaterService: GetCurrentVersionAsync - Started");

            var version = await Task.Run(() => GetType().Assembly.GetName().Version!.ToString());

            logger.LogInformation("UpdaterService: GetCurrentVersionAsync - Version: '{Version}'", version);
            logger.LogInformation("UpdaterService: GetCurrentVersionAsync - Finished");

            return version;
        }

        private async Task UpdateAsync(string path, string version)
        {
            logger.LogInformation("UpdaterService: UpdateAsync - Started");

            var localPath = Path.GetDirectoryName(GetType().Assembly.Location);
            var updates = await Task.Run(() => new DirectoryInfo(path).GetDirectories());

            logger.LogInformation("UpdaterService: UpdateAsync - Update Count: {Count}", updates.Length);

            var missedUpdates = updates.Where(x => version.CompareTo(x.Name) < 0);

            logger.LogInformation("UpdaterService: UpdateAsync - Missed Update Count: {Count}", missedUpdates.Count());

            await Task.Run(() =>
            {
                foreach (var update in missedUpdates)
                {
                    logger.LogInformation("UpdaterService: UpdateAsync - Updating To Version: {Update}", update.Name);
                    logger.LogInformation("UpdaterService: UpdateAsync - Getting Files");

                    foreach (var file in update.GetFiles())
                    {
                        var localFile = $@"{localPath}\{file.Name}";

                        try
                        {
                            if (File.Exists(localFile))
                            {
                                File.Move(localFile, localFile + ".old" + file.Extension, true);
                                file.CopyTo(localFile, true);
                            }
                            else
                            {
                                file.CopyTo(localFile, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError("UpdaterService: UpdateAsync - Error: {Message}", ex.Message);
                        }
                    }

                    logger.LogInformation("UpdaterService: UpdateAsync - Files Updated");
                    logger.LogInformation("UpdaterService: UpdateAsync - Getting Folders");

                    foreach (var directory in update.GetDirectories())
                    {
                        var localDirectory = $@"{localPath}\{directory.Name}";

                        if (!Directory.Exists(localDirectory))
                        {
                            Directory.CreateDirectory(localDirectory);
                        }

                        foreach (var file in directory.GetFiles())
                        {
                            file.CopyTo($@"{localDirectory}\{file.Name}", true);
                        }
                    }

                    logger.LogInformation("UpdaterService: UpdateAsync - Folders Updated");
                }

                Updated = true;
            });

            logger.LogInformation("UpdaterService: UpdateAsync - Finished");
        }
    }
}