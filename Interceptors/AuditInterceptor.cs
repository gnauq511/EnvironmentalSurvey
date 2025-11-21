using EnvironmentalSurvey.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace EnvironmentalSurvey.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                CreateAuditLogs(eventData.Context);
            }
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                CreateAuditLogs(eventData.Context);
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void CreateAuditLogs(DbContext context)
        {
            // Get current user ID from HTTP context
            var userId = GetCurrentUserId();
            var ipAddress = GetIpAddress();

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .Where(e => e.Entity.GetType() != typeof(AuditLog)) // Don't audit the audit log itself
                .ToList();

            foreach (var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = GetAction(entry.State),
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    RecordId = GetPrimaryKey(entry),
                    OldValue = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                        ? SerializeObject(entry.OriginalValues.ToObject())
                        : null,
                    NewValue = entry.State == EntityState.Added || entry.State == EntityState.Modified
                        ? SerializeObject(entry.CurrentValues.ToObject())
                        : null,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now
                };

                context.Set<AuditLog>().Add(auditLog);
            }
        }

        private int? GetCurrentUserId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return userId;
                    }
                }
            }
            catch { }
            return null;
        }

        private string? GetIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    // Try to get real IP from headers (for load balancers/proxies)
                    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        return forwardedFor.Split(',')[0].Trim();
                    }

                    return httpContext.Connection.RemoteIpAddress?.ToString();
                }
            }
            catch { }
            return null;
        }

        private static string GetAction(EntityState state)
        {
            return state switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };
        }

        private static int? GetPrimaryKey(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            try
            {
                var keyProperty = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
                if (keyProperty != null)
                {
                    var value = entry.Property(keyProperty.Name).CurrentValue;
                    if (value != null && int.TryParse(value.ToString(), out int id))
                    {
                        return id;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string? SerializeObject(object? obj)
        {
            if (obj == null) return null;

            try
            {
                // Remove navigation properties and collections to avoid circular references
                var properties = obj.GetType().GetProperties()
                    .Where(p => p.PropertyType.IsValueType ||
                               p.PropertyType == typeof(string) ||
                               p.PropertyType == typeof(DateTime) ||
                               p.PropertyType == typeof(DateTime?))
                    .ToDictionary(
                        p => p.Name,
                        p => p.GetValue(obj)
                    );

                return JsonSerializer.Serialize(properties, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch
            {
                return obj.ToString();
            }
        }
    }
}