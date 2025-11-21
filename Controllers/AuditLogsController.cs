using EnvironmentalSurvey.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnvironmentalSurvey.DTOs;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(AppDbContext context, ILogger<AuditLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/auditlogs
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
            [FromQuery] string? tableName = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(tableName))
                {
                    query = query.Where(a => a.TableName == tableName);
                }

                if (userId.HasValue)
                {
                    query = query.Where(a => a.UserId == userId.Value);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action.Contains(action));
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= toDate.Value);
                }

                var logs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var logDtos = new List<AuditLogDto>();
                foreach (var log in logs)
                {
                    string? userName = null;
                    if (log.UserId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(log.UserId.Value);
                        userName = user?.FullName;
                    }

                    logDtos.Add(new AuditLogDto
                    {
                        LogId = log.LogId,
                        UserId = log.UserId,
                        UserName = userName,
                        Action = log.Action,
                        TableName = log.TableName,
                        RecordId = log.RecordId,
                        OldValue = log.OldValue,
                        NewValue = log.NewValue,
                        IpAddress = log.IpAddress,
                        CreatedAt = log.CreatedAt
                    });
                }

                return Ok(logDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<AuditLogDto>> GetAuditLogById(int id)
        {
            try
            {
                var log = await _context.AuditLogs.FindAsync(id);
                if (log == null)
                {
                    return NotFound(new { message = "Audit log not found" });
                }

                string? userName = null;
                if (log.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(log.UserId.Value);
                    userName = user?.FullName;
                }

                var logDto = new AuditLogDto
                {
                    LogId = log.LogId,
                    UserId = log.UserId,
                    UserName = userName,
                    Action = log.Action,
                    TableName = log.TableName,
                    RecordId = log.RecordId,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    IpAddress = log.IpAddress,
                    CreatedAt = log.CreatedAt
                };

                return Ok(logDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogsByUser(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _context.AuditLogs
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(userId);

                var logDtos = logs.Select(log => new AuditLogDto
                {
                    LogId = log.LogId,
                    UserId = log.UserId,
                    UserName = user?.FullName,
                    Action = log.Action,
                    TableName = log.TableName,
                    RecordId = log.RecordId,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    IpAddress = log.IpAddress,
                    CreatedAt = log.CreatedAt
                }).ToList();

                return Ok(logDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by user");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/table/{tableName}
        [HttpGet("table/{tableName}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogsByTable(
            string tableName,
            [FromQuery] int? recordId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs
                    .Where(a => a.TableName == tableName);

                if (recordId.HasValue)
                {
                    query = query.Where(a => a.RecordId == recordId.Value);
                }

                var logs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var logDtos = new List<AuditLogDto>();
                foreach (var log in logs)
                {
                    string? userName = null;
                    if (log.UserId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(log.UserId.Value);
                        userName = user?.FullName;
                    }

                    logDtos.Add(new AuditLogDto
                    {
                        LogId = log.LogId,
                        UserId = log.UserId,
                        UserName = userName,
                        Action = log.Action,
                        TableName = log.TableName,
                        RecordId = log.RecordId,
                        OldValue = log.OldValue,
                        NewValue = log.NewValue,
                        IpAddress = log.IpAddress,
                        CreatedAt = log.CreatedAt
                    });
                }

                return Ok(logDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by table");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/tables
        [HttpGet("tables")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetTables()
        {
            try
            {
                var tables = await _context.AuditLogs
                    .Select(a => a.TableName)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/actions
        [HttpGet("actions")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetActions()
        {
            try
            {
                var actions = await _context.AuditLogs
                    .Select(a => a.Action)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();

                return Ok(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/auditlogs/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<object>> GetStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= toDate.Value);
                }

                var totalLogs = await query.CountAsync();
                var actionCounts = await query
                    .GroupBy(a => a.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .ToListAsync();

                var tableCounts = await query
                    .GroupBy(a => a.TableName)
                    .Select(g => new { table = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .ToListAsync();

                var uniqueUsers = await query
                    .Where(a => a.UserId.HasValue)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                return Ok(new
                {
                    totalLogs,
                    uniqueUsers,
                    actionCounts,
                    tableCounts,
                    period = new
                    {
                        from = fromDate ?? await query.MinAsync(a => (DateTime?)a.CreatedAt),
                        to = toDate ?? await query.MaxAsync(a => (DateTime?)a.CreatedAt)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/auditlogs/cleanup
        [HttpDelete("cleanup")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                var oldLogs = await _context.AuditLogs
                    .Where(a => a.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{oldLogs.Count} old audit logs deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up audit logs");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}