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
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace FileCompressionCopy
{
    public class FileCompressionCopyScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private ILogger logger                 { get; set; }
        private IFileSystem FileSystem         { get; }
        private ILogManager LogManager         { get; }
        private ISessionManager SessionManager { get; }

        // ReSharper disable once TooManyDependencies
        public FileCompressionCopyScheduledTask(IFileSystem file, ILogManager logManager, ISessionManager sesMan)
        {
            FileSystem     = file;
            LogManager     = logManager;
            SessionManager = sesMan;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var config = Plugin.Instance.Configuration;

            if (config.MonitoredFolder is null || config.EmbyAutoOrganizeFolderPath is null) return;

            logger = LogManager.GetLogger(Plugin.Instance.Name);

            var monitoredDirectoryInfo     = FileSystem.GetDirectories(path: config.MonitoredFolder);

            var monitoredDirectoryContents = monitoredDirectoryInfo.ToList();
            

            logger.Info("Found: " + monitoredDirectoryContents.Count() + " folders in " + config.MonitoredFolder);
            

            foreach (var mediaFolder in monitoredDirectoryContents)
            {
                //Ignore this directory if there is an 'extraction marker' file present because we have already extracted the contents of this folder.
                if (FileSystem.FileExists(mediaFolder.FullName + "\\####emby.extracted####")) continue;

                logger.Info("New media folder: " + mediaFolder.FullName);
                
                CreateExtractionMarker(mediaFolder.FullName, logger);

                var newMediaFiles = FileSystem.GetFiles(mediaFolder.FullName);

                foreach (var file in newMediaFiles)
                {
                    if (file.FullName.IndexOf("sample", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    logger.Info("File checked: " + file.Name);

                    switch (file.Extension)
                    {
                        case ".rar":

                            logger.Info("Found new compressed file to extract: " + file.Name);
                            await Task.Run(
                                () => UnzipAndCopyFiles.BeginCompressedFileExtraction(file.FullName, file.Name, logger,
                                    progress, config, SessionManager), cancellationToken);

                            config.CompletedItems.Add(new ExtractionInfo
                            {
                                Name            = mediaFolder.Name,
                                completed       = DateTime.Now,
                                size            = FileSizeConversions.SizeSuffix(file.Length),
                                extension       = file.Extension,
                                CopyType        = "Unpacked"
                            });
                            break;

                        case ".mkv":
                        case ".avi":
                        case ".mp4":

                            logger.Info("Found new file to copy: " + file.Name);
                            await Task.Run(
                                () => CopyFiles.BeginFileCopy(file.FullName, file.Name, progress,
                                    Plugin.Instance.Configuration, SessionManager), cancellationToken);

                            config.CompletedItems.Add(new ExtractionInfo
                            {
                                Name            = mediaFolder.Name,
                                completed       = DateTime.Now,
                                size            = FileSizeConversions.SizeSuffix(file.Length),
                                extension       = file.Extension,
                                CopyType        = "Copied"
                            });
                            break;
                    }
                }


                Plugin.Instance.UpdateConfiguration(new PluginConfiguration
                {
                    EmbyAutoOrganizeFolderPath = config.EmbyAutoOrganizeFolderPath,
                    MonitoredFolder            = config.MonitoredFolder,
                    CompletedItems             = config.CompletedItems.Where(i => i.completed > DateTime.Now.AddDays(-30)).ToList() //No need to keep a list of items that are 30 days old
                });
            }
            
            progress.Report(100);
            
        }
        
        private static void CreateExtractionMarker(string folderPath, ILogger logger)
        {
            logger.Info("Creating extraction marker " + folderPath + "\\####emby.extracted####");
            using (var sr = new StreamWriter(folderPath + "\\####emby.extracted####")) { sr.Flush(); }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                // Every so often
                new TaskTriggerInfo
                {
                    Type          = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromMinutes(5).Ticks
                }
            };
        }

        public string Name        => "Decompression and copy media files";
        public string Description => "Unzip or Copy new files available in the configured watch folder into Emby's Auto Organize folder.";
        public string Category    => "Library";
        public string Key         => "FileCompressionCopy";
        public bool IsHidden      => false;
        public bool IsEnabled     => true;
        public bool IsLogged      => true;
    }
}