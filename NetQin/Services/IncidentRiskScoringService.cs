using System;
using System.Collections.Generic;
using System.Linq;
using NetQin.Models;

namespace NetQin.Services
{
    public class IncidentRiskScoringService
    {
        public void Apply(List<DetectionIncident> incidents)
        {
            if (incidents == null)
                return;

            foreach (var incident in incidents)
            {
                Apply(incident);
            }
        }

        public void Apply(DetectionIncident incident)
        {
            if (incident == null)
                return;

            if (incident.RiskScore <= 0)
            {
                incident.RiskScore = GetDefaultScore(incident.RuleId);
            }

            incident.RiskScore = Clamp(incident.RiskScore, 0, 100);
            incident.RiskLevel = MapRiskLevel(incident.RiskScore);

            EnsureDefaultTags(incident);

            if (string.IsNullOrWhiteSpace(incident.Recommendation))
            {
                incident.Recommendation = BuildRecommendation(incident);
            }
        }

        public static RiskLevel MapRiskLevel(int riskScore)
        {
            if (riskScore >= 75)
                return RiskLevel.Critical;
            if (riskScore >= 50)
                return RiskLevel.High;
            if (riskScore >= 25)
                return RiskLevel.Medium;

            return RiskLevel.Low;
        }

        private static int GetDefaultScore(string ruleId)
        {
            switch ((ruleId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "DEAUTH_BURST":
                    return 70;
                case "DISASSOC_BURST":
                    return 60;
                case "AUTH_ASSOC_BURST":
                    return 55;
                case "BEACON_BURST":
                    return 20;
                case "EVIL_TWIN_HEURISTIC":
                    return 65;
                default:
                    return 20;
            }
        }

        private static void EnsureDefaultTags(DetectionIncident incident)
        {
            if (incident.Tags == null)
            {
                incident.Tags = new List<string>();
            }

            AddTag(incident, "wifi");
            AddTag(incident, "802.11");

            switch ((incident.RuleId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "DEAUTH_BURST":
                    AddTag(incident, "deauth");
                    AddTag(incident, "disconnect");
                    break;
                case "DISASSOC_BURST":
                    AddTag(incident, "disassoc");
                    AddTag(incident, "disconnect");
                    break;
                case "AUTH_ASSOC_BURST":
                    AddTag(incident, "auth");
                    AddTag(incident, "association");
                    break;
                case "BEACON_BURST":
                    AddTag(incident, "beacon");
                    AddTag(incident, "ap-advertising");
                    break;
                case "EVIL_TWIN_HEURISTIC":
                    AddTag(incident, "evil-twin");
                    AddTag(incident, "ssid-spoofing");
                    break;
            }
        }

        private static string BuildRecommendation(DetectionIncident incident)
        {
            switch ((incident.RuleId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "DEAUTH_BURST":
                    return "Zweryfikować źródło ramek deautoryzacji, sprawdzić legalność AP oraz zachowanie klientów w analizowanym przedziale czasu.";
                case "DISASSOC_BURST":
                    return "Sprawdzić, czy rozłączenia wynikają z prawidłowej pracy AP, czy z prób wymuszania odłączeń klientów.";
                case "AUTH_ASSOC_BURST":
                    return "Porównać wzrost ramek authentication/association z logami AP i sprawdzić, czy nie występuje wymuszony reconnect klientów.";
                case "BEACON_BURST":
                    return "Zweryfikować liczbę reklamowanych beaconów dla danego SSID/BSSID i porównać ją z normalnym profilem środowiska.";
                case "EVIL_TWIN_HEURISTIC":
                    return "Zweryfikować legalność wykrytego BSSID, porównać go z inwentaryzacją AP oraz sprawdzić, czy podobny SSID nie pojawił się nagle na innym kanale.";
                default:
                    return "Przeanalizować kontekst incydentu i porównać go z normalnym profilem ruchu w badanym środowisku.";
            }
        }

        private static void AddTag(DetectionIncident incident, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (!incident.Tags.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase)))
            {
                incident.Tags.Add(tag);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }
    }
}
