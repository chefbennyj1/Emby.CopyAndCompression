using System;

namespace FileCompressionCopy.OrganizeFiles
{
    public class ExtractionInfo
    {
        public string Name                    { get; set; }
        public string completed               { get; set; }
        public string size                    { get; set; } = string.Empty;
        public string extention               { get; set; } = ".rar";
        public DateTimeOffset CreationTimeUTC { get; set; } = DateTimeOffset.MinValue;
        public string CopyType                { get; set; } = "Unpacked";
    }
}