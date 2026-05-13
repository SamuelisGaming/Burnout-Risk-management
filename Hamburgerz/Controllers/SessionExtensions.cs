using Microsoft.AspNetCore.Http;

namespace Hamburgerz.Helpers
{
    public static class SessionExtensions
    {
        public static bool IsLoggedIn(this ISession session)
        {
            return session.GetInt32("UserId") != null;
        }

        public static int? GetUserId(this ISession session)
        {
            return session.GetInt32("UserId");
        }

        public static string? GetUsername(this ISession session)
        {
            return session.GetString("Username");
        }

        public static string? GetUserType(this ISession session)
        {
            return UserAccess.NormalizeUserType(session.GetString("UserType"));
        }

        public static string? GetEmail(this ISession session)
        {
            return session.GetString("Email");
        }
    }
}
