using System;

namespace NetQin.Models
{
    public class PacketRecord
    {
        public int Number { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceMac { get; set; } = "?";
        public string DestinationMac { get; set; } = "?";
        public string Bssid { get; set; } = "?";
        public string Ssid { get; set; } = "?";
        public string ReasonCode { get; set; } = "?";
        public string FrameType { get; set; } = "";
        public string FrameSubtype { get; set; } = "";
        public int? Channel { get; set; }
        public string Info { get; set; } = "";
        public int Length { get; set; }

        public bool IsBeacon { get; set; }
        public bool IsAuthentication { get; set; }
        public bool IsAssociationRequest { get; set; }
        public bool IsAssociationResponse { get; set; }
        public bool IsDeauth { get; set; }
        public bool IsDisassoc { get; set; }
        public bool IsSuspiciousBurst { get; set; }
        public bool ParseError { get; set; }
    }
}