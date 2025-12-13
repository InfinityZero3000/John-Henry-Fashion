using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JohnHenryFashionWeb.Models;
using JohnHenryFashionWeb.Data;
using JohnHenryFashionWeb.Services;
using System.Security.Cryptography;
using System.Text;

namespace JohnHenryFashionWeb.Services
{
    public interface ISecurityService
    {
        Task<bool> IsPasswordValidAsync(string password);
        Task<SecurityCheckResult> CheckAccountSecurityAsync(string userId);
        Task<bool> IsAccountLockedAsync(string userId);
        Task<LoginAttemptResult> RecordLoginAttemptAsync(string email, string ipAddress, bool isSuccessful);
        Task<List<SecurityLog>> GetUserSecurityLogsAsync(string userId, int pageSize = 50);
        Task LogSecurityEventAsync(string userId, string eventType, string description, string? ipAddress = null);
        Task<bool> RequiresTwoFactorAuthAsync(string userId);
        Task<string> GenerateTwoFactorTokenAsync(string userId);
        Task<bool> ValidateTwoFactorTokenAsync(string userId, string token);
        Task<bool> IsIpAddressSuspiciousAsync(string ipAddress);
        Task<List<string>> GetPasswordHistoryAsync(string userId);
        Task SavePasswordHistoryAsync(string userId, string passwordHash);
        Task CleanupExpiredSecurityLogsAsync();
        Task<bool> CheckPasswordReuseAsync(string userId, string newPasswordHash);
        Task<List<ActiveSession>> GetActiveSessionsAsync(string userId);
        Task InvalidateSessionAsync(string sessionId);
        Task InvalidateAllUserSessionsAsync(string userId);
        Task<List<SecurityLog>> GetSecurityLogsAsync(string userId, int count = 50);
    }

    public class SecurityService : ISecurityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SecurityService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly ISystemConfigService _systemConfig;

