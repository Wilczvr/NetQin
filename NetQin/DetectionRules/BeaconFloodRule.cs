using NetQin.Models;

namespace NetQin.DetectionRules
{
    public class BeaconFloodRule : BurstDetectionRuleBase
    {
        public override string RuleId
        {
            get { return "BEACON_BURST"; }
        }

        public override string RuleName
        {
            get { return "Nietypowa intensywność ramek beacon"; }
        }

        protected override bool Matches(PacketRecord packet)
        {
            return packet != null && packet.IsBeacon;
        }

        protected override int GetThreshold(DetectionSettings settings)
        {
            return settings.BeaconBurstThreshold;
        }

        protected override int GetWindowSeconds(DetectionSettings settings)
        {
            return settings.BeaconBurstWindowSeconds;
        }

        protected override string GetEventLabel()
        {
            return "beacon";
        }

        protected override int GetBaseRiskScore()
        {
            return 20;
        }

        protected override string GetRecommendation()
        {
            return "Porównać liczbę beaconów z typowym profilem badanego środowiska i sprawdzić, czy wzrost nie wynika z normalnej pracy punktu dostępowego.";
        }

        protected override string GetEventTag()
        {
            return "beacon";
        }
    }
}