using System;
using System.Threading.Tasks;
using Refit;

namespace Snackable.PresentationApi.ProcessingApi
{
    public interface IProcessingApiClient
    {
        [Get("/file/all?limit={limit}&offset={offset}")]
        Task<FileDto[]> GetAllFilesAsync(int limit, int offset);

        [Get("/file/segments/{fileId}")]
        Task<FileSegmentDto[]> GetSegmentsAsync(Guid fileId);

        [Get("/file/details/{fileId}")]
        Task<FileDetailsDto> GetDetailsAsync(Guid fileId);
    }
}