using System;
using Snackable.PresentationApi.Db;

namespace Snackable.PresentationApi.ProcessingApi
{
    public record FileDto(Guid FileId, FileStatus ProcessingStatus);

    public record FileDetailsDto(
        Guid FileId,
        string FileName,
        string Mp3Path,
        string OriginalFilePath,
        string SeriesTitle);

    public record FileSegmentDto(int FileSegmentId, string SegmentText, int StartTime, int EndTime);
}