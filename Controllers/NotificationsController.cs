using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnvironmentalSurvey.DTOs;
using System.Security.Claims;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(AppDbContext context, ILogger<NotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/notifications/my-notifications
        [HttpGet("my-notifications")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications(
            [FromQuery] bool? isRead = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var query = _context.Notifications
                    .Where(n => n.UserId == currentUserId);

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                var notificationDtos = notifications.Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

                return Ok(notificationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/notifications/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<NotificationDto>> GetNotificationById(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (notification.UserId != currentUserId)
                {
                    return Forbid();
                }

                var notificationDto = new NotificationDto
                {
                    NotificationId = notification.NotificationId,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return Ok(notificationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/notifications
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationDto createDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(createDto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var notification = new Notification
                {
                    UserId = createDto.UserId,
                    Title = createDto.Title,
                    Message = createDto.Message,
                    Type = createDto.Type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var notificationDto = new NotificationDto
                {
                    NotificationId = notification.NotificationId,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return CreatedAtAction(nameof(GetNotificationById), new { id = notification.NotificationId }, notificationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/notifications/broadcast
        [HttpPost("broadcast")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> BroadcastNotification([FromBody] BroadcastNotificationDto broadcastDto)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.IsActive == true && u.RegistrationStatus == "approved");

                if (!string.IsNullOrEmpty(broadcastDto.TargetRole))
                {
                    query = query.Where(u => u.Role == broadcastDto.TargetRole);
                }

                var users = await query.ToListAsync();

                var notifications = users.Select(u => new Notification
                {
                    UserId = u.UserId,
                    Title = broadcastDto.Title,
                    Message = broadcastDto.Message,
                    Type = broadcastDto.Type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Notification sent to {notifications.Count} users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        [Authorize]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (notification.UserId != currentUserId)
                {
                    return Forbid();
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/notifications/mark-all-read
        [HttpPut("mark-all-read")]
        [Authorize]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == currentUserId && n.IsRead == false)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"{notifications.Count} notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        [Authorize]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var count = await _context.Notifications
                    .Where(n => n.UserId == currentUserId && n.IsRead == false)
                    .CountAsync();

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (notification.UserId != currentUserId)
                {
                    return Forbid();
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/notifications/clear-all
        [HttpDelete("clear-all")]
        [Authorize]
        public async Task<ActionResult> ClearAllNotifications()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == currentUserId)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{notifications.Count} notifications deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing notifications");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}