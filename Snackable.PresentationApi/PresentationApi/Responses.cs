using System;
using Snackable.PresentationApi.Db;

namespace Snackable.PresentationApi.PresentationApi
{
    public record FileResponse(
        Guid FileId,
        FileStatus Status,
        string FileName
        );

    public record FileResponseDetailed(
        Guid FileId,
        FileStatus Status,
        string FileName,
        string Mp3Path,
        string OriginalFilePath,
        string SeriesTitle,
        FileSegmentResponse[] Segments
    );

    public record FileSegmentResponse(int FileSegmentId, string SegmentText, int StartTime, int EndTime);
}