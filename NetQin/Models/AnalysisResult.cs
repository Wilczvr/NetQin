using System.Collections.Generic;

namespace NetQin.Models
{
    public class AnalysisResult
    {
        public string FilePath { get; set; } = "";
        public List<PacketRecord> Packets { get; set; } = new List<PacketRecord>();
        public List<AnalysisLogEntry> Logs { get; set; } = new List<AnalysisLogEntry>();
        public AnalysisStatistics Statistics { get; set; } = new AnalysisStatistics();
        public List<DetectionIncident> Incidents { get; set; } = new List<DetectionIncident>();
        public List<CorrelatedIncident> CorrelatedIncidents { get; set; } = new List<CorrelatedIncident>();
    }
}