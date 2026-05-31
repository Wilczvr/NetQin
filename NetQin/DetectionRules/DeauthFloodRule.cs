using NetQin.Models;

namespace NetQin.DetectionRules
{
    public class DeauthFloodRule : BurstDetectionRuleBase
    {
        public override string RuleId
        {
            get { return "DEAUTH_BURST"; }
        }

        public override string RuleName
        {
            get { return "Podejrzana seria ramek deautoryzacji"; }
        }

        protected override bool Matches(PacketRecord packet)
        {
            return packet != null && packet.IsDeauth;
        }

        protected override int GetThreshold(DetectionSettings settings)
        {
            return settings.DeauthBurstThreshold;
        }

        protected override int GetWindowSeconds(DetectionSettings settings)
        {
            return settings.DeauthBurstWindowSeconds;
        }

        protected override string GetEventLabel()
        {
            return "deautoryzacji";
        }

        protected override int GetBaseRiskScore()
        {
            return 70;
        }

        protected override string GetRecommendation()
        {
            return "Zweryfikować źródło ramek deautoryzacji oraz sprawdzić, czy nie dochodzi do wymuszonego rozłączania klientów Wi-Fi.";
        }

        protected override string GetEventTag()
        {
            return "deauth";
        }
    }
}