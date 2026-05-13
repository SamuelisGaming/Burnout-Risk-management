using Hamburgerz.Data;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Helpers
{
    public static class UserTypeSchema
    {
        public static async Task EnsureAsync(AppDbContext context, ILogger? logger = null)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE `users` MODIFY COLUMN `user_type` varchar(20) NOT NULL DEFAULT 'user';");

                await context.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE `users`
                    SET `user_type` = 'user'
                    WHERE `user_type` IS NULL
                       OR TRIM(`user_type`) = ''
                       OR LOWER(TRIM(`user_type`)) NOT IN ('user', 'premium', 'admin');
                    """);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to ensure users.user_type can store user, premium and admin values.");
                throw;
            }
        }
    }
}
