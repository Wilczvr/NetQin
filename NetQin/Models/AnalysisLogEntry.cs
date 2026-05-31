using System;

namespace NetQin.Models
{
    public class AnalysisLogEntry
    {
        public DateTime Timestamp { get; set; }
        public AnalysisLogLevel Level { get; set; }
        public string Message { get; set; } = "";
    }
}