        public SecurityService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SecurityService> logger,
            ICacheService cacheService,
            IConfiguration configuration,
            ISystemConfigService systemConfig)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _cacheService = cacheService;
            _configuration = configuration;
            _systemConfig = systemConfig;
        }

        public Task<bool> IsPasswordValidAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            var minLength = _configuration.GetValue<int>("Security:PasswordPolicy:MinLength", 8);
            var requireDigit = _configuration.GetValue<bool>("Security:PasswordPolicy:RequireDigit", true);
            var requireLowercase = _configuration.GetValue<bool>("Security:PasswordPolicy:RequireLowercase", true);
            var requireUppercase = _configuration.GetValue<bool>("Security:PasswordPolicy:RequireUppercase", true);
            var requireSpecialChar = _configuration.GetValue<bool>("Security:PasswordPolicy:RequireSpecialChar", true);

            if (password.Length < minLength)
                return Task.FromResult(false);

            if (requireDigit && !password.Any(char.IsDigit))
                return Task.FromResult(false);

            if (requireLowercase && !password.Any(char.IsLower))
                return Task.FromResult(false);

            if (requireUppercase && !password.Any(char.IsUpper))
                return Task.FromResult(false);

            if (requireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
                return Task.FromResult(false);

            // Check against common passwords
            var commonPasswords = new[]
            {
                "password", "123456", "password123", "admin", "qwerty",
                "letmein", "welcome", "monkey", "dragon", "pass"
            };

            if (commonPasswords.Contains(password.ToLower()))
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public async Task<SecurityCheckResult> CheckAccountSecurityAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new SecurityCheckResult { IsSecure = false, Issues = new[] { "User not found" } };

            var issues = new List<string>();
            var suggestions = new List<string>();

            // Check password age
            var lastPasswordChange = await GetLastPasswordChangeAsync(userId);
            var passwordAge = DateTime.UtcNow - lastPasswordChange;
            var maxPasswordAge = _configuration.GetValue<int>("Security:PasswordPolicy:MaxAgeDays", 90);

            if (passwordAge.TotalDays > maxPasswordAge)
            {
                issues.Add($"Password is {(int)passwordAge.TotalDays} days old");
                suggestions.Add("Consider changing your password");
            }

            // Check recent login attempts
            var recentFailedAttempts = await _context.SecurityLogs
                .Where(s => s.UserId == userId && 
                           s.EventType == "LoginFailed" && 
                           s.CreatedAt > DateTime.UtcNow.AddHours(-24))
                .CountAsync();

            if (recentFailedAttempts > 5)
            {
                issues.Add($"{recentFailedAttempts} failed login attempts in the last 24 hours");
                suggestions.Add("Review recent login activity");
            }

            // Check two-factor authentication
            var hasTwoFactor = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!hasTwoFactor)
            {
                issues.Add("Two-factor authentication is not enabled");
                suggestions.Add("Enable two-factor authentication for better security");
            }

            // Check email confirmation
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
            {
                issues.Add("Email address is not confirmed");
                suggestions.Add("Confirm your email address");
            }

            return new SecurityCheckResult
            {
                IsSecure = issues.Count == 0,
                Issues = issues.ToArray(),
                Suggestions = suggestions.ToArray(),
                SecurityScore = CalculateSecurityScore(issues.Count, hasTwoFactor, isEmailConfirmed)
            };
        }

        public async Task<bool> IsAccountLockedAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<LoginAttemptResult> RecordLoginAttemptAsync(string email, string ipAddress, bool isSuccessful)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var userId = user?.Id ?? "Unknown";

            // Record the attempt
            await LogSecurityEventAsync(
                userId,
                isSuccessful ? "LoginSuccess" : "LoginFailed",
                $"Login attempt from IP {ipAddress}",
                ipAddress
            );

            if (!isSuccessful)
            {
                // Check for suspicious activity
                var recentFailedAttempts = await _context.SecurityLogs
                    .Where(s => s.IpAddress == ipAddress && 
                               s.EventType == "LoginFailed" && 
                               s.CreatedAt > DateTime.UtcNow.AddMinutes(-15))
                    .CountAsync();

                var maxAttempts = await _systemConfig.GetSettingAsync<int>("max_login_attempts", 5);
                var isBlocked = recentFailedAttempts >= maxAttempts;

                if (isBlocked)
                {
                    await LogSecurityEventAsync(
                        userId,
                        "IpBlocked",
                        $"IP {ipAddress} blocked due to excessive failed login attempts",
                        ipAddress
                    );
                }

                return new LoginAttemptResult
                {
                    IsBlocked = isBlocked,
                    RemainingAttempts = Math.Max(0, maxAttempts - recentFailedAttempts),
                    BlockedUntil = isBlocked ? DateTime.UtcNow.AddMinutes(15) : null
                };
            }

            return new LoginAttemptResult
            {
                IsBlocked = false,
                RemainingAttempts = await _systemConfig.GetSettingAsync<int>("max_login_attempts", 5)
            };
        }

        public async Task<List<SecurityLog>> GetUserSecurityLogsAsync(string userId, int pageSize = 50)
        {
            return await _context.SecurityLogs
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task LogSecurityEventAsync(string userId, string eventType, string description, string? ipAddress = null)
        {
            try
            {
                var securityLog = new SecurityLog
                {
                    UserId = userId,
                    EventType = eventType,
                    Description = description,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SecurityLogs.Add(securityLog);
                await _context.SaveChangesAsync();

                // Clear user security cache
                await _cacheService.RemoveByPatternAsync($"security_{userId}*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event for user {UserId}: {EventType}", userId, eventType);
            }
        }

        public async Task<bool> RequiresTwoFactorAuthAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isEnabled)
                return false;

            // Check if user has suspicious recent activity
            var recentSuspiciousActivity = await _context.SecurityLogs
                .Where(s => s.UserId == userId && 
                           (s.EventType == "LoginFailed" || s.EventType == "SuspiciousActivity") &&
                           s.CreatedAt > DateTime.UtcNow.AddHours(-24))
                .AnyAsync();

            return recentSuspiciousActivity;
        }

        public async Task<string> GenerateTwoFactorTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            
            // Cache the token for 5 minutes
            var cacheKey = $"2fa_token_{userId}";
            await _cacheService.SetAsync(cacheKey, token, TimeSpan.FromMinutes(5));

            return token;
        }

        public async Task<bool> ValidateTwoFactorTokenAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", token);
            
            if (isValid)
            {
                await LogSecurityEventAsync(userId, "TwoFactorSuccess", "Two-factor authentication successful");
            }
            else
            {
                await LogSecurityEventAsync(userId, "TwoFactorFailed", "Two-factor authentication failed");
            }

            return isValid;
        }

        public async Task<bool> IsIpAddressSuspiciousAsync(string ipAddress)
        {
            var cacheKey = $"suspicious_ip_{ipAddress}";
            var cached = await _cacheService.GetAsync<string>(cacheKey);
            if (cached == "true")
                return true;

            // Check for multiple failed attempts from this IP
            var recentFailures = await _context.SecurityLogs
                .Where(s => s.IpAddress == ipAddress && 
                           s.EventType == "LoginFailed" && 
                           s.CreatedAt > DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            var isSuspicious = recentFailures >= 10;
            
            if (isSuspicious)
            {
                await _cacheService.SetAsync(cacheKey, "true", TimeSpan.FromHours(1));
            }

            return isSuspicious;
        }

        public async Task<List<string>> GetPasswordHistoryAsync(string userId)
        {
            return await _context.PasswordHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5) // Keep last 5 passwords
                .Select(p => p.PasswordHash)
                .ToListAsync();
        }

        public async Task SavePasswordHistoryAsync(string userId, string passwordHash)
        {
            var passwordHistory = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordHistories.Add(passwordHistory);

            // Remove old password history (keep only last 5)
            var oldPasswords = await _context.PasswordHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(5)
                .ToListAsync();

            if (oldPasswords.Any())
            {
                _context.PasswordHistories.RemoveRange(oldPasswords);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckPasswordReuseAsync(string userId, string newPasswordHash)
        {
            var recentPasswords = await GetPasswordHistoryAsync(userId);
            return recentPasswords.Contains(newPasswordHash);
        }

        public async Task CleanupExpiredSecurityLogsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep logs for 90 days

                var expiredLogs = await _context.SecurityLogs
                    .Where(s => s.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (expiredLogs.Any())
                {
                    _context.SecurityLogs.RemoveRange(expiredLogs);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired security logs", expiredLogs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired security logs");
            }
        }

        public async Task<List<ActiveSession>> GetActiveSessionsAsync(string userId)
        {
            return await _context.ActiveSessions
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();
        }

        public async Task InvalidateSessionAsync(string sessionId)
        {
            var session = await _context.ActiveSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session != null)
            {
                _context.ActiveSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task InvalidateAllUserSessionsAsync(string userId)
        {
            var sessions = await _context.ActiveSessions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (sessions.Any())
            {
                _context.ActiveSessions.RemoveRange(sessions);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<DateTime> GetLastPasswordChangeAsync(string userId)
        {
            var lastChange = await _context.PasswordHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            return lastChange == default ? DateTime.UtcNow.AddDays(-30) : lastChange;
        }

        private int CalculateSecurityScore(int issueCount, bool hasTwoFactor, bool isEmailConfirmed)
        {
            var score = 100;
            
            score -= issueCount * 15; // Each issue reduces score by 15
            
            if (!hasTwoFactor)
                score -= 20;
                
            if (!isEmailConfirmed)
                score -= 10;

            return Math.Max(0, Math.Min(100, score));
        }

        public async Task<List<SecurityLog>> GetSecurityLogsAsync(string userId, int count = 50)
        {
            return await _context.SecurityLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }

    public class SecurityCheckResult
    {
        public bool IsSecure { get; set; }
        public string[] Issues { get; set; } = Array.Empty<string>();
        public string[] Suggestions { get; set; } = Array.Empty<string>();
        public int SecurityScore { get; set; }
    }

    public class LoginAttemptResult
    {
        public bool IsBlocked { get; set; }
        public int RemainingAttempts { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
