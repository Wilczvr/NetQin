using System.Collections.Generic;
using System.Linq;
using NetQin.DetectionRules;
using NetQin.Models;

namespace NetQin.Services
{
    public class DetectionEngine
    {
        private readonly List<IDetectionRule> _rules;
        private readonly IncidentRiskScoringService _riskScoringService;

        public DetectionEngine()
        {
            _riskScoringService = new IncidentRiskScoringService();

            _rules = new List<IDetectionRule>
            {
                new DeauthFloodRule(),
                new DisassocFloodRule(),
                new BeaconFloodRule(),
                new AuthAssocFloodRule(),
                new EvilTwinRule()
            };
        }

        public List<DetectionIncident> Detect(List<PacketRecord> packets, DetectionSettings settings)
        {
            var incidents = new List<DetectionIncident>();
            var safePackets = packets ?? new List<PacketRecord>();
            var safeSettings = settings ?? new DetectionSettings();

            foreach (var rule in _rules)
            {
                incidents.AddRange(rule.Detect(safePackets, safeSettings));
            }

            _riskScoringService.Apply(incidents);

            return incidents
                .OrderByDescending(i => i.RiskScore)
                .ThenBy(i => i.StartTime)
                .ToList();
        }
    }
}