using System;
using System.Collections.Generic;
using System.Linq;
using NetQin.Models;
using NetQin.Services;

namespace NetQin.DetectionRules
{
    public class EvilTwinRule : IDetectionRule
    {
        public string RuleId
        {
            get { return "EVIL_TWIN_HEURISTIC"; }
        }

        public string RuleName
        {
            get { return "Heurystycznie wykryty możliwy Evil Twin"; }
        }

        public List<DetectionIncident> Detect(List<PacketRecord> packets, DetectionSettings settings)
        {
            var incidents = new List<DetectionIncident>();
            var safeSettings = settings ?? new DetectionSettings();

            if (!safeSettings.EnableEvilTwinHeuristic)
                return incidents;

            int minDistinctBssids = Math.Max(2, safeSettings.EvilTwinMinDistinctBssidsPerSsid);
            int minBeaconsPerBssid = Math.Max(1, safeSettings.EvilTwinMinBeaconsPerBssid);
            int minBaselineLeadSeconds = Math.Max(0, safeSettings.EvilTwinMinBaselineLeadSeconds);
            int rapidAppearanceWindowSeconds = Math.Max(minBaselineLeadSeconds, safeSettings.EvilTwinRapidAppearanceWindowSeconds);

            var beaconPackets = (packets ?? new List<PacketRecord>())
                .Where(p => p != null &&
                            p.IsBeacon &&
                            IsKnownSsid(p.Ssid, safeSettings.IgnoreHiddenSsidsForEvilTwin) &&
                            IsKnown(p.Bssid))
                .OrderBy(p => p.Timestamp)
                .ToList();

            foreach (var ssidGroup in beaconPackets.GroupBy(p => p.Ssid.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var observations = ssidGroup
                    .GroupBy(p => NormalizeMac(p.Bssid), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new BssidObservation(g.Key, g.OrderBy(p => p.Timestamp).ToList()))
                    .Where(o => o.Packets.Count >= minBeaconsPerBssid)
                    .OrderBy(o => o.FirstSeen)
                    .ToList();

                if (observations.Count < minDistinctBssids)
                    continue;

                var baseline = observations
                    .OrderBy(o => o.FirstSeen)
                    .ThenByDescending(o => o.Packets.Count)
                    .First();

                var globalDominantChannel = observations
                    .Where(o => o.PrimaryChannel.HasValue)
                    .GroupBy(o => o.PrimaryChannel.Value)
                    .OrderByDescending(g => g.Sum(o => o.Packets.Count))
                    .ThenBy(g => g.Key)
                    .Select(g => (int?)g.Key)
                    .FirstOrDefault();

                int distinctChannelCount = observations
                    .Where(o => o.PrimaryChannel.HasValue)
                    .Select(o => o.PrimaryChannel.Value)
                    .Distinct()
                    .Count();

                foreach (var candidate in observations)
                {
                    if (string.Equals(candidate.Bssid, baseline.Bssid, StringComparison.OrdinalIgnoreCase))
                        continue;

                    int score = 15;
                    var tags = new List<string>
                    {
                        "wifi",
                        "802.11",
                        "evil-twin",
                        "heuristic",
                        "shared-ssid"
                    };

                    double appearanceDelaySeconds = (candidate.FirstSeen - baseline.FirstSeen).TotalSeconds;
                    bool rapidAppearance = appearanceDelaySeconds >= minBaselineLeadSeconds &&
                                           appearanceDelaySeconds <= rapidAppearanceWindowSeconds;
                    if (rapidAppearance)
                    {
                        score += 15;
                        tags.Add("rapid-ap-appearance");
                    }

                    bool channelDiffersFromBaseline = candidate.PrimaryChannel.HasValue && baseline.PrimaryChannel.HasValue && candidate.PrimaryChannel.Value != baseline.PrimaryChannel.Value;
                    bool channelDiffersFromDominant = candidate.PrimaryChannel.HasValue && globalDominantChannel.HasValue && candidate.PrimaryChannel.Value != globalDominantChannel.Value;
                    if (channelDiffersFromBaseline || channelDiffersFromDominant)
                    {
                        score += 15;
                        tags.Add("channel-shift");
                    }

                    if (distinctChannelCount > 1)
                    {
                        score += 10;
                    }

                    if (candidate.Packets.Count >= minBeaconsPerBssid * 2)
                    {
                        score += 10;
                        tags.Add("beacon-presence");
                    }

                    bool channelDifference = channelDiffersFromBaseline || channelDiffersFromDominant;

                    if (!rapidAppearance)
                        continue;

                    if (safeSettings.EvilTwinRequireChannelDifference && !channelDifference)
                        continue;

                    foreach (var packet in candidate.Packets)
                    {
                        packet.IsSuspiciousBurst = true;
                    }

                    var incident = new DetectionIncident
                    {
                        RuleId = RuleId,
                        Title = string.Format("Możliwy Evil Twin dla SSID \"{0}\"", ssidGroup.Key),
                        SourceMac = candidate.PrimarySourceMac,
                        TargetMac = baseline.Bssid,
                        Bssid = candidate.Bssid,
                        Ssid = ssidGroup.Key,
                        StartTime = candidate.FirstSeen,
                        EndTime = candidate.LastSeen,
                        PacketCount = candidate.Packets.Count,
                        RiskScore = Math.Min(score, 100),
                        Recommendation = "Zweryfikować legalność nowego BSSID, porównać go z inwentaryzacją AP oraz sprawdzić, czy SSID nie został sklonowany na innym kanale.",
                        Description = BuildDescription(ssidGroup.Key, baseline, candidate, rapidAppearance, channelDifference, distinctChannelCount)
                    };

                    incident.Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    incident.RiskLevel = IncidentRiskScoringService.MapRiskLevel(incident.RiskScore);
                    incidents.Add(incident);
                }
            }

            return incidents;
        }

        private static string BuildDescription(string ssid, BssidObservation baseline, BssidObservation candidate, bool rapidAppearance, bool channelDifference, int distinctChannelCount)
        {
            string baselineChannel = baseline.PrimaryChannel.HasValue ? baseline.PrimaryChannel.Value.ToString() : "?";
            string candidateChannel = candidate.PrimaryChannel.HasValue ? candidate.PrimaryChannel.Value.ToString() : "?";

            var reasons = new List<string>
            {
                string.Format("ten sam SSID \"{0}\" jest reklamowany przez wiele BSSID", ssid)
            };

            if (rapidAppearance)
            {
                reasons.Add(string.Format(
                    "nowy BSSID {0} pojawił się {1:0.0} s względem bazowego AP {2}",
                    candidate.Bssid,
                    Math.Abs((candidate.FirstSeen - baseline.FirstSeen).TotalSeconds),
                    baseline.Bssid));
            }

            if (channelDifference)
            {
                reasons.Add(string.Format(
                    "kanał kandydata ({0}) różni się od kanału bazowego AP ({1})",
                    candidateChannel,
                    baselineChannel));
            }

            if (distinctChannelCount > 1)
            {
                reasons.Add(string.Format("dla tego SSID zaobserwowano {0} różne kanały", distinctChannelCount));
            }

            return "Heurystyka Evil Twin: " + string.Join("; ", reasons) + ". Wynik nie stanowi dowodu ataku, ale wskazuje na zwiększone ryzyko podszywania się pod AP.";
        }

        private static bool IsKnown(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value != "?";
        }

        private static bool IsKnownSsid(string value, bool ignoreHiddenSsid)
        {
            return IsKnown(value) &&
                   (!ignoreHiddenSsid ||
                    !string.Equals(value.Trim(), "<hidden>", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeMac(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "?"
                ? "Nieznany"
                : value.Trim().ToUpperInvariant();
        }

        private sealed class BssidObservation
        {
            public BssidObservation(string bssid, List<PacketRecord> packets)
            {
                Bssid = bssid;
                Packets = packets ?? new List<PacketRecord>();
            }

            public string Bssid { get; private set; }
            public List<PacketRecord> Packets { get; private set; }

            public DateTime FirstSeen
            {
                get { return Packets.First().Timestamp; }
            }

            public DateTime LastSeen
            {
                get { return Packets.Last().Timestamp; }
            }

            public int? PrimaryChannel
            {
                get
                {
                    return Packets
                        .Where(p => p.Channel.HasValue)
                        .GroupBy(p => p.Channel.Value)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key)
                        .Select(g => (int?)g.Key)
                        .FirstOrDefault();
                }
            }

            public string PrimarySourceMac
            {
                get
                {
                    return Packets
                        .Select(p => p.SourceMac)
                        .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && v != "?") ?? Bssid;
                }
            }
        }
    }
}
