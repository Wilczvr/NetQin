using System.Collections.Generic;
using NetQin.Models;

namespace NetQin.DetectionRules
{
    public interface IDetectionRule
    {
        string RuleId { get; }
        string RuleName { get; }
        List<DetectionIncident> Detect(List<PacketRecord> packets, DetectionSettings settings);
    }
}