using System.Linq;

namespace Hamburgerz.Models
{
    public static class PasswordRules
    {
        public const int MinLength = 8;

        public static string RequirementsMessage =>
            $"Slaptazodis turi buti bent {MinLength} simboliu ir tureti mazaja, didziaja raide, skaiciu bei specialu simboli.";

        public static bool IsStrong(string? password) =>
            HasMinLength(password)
            && HasLowercase(password)
            && HasUppercase(password)
            && HasDigit(password)
            && HasSymbol(password);

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
