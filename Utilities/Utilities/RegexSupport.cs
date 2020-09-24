using System;
using System.Text.RegularExpressions;
using Utilities.Logger;

namespace Utilities
{
    public static class RegexSupport
    {
        public static float ExtractDigits(string input)
        {
            return float.Parse(Regex.Replace(input, "[^.0-9]", ""));
        }

        public static bool TryExtractDigits(string input, out float digits)
        {
            digits = 0;
            try
            {
                digits = ExtractDigits(input);
                return true;
            }
            catch(Exception ex)
            {
                Log.Write(ex, $"Regex support couldn't extract digits from '{input}'", LogEntry.SeverityType.Low);
                return false;
            }
        }
    }
}
