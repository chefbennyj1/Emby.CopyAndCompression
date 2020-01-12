using System;
using System.Collections.Generic;
using System.IO;
using FileCompressionCopy.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FileCompressionCopy
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage, IHasWebPages
    {
        public static Plugin Instance { get; set; }
        public ImageFormat ThumbImageFormat => ImageFormat.Jpg;

        private readonly Guid _id = new Guid("D8B538E4-5579-4239-A251-721BC3AB4D9D");
        public override Guid Id => _id;

        public override string Name => "Copy and Decompression";


        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.jpg");
        }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths,
            xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = "FileCompressionCopyPage",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.FileCompressionCopyPage.html"
            },
            new PluginPageInfo
            {
                Name = "FileCompressionCopyPageJS",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.FileCompressionCopyPage.js"
            }
        };
    }
}