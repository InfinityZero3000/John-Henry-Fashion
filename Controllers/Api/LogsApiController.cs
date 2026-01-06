using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JohnHenryFashionWeb.Services;

namespace JohnHenryFashionWeb.Controllers.Api
{
    [ApiController]
    [Route("api/admin/logs")]
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer", Roles = "Admin")]
    public class LogsApiController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsApiController> _logger;

        public LogsApiController(ILogService logService, ILogger<LogsApiController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        /// <summary>
        /// Get logs with optional filtering
        /// </summary>
        /// <param name="fromDate">Start date (ISO format)</param>
        /// <param name="toDate">End date (ISO format)</param>
        /// <param name="level">Log level: INF, WRN, ERR, DBG</param>
        /// <param name="maxCount">Maximum number of logs to return (default 500)</param>
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? level,
            [FromQuery] int maxCount = 500)
        {
            try
            {
                _logger.LogInformation("Fetching logs - From: {FromDate}, To: {ToDate}, Level: {Level}, Max: {MaxCount}",
                    fromDate, toDate, level, maxCount);

                var logs = await _logService.GetLogsAsync(fromDate, toDate, level, maxCount);

                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    logs = logs.Select(l => new
                    {
                        id = l.Id,
                        timestamp = l.Timestamp,
                        level = l.Level,
                        message = l.Message.Length > 200 ? l.Message.Substring(0, 200) + "..." : l.Message,
                        fullMessage = l.Message,
                        source = l.Source,
                        userEmail = l.UserEmail,
                        ipAddress = l.IpAddress,
                        requestUrl = l.RequestUrl
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs");
                return StatusCode(500, new { success = false, error = "Failed to fetch logs" });
            }
        }

        /// <summary>
        /// Get a specific log entry by ID
        /// </summary>
        [HttpGet("{logId}")]
        public async Task<IActionResult> GetLogById(string logId)
        {
            try
            {
                var log = await _logService.GetLogByIdAsync(logId);

                if (log == null)
                    return NotFound(new { success = false, error = "Log entry not found" });

                return Ok(new
                {
                    success = true,
                    log = new
                    {
                        id = log.Id,
                        timestamp = log.Timestamp,
                        level = log.Level,
                        message = log.Message,
                        source = log.Source,
                        exception = log.Exception,
                        userEmail = log.UserEmail,
                        ipAddress = log.IpAddress,
                        requestUrl = log.RequestUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching log by ID: {LogId}", logId);
                return StatusCode(500, new { success = false, error = "Failed to fetch log details" });
            }
        }

        /// <summary>
        /// Get list of available log files
        /// </summary>
        [HttpGet("files")]
        public async Task<IActionResult> GetLogFiles()
        {
            try
            {
                var files = await _logService.GetLogFilesAsync();

                return Ok(new
                {
                    success = true,
                    count = files.Count,
                    files = files.Select(f => new
                    {
                        name = f,
                        date = ExtractDateFromFileName(f)
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching log files");
                return StatusCode(500, new { success = false, error = "Failed to fetch log files" });
            }
        }

        /// <summary>
        /// Get statistics about logs (counts by level)
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _logService.GetLogStatisticsAsync();

                return Ok(new
                {
                    success = true,
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching log statistics");
                return StatusCode(500, new { success = false, error = "Failed to fetch statistics" });
            }
        }

        /// <summary>
        /// Download a specific log file
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadLogFile(string fileName)
        {
            try
            {
                var content = await _logService.GetLogFileContentAsync(fileName);
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);

                return File(bytes, "text/plain", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { success = false, error = "Log file not found" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading log file: {FileName}", fileName);
                return StatusCode(500, new { success = false, error = "Failed to download log file" });
            }
        }

        private string? ExtractDateFromFileName(string fileName)
        {
            // Extract date from filename: john-henry-20251022.txt
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"john-henry-(\d{8})\.txt");
            if (match.Success)
            {
                var dateStr = match.Groups[1].Value;
                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", 
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, 
                    out var date))
                {
                    return date.ToString("yyyy-MM-dd");
                }
            }
            return null;
        }
    }
}
