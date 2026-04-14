using System.Linq;

namespace Hamburgerz.Models
{
    public static class PasswordRules
    {
        public const int MinLength = 8;
        public const int MinimumAcceptedScore = 3;

        public static string RequirementsMessage =>
            $"Slaptazodis turi buti bent {MinLength} simboliu ir pasiekti bent vidutini stipruma.";

        public static bool IsStrong(string? password) =>
            HasMinLength(password) && GetScore(password) >= MinimumAcceptedScore;

        public static int GetScore(string? password)
        {
            var score = 0;

            if (HasMinLength(password)) score += 1;
            if (HasLowercase(password)) score += 1;
            if (HasUppercase(password)) score += 1;
            if (HasDigit(password)) score += 1;
            if (HasSymbol(password)) score += 1;

            return score;
        }

        public static bool HasMinLength(string? password) =>
            !string.IsNullOrEmpty(password) && password.Length >= MinLength;

        public static bool HasLowercase(string? password) =>
            !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

        public static bool HasUppercase(string? password) =>
            !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

        public static bool HasDigit(string? password) =>
            !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

        public static bool HasSymbol(string? password) =>
            !string.IsNullOrEmpty(password) && password.Any(character => !char.IsLetterOrDigit(character));
    }
}
