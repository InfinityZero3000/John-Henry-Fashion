using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Models;

namespace JohnHenryFashionWeb.Services
{
    public interface ISystemConfigService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
        Task<bool> SetSettingAsync(string key, string value, string category = "general", string? description = null);
    }

    public class SystemConfigService : ISystemConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SystemConfigService> _logger;
        private readonly Dictionary<string, (string Value, DateTime CachedAt)> _cache = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public SystemConfigService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<SystemConfigService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            // Check cache first
            if (_cache.TryGetValue(key, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < _cacheExpiry)
                {
                    return cached.Value;
                }
                _cache.Remove(key);
            }

            try
            {
                // Try database first
                var setting = await _context.SystemConfigurations
                    .Where(s => s.Key == key)
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                if (setting != null)
                {
                    _cache[key] = (setting, DateTime.UtcNow);
                    return setting;
                }

                // Fallback to appsettings.json
                var configValue = _configuration[$"Settings:{key}"];
                if (configValue != null)
                {
                    return configValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting {Key}", key);
                // Fallback to appsettings.json on error
                return _configuration[$"Settings:{key}"];
            }

            return null;
        }

        public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
        {
            var value = await GetSettingAsync(key);
            if (value == null)
            {
                return defaultValue;
            }

            try
            {
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(value);
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(value);
                }
                else if (typeof(T) == typeof(decimal))
                {
                    return (T)(object)decimal.Parse(value);
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)double.Parse(value);
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }
                else
                {
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing setting {Key} with value {Value}", key, value);
                return defaultValue;
            }
        }

        public async Task<bool> SetSettingAsync(string key, string value, string category = "general", string? description = null)
        {
            try
            {
                var setting = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting != null)
                {
                    setting.Value = value;
                    setting.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    setting = new SystemConfiguration
                    {
                        Id = Guid.NewGuid(),
                        Key = key,
                        Value = value,
                        Category = category,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.SystemConfigurations.Add(setting);
                }

                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cache.Remove(key);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting {Key} to {Value}", key, value);
                return false;
            }
        }
    }
}
