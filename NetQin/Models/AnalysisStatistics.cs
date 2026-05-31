namespace NetQin.Models
{
    public class AnalysisStatistics
    {
        public int TotalPackets { get; set; }
        public int DeauthCount { get; set; }
        public int DisassocCount { get; set; }
        public int BeaconCount { get; set; }
        public int AuthenticationCount { get; set; }
        public int AssociationRequestCount { get; set; }
        public int AssociationResponseCount { get; set; }
        public int ParseErrorCount { get; set; }
        public int SuspiciousBurstCount { get; set; }
    }
}