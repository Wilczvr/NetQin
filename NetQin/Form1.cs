using System;
using System.Drawing;
using System.Windows.Forms;
using SharpPcap;
using PacketDotNet;

namespace NetQin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnLoadPcap_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki PCAP (*.pcap;*.pcapng)|*.pcap;*.pcapng|Wszystkie pliki (*.*)|*.*";
            openFileDialog.Title = "Wybierz zrzut ruchu sieciowego";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog.FileName;

                textBox1.Text = selectedFilePath;

                rtbLogs.SelectionColor = Color.Lime;
                rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] INFO: Rozpoczęto analizę pliku: {selectedFilePath}\n");

                try
                {
                    using (var device = new SharpPcap.LibPcap.CaptureFileReaderDevice(selectedFilePath))
                    {
                        device.Open();
                        int packetCount = 0;
                        int deauthCount = 0; 

                        // Czyścimy tabelę przed załadowaniem nowego pliku
                        dgvPackets.Rows.Clear();

                        // Obsługa pojedynczego pakietu
                        device.OnPacketArrival += (s, args) =>
                        {
                            packetCount++;

                            var rawPacket = args.GetPacket();
                            string time = rawPacket.Timeval.Date.ToString("HH:mm:ss.fff");
                            int length = rawPacket.Data.Length;

                            string macSrc = "?";
                            string macDst = "?";
                            string info = $"Rozmiar: {length} B";

                            try
                            {
                                var parsedPacket = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                                var wifiFrame = parsedPacket.Extract<PacketDotNet.Ieee80211.MacFrame>();

                                if (wifiFrame != null)
                                {
                                    info = $"Wi-Fi: {wifiFrame.FrameControl.Type} - {wifiFrame.FrameControl.SubType}";

                                    if (wifiFrame is PacketDotNet.Ieee80211.ManagementFrame mgmtFrame)
                                    {
                                        macSrc = mgmtFrame.SourceAddress?.ToString();
                                        macDst = mgmtFrame.DestinationAddress?.ToString();
                                    }
                                    else if (wifiFrame is PacketDotNet.Ieee80211.DataFrame dataFrame)
                                    {
                                        macSrc = dataFrame.SourceAddress?.ToString();
                                        macDst = dataFrame.DestinationAddress?.ToString();
                                    }
                                }
                                else
                                {
                                    var ethFrame = parsedPacket.Extract<PacketDotNet.EthernetPacket>();
                                    if (ethFrame != null)
                                    {
                                        macSrc = ethFrame.SourceHardwareAddress?.ToString();
                                        macDst = ethFrame.DestinationHardwareAddress?.ToString();
                                        info = "Ethernet";
                                    }
                                }
                            }
                            catch
                            {
                                // Ignorujemy uszkodzone pakiety
                            }

                            
                            this.Invoke((MethodInvoker)delegate
                            {
                                // Dodajemy wiersz i pobieramy jego indeks
                                int rowIndex = dgvPackets.Rows.Add(packetCount, time, macSrc, macDst, info);

                                
                                if (info.Contains("Deauth"))
                                {
                                    deauthCount++;

                                    // Pokolorowanie na czerwono
                                    dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(60, 20, 20);
                                    dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;

                                    // Czerwony log na dole
                                    rtbLogs.SelectionColor = Color.Red;
                                    rtbLogs.AppendText($"[{time}] CRITICAL: Wykryto atak! Nadawca: {macSrc}\n");
                                }
                            });
                        };

                        device.Capture();

                        
                        rtbLogs.SelectionColor = Color.Lime;
                        rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] SUCCESS: Załadowano {packetCount} pakietów. Wykryto {deauthCount} ataków!\n");
                    }
                }
                catch (Exception ex)
                {
                    rtbLogs.SelectionColor = Color.Red;
                    rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR: Nie udało się otworzyć pliku PCAP. ({ex.Message})\n");
                }
            }
        }
    }
}