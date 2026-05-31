using System;
using System.Collections.Generic;
using System.Linq;

namespace NetQin.Models
{
    public class CorrelatedIncident
    {
        public string CorrelationId { get; set; } = "";
        public string CorrelationType { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public string Ssid { get; set; } = "?";
        public string Bssid { get; set; } = "?";
        public string SourceMac { get; set; } = "?";

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

        public string Recommendation { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public List<DetectionIncident> SourceIncidents { get; set; } = new List<DetectionIncident>();

        public string Severity
        {
            get { return RiskLevel.ToString(); }
        }

        public string TagsDisplay
        {
            get
            {
                if (Tags == null || Tags.Count == 0)
                    return "-";

                return string.Join(", ", Tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }
        }

        public string SourceRulesDisplay
        {
            get
            {
                if (SourceIncidents == null || SourceIncidents.Count == 0)
                    return "-";

                return string.Join(", ", SourceIncidents
                    .Where(i => i != null && !string.IsNullOrWhiteSpace(i.RuleId))
                    .Select(i => i.RuleId)
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}