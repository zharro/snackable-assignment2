using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snackable.PresentationApi.Db;
using Snackable.PresentationApi.ProcessingApi;

namespace Snackable.PresentationApi.BackgroundJobs
{
    public class AllFilesSynchronizer : IHostedService, IDisposable
    {
        private const int FilesPageSize = 1000;
        private readonly TimeSpan _allFilesSyncPeriod = TimeSpan.FromMinutes(1);

        private readonly IProcessingApiClient _processingApiClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _allFilesSyncTimer;

        public AllFilesSynchronizer(
            IProcessingApiClient processingApiClient,
            IServiceScopeFactory scopeFactory)
        {
            _processingApiClient = processingApiClient;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _allFilesSyncTimer = new Timer(SyncAllFiles, null, TimeSpan.Zero, _allFilesSyncPeriod);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _allFilesSyncTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _allFilesSyncTimer?.Dispose();
        }

        private async void SyncAllFiles(object _)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SnackableDbContext>();

            var finishedFileIds = dbContext.Files
                .Where(f => f.Status == FileStatus.Finished)
                .Select(f => f.FileId)
                .ToHashSet();

            var processingFileIds = dbContext.Files
                .Where(f => f.Status == FileStatus.Processing)
                .Select(f => f.FileId)
                .ToHashSet();

            while (true)
            {
                var filesPortion = await _processingApiClient.GetAllFilesAsync(FilesPageSize, 0);

                await SyncFinishedFilesAsync(finishedFileIds, filesPortion, dbContext);

                // Save files in Processing status to control by ProcessingFilesSynchronizer procedure
                SyncProcessingFiles(processingFileIds, filesPortion, dbContext);
                await dbContext.SaveChangesAsync();

                if(filesPortion.Length < FilesPageSize)
                    break;
            }
        }

        private async Task SyncFinishedFilesAsync(
            HashSet<Guid> knownFinishedFileIds,
            FileDto[] relevantFiles,
            SnackableDbContext dbContext)
        {
            var justFinishedFiles = relevantFiles
                .Where(f => f.ProcessingStatus == FileStatus.Finished)
                .Where(f => !knownFinishedFileIds.Contains(f.FileId));

            // Save a new portion of finished files
            foreach (var justFinishedFile in justFinishedFiles)
            {
                // Load details and segments in parallel
                var detailsTask = _processingApiClient.GetDetailsAsync(justFinishedFile.FileId);
                var segmentsTask = _processingApiClient.GetSegmentsAsync(justFinishedFile.FileId);
                await Task.WhenAll(detailsTask, segmentsTask);

                var current = await dbContext.Files
                    .Where(f => f.FileId == justFinishedFile.FileId)
                    .SingleOrDefaultAsync();

                if (current == null)
                {
                    dbContext.Files.Add(new File
                    {
                        FileId = justFinishedFile.FileId,
                        Tenant = Tenant.Default,
                        Status = justFinishedFile.ProcessingStatus,
                        Name = detailsTask.Result.FileName,
                        Mp3Path = detailsTask.Result.Mp3Path,
                        OriginalFilePath = detailsTask.Result.OriginalFilePath,
                        SeriesTitle = detailsTask.Result.SeriesTitle,
                        Segments = segmentsTask.Result.Select(s => new FileSegment
                        {
                            FileSegmentId = s.FileSegmentId,
                            Text = s.SegmentText,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime
                        }).ToHashSet()
                    });
                }
                else
                {
                    current.Status = FileStatus.Finished;
                    current.Name = detailsTask.Result.FileName;
                    current.Mp3Path = detailsTask.Result.Mp3Path;
                    current.OriginalFilePath = detailsTask.Result.OriginalFilePath;
                    current.Segments = segmentsTask.Result.Select(s => new FileSegment
                    {
                        FileSegmentId = s.FileSegmentId,
                        Text = s.SegmentText,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    }).ToHashSet();
                }
            }
        }

        private void SyncProcessingFiles(
            HashSet<Guid> knownProcessingFileIds,
            FileDto[] relevantFiles,
            SnackableDbContext dbContext)
        {
            var newFiles = relevantFiles
                .Where(f => f.ProcessingStatus == FileStatus.Processing)
                .Where(f => !knownProcessingFileIds.Contains(f.FileId));

            foreach (var newFile in newFiles)
            {
                dbContext.Files.Add(new File
                {
                    FileId = newFile.FileId,
                    Tenant = Tenant.Default,
                    Status = newFile.ProcessingStatus
                });
            }
        }
    }
}