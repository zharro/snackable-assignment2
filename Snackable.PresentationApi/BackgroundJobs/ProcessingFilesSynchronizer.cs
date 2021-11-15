using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snackable.PresentationApi.Db;
using Snackable.PresentationApi.ProcessingApi;

namespace Snackable.PresentationApi.BackgroundJobs
{
    public class ProcessingFilesSynchronizer : IHostedService, IDisposable
    {
        private readonly TimeSpan _processingFilesSyncPeriod = TimeSpan.FromSeconds(3);

        private readonly IProcessingApiClient _processingApiClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _processingFilesSyncTimer;

        public ProcessingFilesSynchronizer(
            IProcessingApiClient processingApiClient,
            IServiceScopeFactory scopeFactory)
        {
            _processingApiClient = processingApiClient;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _processingFilesSyncTimer = new Timer(SyncProcessingFiles, null, TimeSpan.Zero, _processingFilesSyncPeriod);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _processingFilesSyncTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _processingFilesSyncTimer?.Dispose();
        }

        private async void SyncProcessingFiles(object _)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SnackableDbContext>();

            var processingFiles = dbContext.Files
                .Where(f => f.Status == FileStatus.Processing);

            foreach (var processingFile in processingFiles)
            {
                var segments = await _processingApiClient.GetSegmentsAsync(processingFile.FileId);

                // As Processing Api doesn't provide a direct way of determining a file status -
                // determine it by presence of segments
                if (segments.Length > 0)
                {
                    processingFile.Status = FileStatus.Finished;
                    processingFile.Segments = segments.Select(s => new FileSegment
                    {
                        FileSegmentId = s.FileSegmentId,
                        Text = s.SegmentText,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    }).ToHashSet();

                    var details = await _processingApiClient.GetDetailsAsync(processingFile.FileId);
                    processingFile.Name = details.FileName;
                    processingFile.Mp3Path = details.Mp3Path;
                    processingFile.OriginalFilePath = details.OriginalFilePath;
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}