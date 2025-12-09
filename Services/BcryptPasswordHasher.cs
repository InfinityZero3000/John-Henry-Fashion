using Microsoft.AspNetCore.Identity;
using BCrypt.Net;

namespace JohnHenryFashionWeb.Services
{
    /// <summary>
    /// Custom password hasher using BCrypt algorithm
    /// BCrypt is more secure than default PBKDF2 because:
    /// - Automatically handles salt generation
    /// - Configurable work factor (cost) that can be increased over time
    /// - Designed specifically for password hashing (slow by design)
    /// </summary>
    public class BcryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private readonly ILogger<BcryptPasswordHasher<TUser>> _logger;
        
        // Work factor (cost): 12 is a good balance between security and performance
        // Each increment doubles the time required to hash
        // 10 = ~100ms, 12 = ~400ms, 14 = ~1.6s
        private const int WorkFactor = 12;

        public BcryptPasswordHasher(ILogger<BcryptPasswordHasher<TUser>> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hash a password using BCrypt
        /// </summary>
        /// <param name="user">The user (not used but required by interface)</param>
        /// <param name="password">The plain text password to hash</param>
        /// <returns>BCrypt hashed password with embedded salt</returns>
        public string HashPassword(TUser user, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // BCrypt.HashPassword automatically:
            // 1. Generates a random salt
            // 2. Combines salt with password
            // 3. Applies the bcrypt algorithm with specified work factor
            // 4. Returns hash in format: $2a$12$salt+hash (includes version, cost, salt, and hash)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
            
            _logger.LogDebug("Password hashed using BCrypt with work factor {WorkFactor}", WorkFactor);
            
            return hashedPassword;
        }

        /// <summary>
        /// Verify a password against a BCrypt hash
        /// Also handles legacy hashes from ASP.NET Identity (PBKDF2)
        /// </summary>
        /// <param name="user">The user (not used but required by interface)</param>
        /// <param name="hashedPassword">The stored hash to verify against</param>
        /// <param name="providedPassword">The plain text password to verify</param>
        /// <returns>Verification result</returns>
        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            if (string.IsNullOrEmpty(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Check if this is a BCrypt hash (starts with $2a$, $2b$, or $2y$)
            if (IsBcryptHash(hashedPassword))
            {
                try
                {
                    var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
                    
                    if (isValid)
                    {
                        // Check if rehash is needed (e.g., work factor was increased)
                        if (NeedsRehash(hashedPassword))
                        {
                            _logger.LogInformation("Password verified but needs rehash with updated work factor");
                            return PasswordVerificationResult.SuccessRehashNeeded;
                        }
                        
                        return PasswordVerificationResult.Success;
                    }
                    
                    return PasswordVerificationResult.Failed;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying BCrypt password");
                    return PasswordVerificationResult.Failed;
                }
            }
            else
            {
                // This is a legacy hash (ASP.NET Identity PBKDF2)
                // Try to verify using the default hasher for backwards compatibility
                try
                {
                    var defaultHasher = new PasswordHasher<TUser>();
                    var result = defaultHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                    
                    if (result == PasswordVerificationResult.Success || 
                        result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        // Password is valid but stored in old format
                        // Signal that it needs to be rehashed with BCrypt
                        _logger.LogInformation("Legacy password hash verified, needs migration to BCrypt");
                        return PasswordVerificationResult.SuccessRehashNeeded;
                    }
                    
                    return PasswordVerificationResult.Failed;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying legacy password hash");
                    return PasswordVerificationResult.Failed;
                }
            }
        }

        /// <summary>
        /// Check if a hash is a BCrypt hash
        /// </summary>
        private static bool IsBcryptHash(string hash)
        {
            // BCrypt hashes start with $2a$, $2b$, or $2y$ followed by the cost factor
            return !string.IsNullOrEmpty(hash) && 
                   (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"));
        }

        /// <summary>
        /// Check if a BCrypt hash needs to be rehashed (e.g., work factor changed)
        /// </summary>
        private bool NeedsRehash(string hash)
        {
            try
            {
                // Extract work factor from hash
                // Format: $2a$XX$... where XX is the work factor
                var parts = hash.Split('$');
                if (parts.Length >= 3 && int.TryParse(parts[2], out var currentWorkFactor))
                {
                    return currentWorkFactor < WorkFactor;
                }
            }
            catch
            {
                // If we can't parse, assume no rehash needed
            }
            
            return false;
        }
    }
}
