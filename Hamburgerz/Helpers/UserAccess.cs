using Microsoft.AspNetCore.Http;

namespace Hamburgerz.Helpers
{
    public static class UserAccess
    {
        public const string User = "user";
        public const string Premium = "premium";
        public const string Admin = "admin";
        public const int FreeMeasurementLimit = 7;

        public static string NormalizeUserType(string? userType)
        {
            var normalized = (userType ?? string.Empty).Trim().ToLowerInvariant();

            return normalized switch
            {
                Admin => Admin,
                Premium => Premium,
                _ => User
            };
        }

        public static bool IsAdmin(string? userType) =>
            NormalizeUserType(userType) == Admin;

        public static bool IsPremium(string? userType) =>
            NormalizeUserType(userType) == Premium;

        public static bool HasPremiumFeatures(string? userType)
        {
            var normalized = NormalizeUserType(userType);
            return normalized == Premium || normalized == Admin;
        }

        public static bool IsAdmin(this ISession session) =>
            IsAdmin(session.GetString("UserType"));

        public static bool HasPremiumFeatures(this ISession session) =>
            HasPremiumFeatures(session.GetString("UserType"));
    }
}
