namespace NetQin.Models
{
    public class DetectionSettings
    {
        public int DeauthBurstThreshold { get; set; } = 5;
        public int DeauthBurstWindowSeconds { get; set; } = 3;

        public int DisassocBurstThreshold { get; set; } = 5;
        public int DisassocBurstWindowSeconds { get; set; } = 3;
        public bool CountDisassocAsSuspicious { get; set; } = true;

        public int BeaconBurstThreshold { get; set; } = 35;
        public int BeaconBurstWindowSeconds { get; set; } = 3;

        public int AuthAssocBurstThreshold { get; set; } = 10;
        public int AuthAssocBurstWindowSeconds { get; set; } = 5;

        public bool EnableEvilTwinHeuristic { get; set; } = true;
        public int EvilTwinMinDistinctBssidsPerSsid { get; set; } = 2;
        public int EvilTwinRapidAppearanceWindowSeconds { get; set; } = 30;
        public int EvilTwinMinBeaconsPerBssid { get; set; } = 3;
        public int EvilTwinMinBaselineLeadSeconds { get; set; } = 2;
        public bool EvilTwinRequireChannelDifference { get; set; } = true;
        public bool IgnoreHiddenSsidsForEvilTwin { get; set; } = true;
    }
}
