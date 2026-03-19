using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Hamburgerz.PasswordValidation
{
    public class PasswordComplexity : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return false;

            var password = value.ToString();

            // At least one lowercase
            if (!Regex.IsMatch(password, "[a-z]"))
                return false;

            // At least one uppercase
            if (!Regex.IsMatch(password, "[A-Z]"))
                return false;

            // At least one digit
            if (!Regex.IsMatch(password, "[0-9]"))
                return false;

            // At least one special character
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return false;

            return true;
        }
    }
}
