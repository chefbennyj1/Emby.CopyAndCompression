using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace FileCompressionCopy
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private static ServerEntryPoint Instance { get; set; }

        public ServerEntryPoint(ILogManager logManager, IFileSystem file)
        {
            Instance = this;
        }
        

        public void Dispose()
        {
        }

        public void Run()
        {
        }
    }
}