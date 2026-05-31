using NetQin.Models;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetQin.Services
{
    public class PcapAnalyzerService
    {
        private const int MaxDetailedFrameLogs = 500;
        private const int MaxDetailedParseErrorLogs = 25;
        private static readonly object PropertyCacheLock = new object();
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public AnalysisResult Analyze(string filePath, DetectionSettings settings = null)
        {
            settings = settings ?? new DetectionSettings();

            var result = new AnalysisResult
            {
                FilePath = filePath
            };

            int packetCounter = 0;
            int detailedFrameLogCount = 0;
            int suppressedFrameLogCount = 0;
            int detailedParseErrorLogCount = 0;
            int suppressedParseErrorLogCount = 0;

            using (var device = new CaptureFileReaderDevice(filePath))
            {
                device.Open();

                device.OnPacketArrival += (sender, e) =>
                {
                    packetCounter++;

                    var rawPacket = e.GetPacket();
                    var time = rawPacket.Timeval.Date;
                    var len = rawPacket.Data.Length;

                    var record = new PacketRecord
                    {
                        Number = packetCounter,
                        Timestamp = time,
                        Length = len,
                        SourceMac = "?",
                        DestinationMac = "?",
                        Bssid = "?",
                        Ssid = "?",
                        ReasonCode = "?",
                        FrameType = "",
                        FrameSubtype = "",
                        Channel = null,
                        Info = string.Format("Rozmiar: {0} B", len)
                    };

                    try
                    {
                        var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                        var macFrame = packet.Extract<MacFrame>();

                        if (macFrame != null)
                        {
                            record.FrameType = SafeToString(GetNestedPropertyValue(macFrame, "FrameControl", "Type"));
                            record.FrameSubtype = SafeToString(GetNestedPropertyValue(macFrame, "FrameControl", "SubType"));
                            record.Info = BuildFrameInfo(record.FrameType, record.FrameSubtype, len);

                            PopulateChannel(record, packet, macFrame);

                            if (macFrame is ManagementFrame)
                            {
                                PopulateManagementAddresses(record, macFrame);
                            }
                            else if (macFrame is DataFrame dataFrame)
                            {
                                if (dataFrame.SourceAddress != null)
                                    record.SourceMac = dataFrame.SourceAddress.ToString();

                                if (dataFrame.DestinationAddress != null)
                                    record.DestinationMac = dataFrame.DestinationAddress.ToString();
                            }

                            string detectedBssid = ReadPotentialMac(macFrame, "BssId", "Bssid", "BasicServiceSetId");
                            if (detectedBssid != "?")
                                record.Bssid = detectedBssid;

                            record.Ssid = TryExtractSsid(macFrame);
                            record.ReasonCode = TryExtractReasonCode(macFrame);

                            if (macFrame is BeaconFrame)
                            {
                                record.IsBeacon = true;
                                result.Statistics.BeaconCount++;
                                record.Info = BuildSemanticInfo("Beacon", record);
                            }
                            else if (macFrame is AuthenticationFrame)
                            {
                                record.IsAuthentication = true;
                                result.Statistics.AuthenticationCount++;
                                record.Info = BuildSemanticInfo("Authentication", record);
                            }
                            else if (macFrame is AssociationRequestFrame)
                            {
                                record.IsAssociationRequest = true;
                                result.Statistics.AssociationRequestCount++;
                                record.Info = BuildSemanticInfo("Association Request", record);
                            }
                            else if (macFrame is AssociationResponseFrame)
                            {
                                record.IsAssociationResponse = true;
                                result.Statistics.AssociationResponseCount++;
                                record.Info = BuildSemanticInfo("Association Response", record);
                            }
                            else if (macFrame is DeauthenticationFrame)
                            {
                                record.IsDeauth = true;
                                result.Statistics.DeauthCount++;
                                record.Info = BuildSemanticInfo("Deauthentication", record);
                                AddDetailedFrameLog(
                                    result,
                                    record,
                                    "deauth",
                                    ref detailedFrameLogCount,
                                    ref suppressedFrameLogCount);
                            }
                            else if (macFrame is DisassociationFrame)
                            {
                                record.IsDisassoc = true;
                                result.Statistics.DisassocCount++;
                                record.Info = BuildSemanticInfo("Disassociation", record);
                                AddDetailedFrameLog(
                                    result,
                                    record,
                                    "disassoc",
                                    ref detailedFrameLogCount,
                                    ref suppressedFrameLogCount);
                            }
                        }
                        else
                        {
                            var ethernetPacket = packet.Extract<EthernetPacket>();
                            if (ethernetPacket != null)
                            {
                                record.SourceMac = ethernetPacket.SourceHardwareAddress != null
                                    ? ethernetPacket.SourceHardwareAddress.ToString()
                                    : "?";

                                record.DestinationMac = ethernetPacket.DestinationHardwareAddress != null
                                    ? ethernetPacket.DestinationHardwareAddress.ToString()
                                    : "?";

                                record.Info = string.Format("Ethernet: {0}", ethernetPacket.Type);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        record.ParseError = true;
                        result.Statistics.ParseErrorCount++;
                        record.Info = "Błąd parsowania pakietu";

                        if (detailedParseErrorLogCount < MaxDetailedParseErrorLogs)
                        {
                            result.Logs.Add(new AnalysisLogEntry
                            {
                                Timestamp = record.Timestamp,
                                Level = AnalysisLogLevel.Warning,
                                Message = string.Format(
                                    "Nie udało się zdekodować pakietu #{0}: {1}",
                                    record.Number,
                                    ex.Message)
                            });

                            detailedParseErrorLogCount++;
                        }
                        else
                        {
                            suppressedParseErrorLogCount++;
                        }
                    }

                    result.Packets.Add(record);
                };

                device.Capture();
                device.Close();
            }

            result.Statistics.TotalPackets = result.Packets.Count;

            if (suppressedFrameLogCount > 0)
            {
                result.Logs.Add(new AnalysisLogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = AnalysisLogLevel.Info,
                    Message = string.Format(
                        "Pominięto {0} szczegółowych wpisów deauth/disassoc, aby ograniczyć rozmiar dziennika.",
                        suppressedFrameLogCount)
                });
            }

            if (suppressedParseErrorLogCount > 0)
            {
                result.Logs.Add(new AnalysisLogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = AnalysisLogLevel.Info,
                    Message = string.Format(
                        "Pominięto {0} kolejnych szczegółowych wpisów o błędach parsowania.",
                        suppressedParseErrorLogCount)
                });
            }

            return result;
        }

        private static void AddDetailedFrameLog(
            AnalysisResult result,
            PacketRecord record,
            string frameLabel,
            ref int detailedLogCount,
            ref int suppressedLogCount)
        {
            if (detailedLogCount >= MaxDetailedFrameLogs)
            {
                suppressedLogCount++;
                return;
            }

            result.Logs.Add(new AnalysisLogEntry
            {
                Timestamp = record.Timestamp,
                Level = AnalysisLogLevel.Notice,
                Message = string.Format(
                    "Wykryto ramkę {0} od {1} do {2} (BSSID: {3}, reason: {4}).",
                    frameLabel,
                    record.SourceMac,
                    record.DestinationMac,
                    record.Bssid,
                    record.ReasonCode)
            });

            detailedLogCount++;
        }

        private static void PopulateManagementAddresses(PacketRecord record, MacFrame macFrame)
        {
            var sourceAddress = GetPropertyValue(macFrame, "SourceAddress");
            var destinationAddress = GetPropertyValue(macFrame, "DestinationAddress");
            var bssId = GetPropertyValue(macFrame, "BssId") ?? GetPropertyValue(macFrame, "Bssid");

            if (sourceAddress != null)
                record.SourceMac = sourceAddress.ToString();

            if (destinationAddress != null)
                record.DestinationMac = destinationAddress.ToString();

            if (bssId != null)
                record.Bssid = bssId.ToString();
        }

        private static void PopulateChannel(PacketRecord record, object packet, MacFrame macFrame)
        {
            record.Channel = TryGetChannel(packet)
                ?? TryGetChannel(macFrame)
                ?? TryParseChannelFromFrequency(packet)
                ?? TryParseChannelFromFrequency(macFrame);
        }

        private static int? TryGetChannel(object instance)
        {
            if (instance == null)
                return null;

            var value = GetPropertyValue(instance, "Channel");
            if (value == null)
                return null;

            int parsed;
            if (int.TryParse(value.ToString(), out parsed))
                return parsed;

            return null;
        }

        private static int? TryParseChannelFromFrequency(object instance)
        {
            if (instance == null)
                return null;

            var value = GetPropertyValue(instance, "ChannelFrequency")
                ?? GetPropertyValue(instance, "Frequency")
                ?? GetPropertyValue(instance, "CenterFrequency");

            if (value == null)
                return null;

            int mhz;
            if (!int.TryParse(value.ToString(), out mhz))
                return null;

            if (mhz == 2484)
                return 14;

            if (mhz >= 2412 && mhz <= 2472)
                return (mhz - 2407) / 5;

            if (mhz >= 5000 && mhz <= 5900)
                return (mhz - 5000) / 5;

            return null;
        }

        private static string ReadPotentialMac(object instance, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var value = GetPropertyValue(instance, propertyName);
                if (value != null)
                    return value.ToString();
            }

            return "?";
        }

        private static string TryExtractReasonCode(object frame)
        {
            var value = GetPropertyValue(frame, "ReasonCode")
                ?? GetNestedPropertyValue(frame, "ReasonCodeInformation", "ReasonCode")
                ?? GetNestedPropertyValue(frame, "ReasonCodeInformation", "Value");

            if (value == null)
                return "?";

            return SafeToString(value);
        }

        private static string TryExtractSsid(object frame)
        {
            var directValue = GetPropertyValue(frame, "Ssid")
                ?? GetPropertyValue(frame, "SSID")
                ?? GetNestedPropertyValue(frame, "SsidInformationElement", "SSID")
                ?? GetNestedPropertyValue(frame, "SsidInformationElement", "Ssid")
                ?? GetNestedPropertyValue(frame, "SsidInformationElement", "Value");

            var directString = NormalizeSsidValue(directValue);
            if (directString != "?")
                return directString;

            var informationElements = GetPropertyValue(frame, "InformationElements") as IEnumerable;
            if (informationElements == null)
                return "?";

            foreach (var element in informationElements)
            {
                var idValue = GetPropertyValue(element, "ElementId") ?? GetPropertyValue(element, "Id");
                var idText = idValue != null ? idValue.ToString() : string.Empty;

                if (string.Equals(idText, "ServiceSetIdentity", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(idText, "SSID", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(idText, "0", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = NormalizeSsidValue(
                        GetPropertyValue(element, "Value")
                        ?? GetPropertyValue(element, "Bytes")
                        ?? GetPropertyValue(element, "Data")
                        ?? GetPropertyValue(element, "Information"));

                    if (candidate != "?")
                        return candidate;
                }
            }

            return "?";
        }

        private static string NormalizeSsidValue(object value)
        {
            if (value == null)
                return "?";

            var bytes = value as byte[];
            if (bytes != null)
            {
                var text = Encoding.UTF8.GetString(bytes).Trim('\0', ' ');
                return string.IsNullOrWhiteSpace(text) ? "<hidden>" : text;
            }

            var textValue = value.ToString();
            if (string.IsNullOrWhiteSpace(textValue))
                return "?";

            return textValue;
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = GetCachedProperty(instance.GetType(), propertyName);
            if (property == null)
                return null;

            try
            {
                return property.GetValue(instance, null);
            }
            catch
            {
                return null;
            }
        }

        private static PropertyInfo GetCachedProperty(Type type, string propertyName)
        {
            lock (PropertyCacheLock)
            {
                Dictionary<string, PropertyInfo> properties;
                if (!PropertyCache.TryGetValue(type, out properties))
                {
                    properties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
                    PropertyCache[type] = properties;
                }

                PropertyInfo property;
                if (!properties.TryGetValue(propertyName, out property))
                {
                    property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    properties[propertyName] = property;
                }

                return property;
            }
        }

        private static object GetNestedPropertyValue(object instance, params string[] propertyPath)
        {
            object current = instance;

            foreach (var propertyName in propertyPath)
            {
                current = GetPropertyValue(current, propertyName);
                if (current == null)
                    return null;
            }

            return current;
        }

        private static string SafeToString(object value)
        {
            return value == null ? string.Empty : value.ToString();
        }

        private static string BuildFrameInfo(string frameType, string frameSubtype, int length)
        {
            if (!string.IsNullOrWhiteSpace(frameType) || !string.IsNullOrWhiteSpace(frameSubtype))
            {
                return string.Format("{0} / {1}", frameType, frameSubtype).Trim(' ', '/');
            }

            return string.Format("Rozmiar: {0} B", length);
        }

        private static string BuildSemanticInfo(string label, PacketRecord record)
        {
            var parts = new[]
            {
                label,
                record.Ssid != "?" ? "SSID: " + record.Ssid : null,
                record.Bssid != "?" ? "BSSID: " + record.Bssid : null,
                record.Channel.HasValue ? "Kanał: " + record.Channel.Value : null,
                record.ReasonCode != "?" ? "Reason: " + record.ReasonCode : null
            }
            .Where(p => !string.IsNullOrWhiteSpace(p));

            return string.Join(" | ", parts);
        }
    }
}
