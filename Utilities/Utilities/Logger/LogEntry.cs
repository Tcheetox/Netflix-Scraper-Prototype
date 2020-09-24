using System;

namespace Utilities.Logger
{
    public class LogEntry
    {
        public readonly bool IsException;
        public readonly Exception Exception;
        public readonly string Caption;
        public readonly string Message;
        public readonly SeverityType Severity;
        public readonly DateTime Timestamp;
        public readonly bool IsLogOnly;

        public enum SeverityType
        {
            None,
            Low,
            Medium,
            High,
            Critical
        }

        public LogEntry(string _caption, SeverityType _severity, bool _isLogOnly, string _message)
        {
            Caption = _caption;
            Message = _message;
            Severity = _severity;
            Timestamp = DateTime.Now;
            IsLogOnly = _isLogOnly;
        }

        public LogEntry(Exception _exception, string _caption, SeverityType _severity, bool _isLogOnly , string _message)
        {
            IsException = true;
            Exception = _exception;
            Caption = _caption;
            Message = _message;
            Severity = _severity;
            Timestamp = DateTime.Now;
            IsLogOnly = _isLogOnly;
        }

        public string ToString(bool printSeverity = true) 
        {
            // Prefix
            string content = $"[{Timestamp:HH:mm:ss}] ";
            if (Severity != SeverityType.None && printSeverity)
                content += $"*{Severity}* ";

            // header
            if (IsException)
                content += $"!! {Caption}";
            else
                content += $"{Caption}";

            // body
            if (!string.IsNullOrEmpty(Message))
                content += Environment.NewLine + $" > {Message}";

            // footer
            if (IsException)
                content += Environment.NewLine + Exception.ToString();

            return content;
        }
    }
}
