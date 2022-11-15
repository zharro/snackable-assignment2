using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackable.PresentationApi.Db;

namespace Snackable.PresentationApi.PresentationApi
{
    [ApiController]
    [Route("files")]
    public class FilesController : ControllerBase
    {
        private readonly SnackableDbContext _dbContext;

        public FilesController(SnackableDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{fileId}")]
        public async Task<ActionResult<FileResponse[]>> Get(Guid fileId)
        {
            var file = await _dbContext.Files
                .Include(f => f.Segments)
                .SingleOrDefaultAsync(f => f.FileId == fileId);

            if (file == null)
                return NotFound();
            if (file.Status != FileStatus.Finished)
                return NotFound("File hasn't been processed yet or file processing failed");

            return Ok(new[]
            {
                new FileResponseDetailed(
                    file.FileId,
                    FileStatus.Finished,
                    file.Name,
                    file.Mp3Path,
                    file.OriginalFilePath,
                    file.SeriesTitle,
                    file.Segments.Select(s =>
                        new FileSegmentResponse(s.FileSegmentId, s.Text, s.StartTime, s.EndTime)).ToArray()
                )
            });
        }

        [HttpGet]
        public async Task<ActionResult<FileResponse[]>> Get(
            [FromQuery] int limit = 1000,
            [FromQuery] int offset = 0)
        {
            var filesPortion = await _dbContext.Files
                .Where(f => f.Status == FileStatus.Finished)
                .Skip(offset)
                .Take(limit)
                .Select(f => new FileResponse(f.FileId, f.Status, f.Name))
                .ToArrayAsync();

            return Ok(filesPortion);
        }
    }
}