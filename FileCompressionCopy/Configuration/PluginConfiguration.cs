using System.Collections.Generic;
using FileCompressionCopy.OrganizeFiles;
using MediaBrowser.Model.Plugins;

namespace FileCompressionCopy.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string EmbyAutoOrganizeFolderPath { get; set; }

        public string MonitoredFolder { get; set; }

        public List<ExtractionInfo> CompletedItems { get; set; }
    }
}