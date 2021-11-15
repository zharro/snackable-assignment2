using System;
using System.Collections.Generic;

namespace Snackable.PresentationApi.Db
{
    public class File
    {
        public Guid FileId { get; set; }
        public Tenant Tenant { get; set; }
        public FileStatus Status { get; set; }
        public string Name { get; set; }
        public string Mp3Path { get; set; }
        public string OriginalFilePath { get; set; }
        public string SeriesTitle { get; set; }
        public HashSet<FileSegment> Segments { get; set; }
    }
}