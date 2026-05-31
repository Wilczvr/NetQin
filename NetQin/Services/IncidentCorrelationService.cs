using System;
using System.Collections.Generic;
using System.Linq;
using NetQin.Models;

namespace NetQin.Services
{
    public class IncidentCorrelationService
    {
        private readonly int _correlationWindowSeconds;

        public IncidentCorrelationService(int correlationWindowSeconds = 30)
        {
            _correlationWindowSeconds = correlationWindowSeconds;
        }

        public List<CorrelatedIncident> Correlate(List<DetectionIncident> incidents)
        {
            var safeIncidents = incidents ?? new List<DetectionIncident>();
            var result = new List<CorrelatedIncident>();

            result.AddRange(CorrelateForcedReconnect(safeIncidents));
            result.AddRange(CorrelateFakeApCampaign(safeIncidents));

            return result
                .GroupBy(BuildIdentity, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(i => i.RiskScore).First())
                .OrderByDescending(i => i.RiskScore)
                .ThenBy(i => i.StartTime)
                .ToList();
        }

        private IEnumerable<CorrelatedIncident> CorrelateForcedReconnect(List<DetectionIncident> incidents)
        {
            var result = new List<CorrelatedIncident>();

            var disconnectIncidents = incidents
                .Where(i => i != null && (i.RuleId == "DEAUTH_BURST" || i.RuleId == "DISASSOC_BURST"))
                .ToList();

            var reconnectIncidents = incidents
                .Where(i => i != null && i.RuleId == "AUTH_ASSOC_BURST")
                .ToList();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var disconnect in disconnectIncidents)
            {
                foreach (var reconnect in reconnectIncidents)
                {
                    if (!IsReconnectAfterDisconnect(disconnect, reconnect))
                        continue;

                    if (!SharesReconnectContext(disconnect, reconnect))
                        continue;

                    string key = string.Format(
                        "FORCED_RECONNECT|{0}|{1}|{2}|{3}",
                        FirstKnown(disconnect.Ssid, reconnect.Ssid),
                        FirstKnown(disconnect.Bssid, reconnect.Bssid),
                        disconnect.StartTime.Ticks,
                        reconnect.StartTime.Ticks);

                    if (!seen.Add(key))
                        continue;

                    int riskScore = Math.Max(disconnect.RiskScore, reconnect.RiskScore) + 15;

                    if (SameKnown(disconnect.Ssid, reconnect.Ssid))
                        riskScore += 5;

                    if (SameKnown(disconnect.Bssid, reconnect.Bssid))
                        riskScore += 5;

                    riskScore = Math.Min(100, riskScore);

                    var correlated = new CorrelatedIncident
                    {
                        CorrelationId = Guid.NewGuid().ToString("N"),
                        CorrelationType = "FORCED_RECONNECT",
                        Title = "Możliwe wymuszenie reconnectu klienta",
                        Description =
                            "W krótkim odstępie czasu wykryto incydent rozłączający oraz wzrost ramek authentication/association. " +
                            "Taki układ może wskazywać na próbę wymuszenia ponownego połączenia klienta.",
                        Ssid = FirstKnown(disconnect.Ssid, reconnect.Ssid),
                        Bssid = FirstKnown(disconnect.Bssid, reconnect.Bssid),
                        SourceMac = FirstKnown(disconnect.SourceMac, reconnect.SourceMac),
                        StartTime = Min(disconnect.StartTime, reconnect.StartTime),
                        EndTime = Max(disconnect.EndTime, reconnect.EndTime),
                        RiskScore = riskScore,
                        RiskLevel = IncidentRiskScoringService.MapRiskLevel(riskScore),
                        Recommendation =
                            "Zweryfikować, czy seria ramek deauth/disassoc nie poprzedza wymuszonego reconnectu klientów oraz porównać zdarzenia z logami punktu dostępowego.",
                        SourceIncidents = new List<DetectionIncident> { disconnect, reconnect },
                        Tags = new List<string>
                        {
                            "correlation",
                            "forced-reconnect",
                            "wifi",
                            "802.11"
                        }
                    };

                    result.Add(correlated);
                }
            }

            return result;
        }

