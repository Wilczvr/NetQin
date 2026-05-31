using System;
using System.Collections.Generic;
using System.Linq;
using NetQin.Models;
using NetQin.Services;

namespace NetQin.DetectionRules
{
    public abstract class BurstDetectionRuleBase : IDetectionRule
    {
        public abstract string RuleId { get; }
        public abstract string RuleName { get; }

        protected abstract bool Matches(PacketRecord packet);
        protected abstract int GetThreshold(DetectionSettings settings);
        protected abstract int GetWindowSeconds(DetectionSettings settings);
        protected abstract string GetEventLabel();

        public virtual List<DetectionIncident> Detect(List<PacketRecord> packets, DetectionSettings settings)
        {
            var safePackets = packets ?? new List<PacketRecord>();
            var safeSettings = settings ?? new DetectionSettings();

            var candidates = safePackets
                .Where(Matches)
                .OrderBy(p => p.Timestamp)
                .ToList();

            return DetectBurst(
                candidates,
                Math.Max(1, GetThreshold(safeSettings)),
                Math.Max(0, GetWindowSeconds(safeSettings)));
        }

        protected virtual List<DetectionIncident> DetectBurst(List<PacketRecord> packets, int threshold, int windowSeconds)
        {
            var incidents = new List<DetectionIncident>();
            var windows = new Dictionary<string, Queue<PacketRecord>>();
            var activeIncidents = new Dictionary<string, DetectionIncident>();

            foreach (var packet in packets)
            {
                var sourceKey = BuildWindowKey(packet);
                Queue<PacketRecord> queue;

                if (!windows.TryGetValue(sourceKey, out queue))
                {
                    queue = new Queue<PacketRecord>();
                    windows[sourceKey] = queue;
                }

                queue.Enqueue(packet);

                while (queue.Count > 0 &&
                       (packet.Timestamp - queue.Peek().Timestamp).TotalSeconds > windowSeconds)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= threshold)
                {
                    packet.IsSuspiciousBurst = true;

                    foreach (var queuedPacket in queue)
                    {
                        queuedPacket.IsSuspiciousBurst = true;
                    }

                    DetectionIncident activeIncident;
                    if (!activeIncidents.TryGetValue(sourceKey, out activeIncident))
                    {
                        var burstPackets = queue.ToList();
                        activeIncident = BuildIncident(packet, burstPackets, threshold, windowSeconds);
                        activeIncidents[sourceKey] = activeIncident;
                        incidents.Add(activeIncident);
                    }
                    else
                    {
                        activeIncident.EndTime = packet.Timestamp;
                        activeIncident.PacketCount++;
                    }
                }
                else
                {
                    activeIncidents.Remove(sourceKey);
                }
            }

            return incidents;
        }

        protected virtual DetectionIncident BuildIncident(PacketRecord packet, List<PacketRecord> burstPackets, int threshold, int windowSeconds)
        {
            var incident = new DetectionIncident
            {
                RuleId = RuleId,
                Title = RuleName,
                SourceMac = packet.SourceMac,
                TargetMac = packet.DestinationMac,
                Bssid = FirstKnownValue(burstPackets.Select(p => p.Bssid)),
                Ssid = FirstKnownValue(burstPackets.Select(p => p.Ssid)),
                ReasonCode = FirstKnownValue(burstPackets.Select(p => p.ReasonCode)),
                StartTime = burstPackets.First().Timestamp,
                EndTime = burstPackets.Last().Timestamp,
                PacketCount = burstPackets.Count,
                Description = string.Format(
                    "Wykryto co najmniej {0} ramek {1} w ciągu {2} s od źródła {3}. SSID: {4}, BSSID: {5}.",
                    threshold,
                    GetEventLabel(),
                    windowSeconds,
                    Normalize(packet.SourceMac),
                    FirstKnownValue(burstPackets.Select(p => p.Ssid)),
                    FirstKnownValue(burstPackets.Select(p => p.Bssid)))
            };

            incident.RiskScore = GetBaseRiskScore();
            incident.RiskLevel = IncidentRiskScoringService.MapRiskLevel(incident.RiskScore);
            incident.Recommendation = GetRecommendation();
            incident.Tags.Add(GetEventTag());

            return incident;
        }

        protected virtual int GetBaseRiskScore()
        {
            return 35;
        }

        protected virtual string GetRecommendation()
        {
            return "Zweryfikować źródło ramek i porównać incydent z normalnym profilem ruchu w badanym środowisku.";
        }

        protected virtual string GetEventTag()
        {
            return "wifi-anomaly";
        }

        protected static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "?"
                ? "Nieznany"
                : value.Trim();
        }

        protected static string FirstKnownValue(IEnumerable<string> values)
        {
            var first = values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && v != "?");
            return string.IsNullOrWhiteSpace(first) ? "?" : first;
        }

        private static string BuildWindowKey(PacketRecord packet)
        {
            if (IsKnown(packet.SourceMac))
                return "SOURCE|" + packet.SourceMac.Trim().ToUpperInvariant();

            if (IsKnown(packet.Bssid))
                return "BSSID|" + packet.Bssid.Trim().ToUpperInvariant();

            return "PACKET|" + packet.Number;
        }

        private static bool IsKnown(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value != "?";
        }
    }
}
