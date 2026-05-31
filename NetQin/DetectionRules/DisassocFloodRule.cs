using System.Collections.Generic;
using NetQin.Models;

namespace NetQin.DetectionRules
{
    public class DisassocFloodRule : BurstDetectionRuleBase
    {
        public override string RuleId
        {
            get { return "DISASSOC_BURST"; }
        }

        public override string RuleName
        {
            get { return "Podejrzana seria ramek disassociation"; }
        }

        public override List<DetectionIncident> Detect(List<PacketRecord> packets, DetectionSettings settings)
        {
            if (settings == null || !settings.CountDisassocAsSuspicious)
            {
                return new List<DetectionIncident>();
            }

            return base.Detect(packets, settings);
        }

        protected override bool Matches(PacketRecord packet)
        {
            return packet != null && packet.IsDisassoc;
        }

        protected override int GetThreshold(DetectionSettings settings)
        {
            return settings.DisassocBurstThreshold;
        }

        protected override int GetWindowSeconds(DetectionSettings settings)
        {
            return settings.DisassocBurstWindowSeconds;
        }

        protected override string GetEventLabel()
        {
            return "disassociation";
        }

        protected override int GetBaseRiskScore()
        {
            return 60;
        }

        protected override string GetRecommendation()
        {
            return "Sprawdzić, czy rozłączenia są zgodne z normalnym zachowaniem AP, czy wskazują na aktywne zakłócanie połączenia.";
        }

        protected override string GetEventTag()
        {
            return "disassoc";
        }
    }
}