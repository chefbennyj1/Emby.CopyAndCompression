﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using FileCompressionCopy.Configuration;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;

// ReSharper disable TooManyArguments
namespace FileCompressionCopy.OrganizeFiles.UnzipCopy
{
    
    public class UnzipAndCopyFiles
    {
        private static long totalSize                 { get; set; }
        private static ExtractionInfo CurrentObjective { get; set; }
        private static IProgress<double> Progress     { get; set; }
        private static ISessionManager SessionManager { get; set; }

        public static void BeginCompressedFileExtraction(string fullFileName, string fileName, ILogger log, IProgress<double> prog, PluginConfiguration config, ISessionManager sesMan)
        {

            Progress       = prog;
            SessionManager = sesMan;

            log.Info("Found New RAR File to Decompress: " + fileName);

            CurrentObjective = new ExtractionInfo { Name = Path.GetFileNameWithoutExtension(fileName) };

            string extractPath = $"{config.EmbyAutoOrganizeFolderPath}\\{Path.GetFileNameWithoutExtension(fileName)}";

            log.Info("Creating Extraction Path: " + extractPath);

            Directory.CreateDirectory(extractPath);

            var archive = ArchiveFactory.Open(fullFileName);

            log.Info("Archive open: " + fullFileName);

            // Calculate the total extraction size.
            totalSize = archive.TotalSize;
            log.Info("Archive Total Size: " + totalSize);

            foreach (IArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                archive.EntryExtractionEnd  += FileMoveSuccess;
                archive.CompressedBytesRead += Archive_CompressedBytesRead;

                entry.WriteToDirectory(extractPath, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite       = true
                });
            }
        }

        private static void Archive_CompressedBytesRead(object sender, CompressedBytesReadEventArgs e)
        {
            long compressedBytesRead = e.CompressedBytesRead;
            double compressedPercent = (compressedBytesRead / (double)totalSize) * 100;

            CurrentObjective.Progress = Math.Round(compressedPercent, 1);
            SessionManager.SendMessageToAdminSessions("ExtractionProgress", CurrentObjective, CancellationToken.None);

            Progress.Report(Math.Round(compressedPercent, 1));
        }

        private static void FileMoveSuccess(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            if (!e.Item.IsComplete) return;
            totalSize = 0;
        }
    }
}