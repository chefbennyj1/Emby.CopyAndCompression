using System;
using System.IO;
using System.Linq;
using FileCompressionCopy.Configuration;
using MediaBrowser.Model.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;

// ReSharper disable TooManyArguments
namespace FileCompressionCopy.OrganizeFiles.UnzipCopy
{
    public class UnzipAndCopyFiles
    {
        private static long totalSize { get; set; }
        private static IProgress<double> Progress { get; set; }

        public static void BeginDecompressionAndCopy(string fullFileName, string fileName, ILogger log,
            IProgress<double> prog, PluginConfiguration config)
        {
            Progress = prog;
            log.Info("Found New RAR File to Decompress: " + fileName);

            string extractPath = config.EmbyAutoOrganizeFolderPath + "\\" +
                                 (Path.GetFileNameWithoutExtension(fileName));

            log.Info("Creating Extraction Path: " + extractPath);

            Directory.CreateDirectory(extractPath);
            IArchive archive = ArchiveFactory.Open(fullFileName);
            log.Info("Archive open: " + fullFileName);
            // Calculate the total extraction size.
            totalSize = archive.TotalSize;
            log.Info("Archive Total Size: " + totalSize);
            foreach (IArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                archive.EntryExtractionEnd += FileMoveSuccess;
                archive.CompressedBytesRead += Archive_CompressedBytesRead;

                entry.WriteToDirectory(extractPath, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }


        private static void Archive_CompressedBytesRead(object sender, CompressedBytesReadEventArgs e)
        {
            long b = e.CompressedBytesRead;
            var p = Math.Round((e.CompressedBytesRead / (double) totalSize) * 100, 1);
            Progress.Report(p);
        }

        private static void FileMoveSuccess(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
            if (!e.Item.IsComplete) return;
            totalSize = 0;
        }
    }
}