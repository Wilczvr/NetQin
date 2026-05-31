using NetQin.Models;

namespace NetQin.DetectionRules
{
    public class AuthAssocFloodRule : BurstDetectionRuleBase
    {
        public override string RuleId
        {
            get { return "AUTH_ASSOC_BURST"; }
        }

        public override string RuleName
        {
            get { return "Podejrzana seria ramek authentication/association"; }
        }

        protected override bool Matches(PacketRecord packet)
        {
            return packet != null &&
                   (packet.IsAuthentication ||
                    packet.IsAssociationRequest ||
                    packet.IsAssociationResponse);
        }

        protected override int GetThreshold(DetectionSettings settings)
        {
            return settings.AuthAssocBurstThreshold;
        }

        protected override int GetWindowSeconds(DetectionSettings settings)
        {
            return settings.AuthAssocBurstWindowSeconds;
        }

        protected override string GetEventLabel()
        {
            return "authentication/association";
        }

        protected override int GetBaseRiskScore()
        {
            return 55;
        }

        protected override string GetRecommendation()
        {
            return "Sprawdzić, czy wzrost ramek authentication/association nie jest skutkiem wymuszonego reconnectu lub prób podszywania się pod AP.";
        }

        protected override string GetEventTag()
        {
            return "auth-assoc";
        }
    }
}