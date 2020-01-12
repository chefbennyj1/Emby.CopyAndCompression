using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileCompressionCopy.Configuration;
using FileCompressionCopy.Helpers;
using FileCompressionCopy.OrganizeFiles;
using FileCompressionCopy.OrganizeFiles.Copy;
using FileCompressionCopy.OrganizeFiles.UnzipCopy;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace FileCompressionCopy
{
    public class FileCompressionCopyScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private ILogger logger { get; set; }
        private IFileSystem FileSystem { get; set; }
        private ILogManager LogManager { get; set; }
        private ITaskManager TaskManager { get; set; }

        // ReSharper disable once TooManyDependencies
        public FileCompressionCopyScheduledTask(IFileSystem file, ILogManager logManager, ITaskManager taskMan)
        {
            FileSystem = file;
            LogManager = logManager;
            TaskManager = taskMan;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var config = Plugin.Instance.Configuration;

            if (Equals(config.MonitoredFolder, null) || Equals(config.EmbyAutoOrganizeFolderPath, null)) return;

            logger = LogManager.GetLogger(Plugin.Instance.Name);

            var directoryInfo = FileSystem.GetDirectories(path: config.MonitoredFolder);

            var directoryInfoList = directoryInfo.ToList();

            logger.Info("Found: " + directoryInfoList.Count() + " folders in " + config.MonitoredFolder);

            foreach (var newMediaFolder in directoryInfoList)
            {
                if (FileSystem.FileExists(newMediaFolder.FullName + "\\####emby.extracted####")) continue;

                logger.Info("New media file: " + newMediaFolder.FullName);

                logger.Info("Creating compression marker " + newMediaFolder.FullName + "\\####emby.extracted####");

                using (var sr = new StreamWriter(newMediaFolder.FullName + "\\####emby.extracted####"))
                {
                    sr.Flush();
                }

                var newMediaFiles = FileSystem.GetFiles(newMediaFolder.FullName);

                foreach (var file in newMediaFiles)
                {
                    if (file.FullName.IndexOf("sample", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    logger.Info("File checked: " + file.Name);

                    switch (file.Extension)
                    {
                        case ".rar":
                            logger.Info("Found new compressed file to decompress: " + file.Name);
                            await Task.Run(
                                () => UnzipAndCopyFiles.BeginDecompressionAndCopy(file.FullName, file.Name, logger,
                                    progress, config), cancellationToken);

                            config.CompletedItems.Add(new ExtractionInfo
                            {
                                Name = newMediaFolder.Name,
                                completed = DateTime.Now.ToString("yyyy-M-dd--HH:mm-ss"),
                                size = FileSizeConversions.SizeSuffix(file.Length),
                                extention = file.Extension,
                                CreationTimeUTC = file.CreationTimeUtc,
                                CopyType = "Unpacked"
                            });
                            break;

                        // ReSharper disable RedundantCaseLabel
                        case ".mkv":
                        case ".avi":
                        case ".mp4":

                            logger.Info("Found New File to Copy: " + file.Name);
                            await Task.Run(
                                () => CopyFiles.BeginCopy(file.FullName, file.Name, progress,
                                    Plugin.Instance.Configuration), cancellationToken);

                            config.CompletedItems.Add(new ExtractionInfo
                            {
                                Name = newMediaFolder.Name,
                                completed = DateTime.Now.ToString("yyyy-M-dd--HH:mm-ss"),
                                size = FileSizeConversions.SizeSuffix(file.Length),
                                extention = file.Extension,
                                CreationTimeUTC = file.CreationTimeUtc,
                                CopyType = "Copied"
                            });
                            break;
                    }
                }


                Plugin.Instance.UpdateConfiguration(new PluginConfiguration
                {
                    EmbyAutoOrganizeFolderPath = config.EmbyAutoOrganizeFolderPath,
                    MonitoredFolder = config.MonitoredFolder,
                    CompletedItems = config.CompletedItems
                });
            }

            progress.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                // Every so often
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromMinutes(5).Ticks
                }
            };
        }

        public string Name => "Decompression and copy media files";

        public string Description =>
            "Unzip or Copy new files available in the configured watch folder into Emby's Auto Organize folder.";

        public string Category => "Library";

        public string Key => "FileCompressionCopy";

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
    }
}