        private IEnumerable<CorrelatedIncident> CorrelateFakeApCampaign(List<DetectionIncident> incidents)
        {
            var result = new List<CorrelatedIncident>();

            var beaconIncidents = incidents
                .Where(i => i != null && i.RuleId == "BEACON_BURST")
                .ToList();

            var evilTwinIncidents = incidents
                .Where(i => i != null && i.RuleId == "EVIL_TWIN_HEURISTIC")
                .ToList();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var beacon in beaconIncidents)
            {
                foreach (var evilTwin in evilTwinIncidents)
                {
                    if (!AreWithinWindow(beacon, evilTwin))
                        continue;

                    if (!SameKnown(beacon.Ssid, evilTwin.Ssid) ||
                        !SharesFakeApContext(beacon, evilTwin))
                        continue;

                    string key = string.Format(
                        "FAKE_AP_CAMPAIGN|{0}|{1}|{2}|{3}",
                        FirstKnown(beacon.Ssid, evilTwin.Ssid),
                        FirstKnown(beacon.Bssid, evilTwin.Bssid),
                        beacon.StartTime.Ticks,
                        evilTwin.StartTime.Ticks);

                    if (!seen.Add(key))
                        continue;

                    int riskScore = Math.Max(beacon.RiskScore, evilTwin.RiskScore) + 20;

                    if (SameKnown(beacon.Bssid, evilTwin.Bssid))
                        riskScore += 5;

                    riskScore = Math.Min(100, riskScore);

                    var correlated = new CorrelatedIncident
                    {
                        CorrelationId = Guid.NewGuid().ToString("N"),
                        CorrelationType = "FAKE_AP_CAMPAIGN",
                        Title = "Możliwa kampania fałszywego AP",
                        Description =
                            "Wykryto jednocześnie nietypową intensywność beaconów oraz heurystykę Evil Twin dla tego samego SSID. " +
                            "Taki zestaw zdarzeń zwiększa prawdopodobieństwo podszywania się pod punkt dostępowy.",
                        Ssid = FirstKnown(beacon.Ssid, evilTwin.Ssid),
                        Bssid = FirstKnown(evilTwin.Bssid, beacon.Bssid),
                        SourceMac = FirstKnown(evilTwin.SourceMac, beacon.SourceMac),
                        StartTime = Min(beacon.StartTime, evilTwin.StartTime),
                        EndTime = Max(beacon.EndTime, evilTwin.EndTime),
                        RiskScore = riskScore,
                        RiskLevel = IncidentRiskScoringService.MapRiskLevel(riskScore),
                        Recommendation =
                            "Zweryfikować legalność BSSID, porównać SSID z inwentaryzacją AP oraz sprawdzić, czy dodatkowa reklama beaconów nie pochodzi z nieautoryzowanego źródła.",
                        SourceIncidents = new List<DetectionIncident> { beacon, evilTwin },
                        Tags = new List<string>
                        {
                            "correlation",
                            "fake-ap",
                            "evil-twin",
                            "wifi",
                            "802.11"
                        }
                    };

                    result.Add(correlated);
                }
            }

            return result;
        }

        private bool AreWithinWindow(DetectionIncident first, DetectionIncident second)
        {
            if (first == null || second == null)
                return false;

            bool overlaps = first.StartTime <= second.EndTime && second.StartTime <= first.EndTime;
            if (overlaps)
                return true;

            DateTime earlierEnd = first.EndTime <= second.EndTime ? first.EndTime : second.EndTime;
            DateTime laterStart = first.StartTime >= second.StartTime ? first.StartTime : second.StartTime;

            return Math.Abs((laterStart - earlierEnd).TotalSeconds) <= _correlationWindowSeconds;
        }

        private bool IsReconnectAfterDisconnect(DetectionIncident disconnect, DetectionIncident reconnect)
        {
            if (disconnect == null || reconnect == null)
                return false;

            if (reconnect.StartTime < disconnect.StartTime)
                return false;

            DateTime comparisonPoint = reconnect.StartTime <= disconnect.EndTime
                ? disconnect.EndTime
                : reconnect.StartTime;

            return (comparisonPoint - disconnect.EndTime).TotalSeconds <= _correlationWindowSeconds;
        }

        private static bool SharesReconnectContext(DetectionIncident disconnect, DetectionIncident reconnect)
        {
            bool sharesClient =
                SameKnown(disconnect.TargetMac, reconnect.SourceMac) ||
                SameKnown(disconnect.TargetMac, reconnect.TargetMac);

            bool sharesBssid =
                SameKnown(disconnect.Bssid, reconnect.Bssid) ||
                SameKnown(disconnect.SourceMac, reconnect.Bssid) ||
                SameKnown(disconnect.Bssid, reconnect.TargetMac);

            return sharesClient || sharesBssid;
        }

        private static bool SharesFakeApContext(DetectionIncident beacon, DetectionIncident evilTwin)
        {
            return SameKnown(beacon.Bssid, evilTwin.Bssid) ||
                   SameKnown(beacon.SourceMac, evilTwin.Bssid);
        }

        private static string BuildIdentity(CorrelatedIncident incident)
        {
            return string.Format(
                "{0}|{1}|{2}|{3}|{4}",
                incident.CorrelationType ?? "",
                incident.Ssid ?? "",
                incident.Bssid ?? "",
                incident.StartTime.Ticks,
                incident.EndTime.Ticks);
        }

        private static string FirstKnown(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value) && value != "?")
                    return value;
            }

            return "?";
        }

        private static bool SameKnown(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) &&
                   !string.IsNullOrWhiteSpace(second) &&
                   first != "?" &&
                   second != "?" &&
                   string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime Min(DateTime first, DateTime second)
        {
            return first <= second ? first : second;
        }

        private static DateTime Max(DateTime first, DateTime second)
        {
            return first >= second ? first : second;
        }
    }
}
