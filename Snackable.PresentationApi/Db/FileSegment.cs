namespace Snackable.PresentationApi.Db
{
    public class FileSegment
    {
        public int FileSegmentId { get; set; }
        public string Text { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }
}