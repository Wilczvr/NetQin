using System;
using System.Collections.Generic;
using WinColor = System.Drawing.Color;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpPcap;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace NetQin
{
    public partial class Form1 : Form
    {
        private string currentFilePath = string.Empty;
        private int lastPacketCount = 0;
        private int lastDeauthCount = 0;
        private int lastParseErrorCount = 0;
        private int lastSuspiciousBurstCount = 0;
        private bool isAnalysisRunning = false;
        private readonly ToolTip fileNameToolTip = new ToolTip();

        public Form1()
        {
            InitializeComponent();

            textBox1.ReadOnly = true;

            dgvPackets.ReadOnly = true;
            dgvPackets.AllowUserToAddRows = false;
            dgvPackets.AllowUserToDeleteRows = false;
            dgvPackets.AllowUserToResizeRows = false;
            dgvPackets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPackets.MultiSelect = false;
            dgvPackets.RowHeadersVisible = false;
            dgvPackets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPackets.RowTemplate.Height = 34;

            btnExportReport.Enabled = false;

            ConfigurePremiumUi();
            UpdateDashboard();
        }

        private void ConfigurePremiumUi()
        {
            fileNameToolTip.InitialDelay = 150;
            fileNameToolTip.ReshowDelay = 100;
            fileNameToolTip.AutoPopDelay = 8000;
            fileNameToolTip.ShowAlways = true;
            fileNameToolTip.SetToolTip(textBox1, "Brak wczytanego pliku");

            lblPacketCount.Font = new System.Drawing.Font("Segoe UI Semibold", 27F, System.Drawing.FontStyle.Bold);
            lblDeauthCount.Font = new System.Drawing.Font("Segoe UI Semibold", 27F, System.Drawing.FontStyle.Bold);
            lblSuspiciousCount.Font = new System.Drawing.Font("Segoe UI Semibold", 27F, System.Drawing.FontStyle.Bold);

            lblStatusValue.Font = new System.Drawing.Font("Segoe UI Semibold", 22F, System.Drawing.FontStyle.Bold);
            lblStatusDescription.MaximumSize = new System.Drawing.Size(320, 0);

            rtbLogs.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular);

            Column1.FillWeight = 12F;
            Column2.FillWeight = 18F;
            Column3.FillWeight = 24F;
            Column4.FillWeight = 24F;
            Column5.FillWeight = 62F;

            lblAppTitle.Text = "NetQin";
            lblSubtitle.Text = "Wi-Fi Threat Analysis Console";
        }

        private async void btnLoadPcap_Click(object sender, EventArgs e)
        {
            if (isAnalysisRunning)
            {
                LogMessage("WARN", "Analiza już trwa.", WinColor.Orange);
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Pliki PCAP (*.pcap;*.pcapng)|*.pcap;*.pcapng|Wszystkie pliki (*.*)|*.*";
                openFileDialog.Title = "Wybierz zrzut ruchu sieciowego";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                currentFilePath = openFileDialog.FileName;
                textBox1.Text = Path.GetFileName(currentFilePath);
                fileNameToolTip.SetToolTip(textBox1, currentFilePath);

                ResetView();
                LogMessage("INFO", $"Rozpoczęto analizę pliku: {currentFilePath}", WinColor.Lime);

                isAnalysisRunning = true;
                btnLoadPcap.Enabled = false;
                btnExportReport.Enabled = false;

                try
                {
                    await Task.Run(() => AnalyzePcap(currentFilePath));

                    LogMessage(
                        "SUCCESS",
                        $"Załadowano {lastPacketCount} pakietów. Ramki deautoryzacji: {lastDeauthCount}. Podejrzane serie: {lastSuspiciousBurstCount}. Błędy parsowania: {lastParseErrorCount}.",
                        WinColor.Lime
                    );
                }
                catch (Exception ex)
                {
                    LogMessage("ERROR", $"Nie udało się przeanalizować pliku PCAP. ({ex.Message})", WinColor.Red);
                }
                finally
                {
                    isAnalysisRunning = false;
                    btnLoadPcap.Enabled = true;
                    btnExportReport.Enabled = !string.IsNullOrWhiteSpace(currentFilePath);
                    UpdateDashboard();
                }
            }
        }

        private void AnalyzePcap(string filePath)
        {
            int packetCount = 0;
            int deauthCount = 0;
            int parseErrorCount = 0;
            int suspiciousBurstCount = 0;

            const int deauthAlertThreshold = 5;
            TimeSpan deauthAlertWindow = TimeSpan.FromSeconds(3);

            var deauthWindows = new Dictionary<string, Queue<DateTime>>();
            var alertedSources = new HashSet<string>();

            using (var device = new SharpPcap.LibPcap.CaptureFileReaderDevice(filePath))
            {
                device.Open();

                device.OnPacketArrival += (s, args) =>
                {
                    packetCount++;

                    var rawPacket = args.GetPacket();
                    DateTime packetTimestamp = rawPacket.Timeval.Date;
                    string time = packetTimestamp.ToString("HH:mm:ss.fff");
                    int length = rawPacket.Data.Length;

                    string macSrc = "?";
                    string macDst = "?";
                    string info = $"Rozmiar: {length} B";

                    bool isDeauthFrame = false;
                    bool isSuspiciousBurst = false;
                    bool shouldLogBurstAlert = false;

                    try
                    {
                        var parsedPacket = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                        var wifiFrame = parsedPacket.Extract<MacFrame>();

                        if (wifiFrame != null)
                        {
                            info = $"Wi-Fi: {wifiFrame.FrameControl.Type} - {wifiFrame.FrameControl.SubType}";

                            if (wifiFrame is ManagementFrame mgmtFrame)
                            {
                                macSrc = mgmtFrame.SourceAddress?.ToString() ?? "?";
                                macDst = mgmtFrame.DestinationAddress?.ToString() ?? "?";
                            }
                            else if (wifiFrame is DataFrame dataFrame)
                            {
                                macSrc = dataFrame.SourceAddress?.ToString() ?? "?";
                                macDst = dataFrame.DestinationAddress?.ToString() ?? "?";
                            }

                            if (wifiFrame is DeauthenticationFrame)
                            {
                                isDeauthFrame = true;
                                deauthCount++;
                                info = "Wi-Fi: Zarządzanie - Deautoryzacja";

                                string sourceKey = string.IsNullOrWhiteSpace(macSrc) || macSrc == "?"
                                    ? "Nieznany"
                                    : macSrc;

                                if (!deauthWindows.TryGetValue(sourceKey, out Queue<DateTime> timestamps))
                                {
                                    timestamps = new Queue<DateTime>();
                                    deauthWindows[sourceKey] = timestamps;
                                }

                                timestamps.Enqueue(packetTimestamp);

                                while (timestamps.Count > 0 && (packetTimestamp - timestamps.Peek()) > deauthAlertWindow)
                                {
                                    timestamps.Dequeue();
                                }

                                if (timestamps.Count >= deauthAlertThreshold)
                                {
                                    isSuspiciousBurst = true;

                                    if (!alertedSources.Contains(sourceKey))
                                    {
                                        alertedSources.Add(sourceKey);
                                        suspiciousBurstCount++;
                                        shouldLogBurstAlert = true;
                                    }
                                }
                                else
                                {
                                    alertedSources.Remove(sourceKey);
                                }
                            }
                        }
                        else
                        {
                            var ethFrame = parsedPacket.Extract<EthernetPacket>();
                            if (ethFrame != null)
                            {
                                macSrc = ethFrame.SourceHardwareAddress?.ToString() ?? "?";
                                macDst = ethFrame.DestinationHardwareAddress?.ToString() ?? "?";
                                info = "Ethernet";
                            }
                        }
                    }
                    catch
                    {
                        parseErrorCount++;
                        info = "Błąd parsowania pakietu";
                    }

                    BeginInvoke((MethodInvoker)delegate
                    {
                        int rowIndex = dgvPackets.Rows.Add(packetCount, time, macSrc, macDst, info);

                        if (isDeauthFrame)
                        {
                            if (isSuspiciousBurst)
                            {
                                dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(60, 20, 20);
                                dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.Red;

                                if (shouldLogBurstAlert)
                                {
                                    LogMessage(
                                        "ALERT",
                                        $"Podejrzana seria ramek deautoryzacji z adresu {macSrc} (>= {deauthAlertThreshold} w {deauthAlertWindow.TotalSeconds:0} s).",
                                        WinColor.Red
                                    );
                                }
                            }
                            else
                            {
                                dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(60, 45, 0);
                                dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.Orange;

                                LogMessage(
                                    "NOTICE",
                                    $"Wykryto pojedynczą ramkę deautoryzacji. Nadawca: {macSrc}",
                                    WinColor.Orange
                                );
                            }
                        }
                    });
                };

                device.Capture();
                device.Close();
            }

            lastPacketCount = packetCount;
            lastDeauthCount = deauthCount;
            lastParseErrorCount = parseErrorCount;
            lastSuspiciousBurstCount = suspiciousBurstCount;

            BeginInvoke((MethodInvoker)delegate
            {
                UpdateDashboard();
            });
        }

        private void ResetView()
        {
            dgvPackets.Rows.Clear();
            rtbLogs.Clear();

            lastPacketCount = 0;
            lastDeauthCount = 0;
            lastParseErrorCount = 0;
            lastSuspiciousBurstCount = 0;

            UpdateDashboard();
        }

        private void UpdateDashboard()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)UpdateDashboard);
                return;
            }

            lblPacketCount.Text = lastPacketCount.ToString();
            lblDeauthCount.Text = lastDeauthCount.ToString();
            lblSuspiciousCount.Text = lastSuspiciousBurstCount.ToString();

            lblPacketCount.ForeColor = WinColor.White;
            lblDeauthCount.ForeColor = lastDeauthCount > 0 ? WinColor.FromArgb(255, 183, 0) : WinColor.White;
            lblSuspiciousCount.ForeColor = lastSuspiciousBurstCount > 0 ? WinColor.FromArgb(255, 80, 80) : WinColor.White;

            lblPacketsCaption.ForeColor = WinColor.FromArgb(172, 181, 215);
            lblDeauthCaption.ForeColor = WinColor.FromArgb(172, 181, 215);
            lblSuspiciousCaption.ForeColor = WinColor.FromArgb(172, 181, 215);

            panelPackets.BackColor = WinColor.FromArgb(24, 28, 52);
            panelDeauth.BackColor = lastDeauthCount > 0
                ? WinColor.FromArgb(74, 52, 0)
                : WinColor.FromArgb(24, 28, 52);
            panelSuspicious.BackColor = lastSuspiciousBurstCount > 0
                ? WinColor.FromArgb(74, 24, 24)
                : WinColor.FromArgb(24, 28, 52);

            if (lastSuspiciousBurstCount > 0)
            {
                panelStatus.BackColor = WinColor.FromArgb(74, 24, 24);
                lblStatusValue.Text = "ALERT";
                lblStatusValue.ForeColor = WinColor.FromArgb(255, 80, 80);
                lblStatusDescription.Text = "Wykryto podejrzane serie ramek deautoryzacji. To zachowanie wygląda jak próba zakłócenia połączenia.";
            }
            else if (lastDeauthCount > 0)
            {
                panelStatus.BackColor = WinColor.FromArgb(74, 52, 0);
                lblStatusValue.Text = "UWAGA";
                lblStatusValue.ForeColor = WinColor.FromArgb(255, 183, 0);
                lblStatusDescription.Text = "Wykryto pojedyncze ramki deautoryzacji. Zdarzenie nie wygląda jeszcze na serię alarmową.";
            }
            else if (lastPacketCount > 0)
            {
                panelStatus.BackColor = WinColor.FromArgb(18, 58, 32);
                lblStatusValue.Text = "BRAK ZAGROŻENIA";
                lblStatusValue.ForeColor = WinColor.LimeGreen;
                lblStatusDescription.Text = "Nie wykryto deautoryzacji ani podejrzanych serii. Analiza nie wskazuje aktywnego zagrożenia.";
            }
            else
            {
                panelStatus.BackColor = WinColor.FromArgb(24, 28, 52);
                lblStatusValue.Text = "BRAK DANYCH";
                lblStatusValue.ForeColor = WinColor.LightGray;
                lblStatusDescription.Text = "Oczekiwanie na analizę pliku...";
            }
        }

        private void LogMessage(string level, string message, WinColor color)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke((MethodInvoker)(() => LogMessage(level, message, color)));
                return;
            }

            rtbLogs.SelectionColor = color;
            rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {level}: {message}\n");
            rtbLogs.ScrollToCaret();
        }

        private void btnExportReport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentFilePath))
            {
                LogMessage("WARN", "Najpierw załaduj plik PCAP.", WinColor.Orange);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Plik PDF (*.pdf)|*.pdf";
                saveFileDialog.Title = "Zapisz raport PDF";
                saveFileDialog.FileName = $"raport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    CreatePdfReport(saveFileDialog.FileName);
                    LogMessage("SUCCESS", $"Raport PDF zapisano: {saveFileDialog.FileName}", WinColor.Lime);
                }
                catch (Exception ex)
                {
                    LogMessage("ERROR", $"Nie udało się zapisać raportu PDF. ({ex.Message})", WinColor.Red);
                }
            }
        }

        private void CreatePdfReport(string outputPath)
        {
            var document = new Document();
            document.Info.Title = "Raport analizy PCAP";
            document.Info.Subject = "Raport wygenerowany przez NetQin";
            document.Info.Author = "NetQin";

            DefinePdfStyles(document);

            Section section = document.AddSection();
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);

            Paragraph title = section.AddParagraph("Raport analizy PCAP");
            title.Style = "ReportTitle";

            Paragraph subtitle = section.AddParagraph($"Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            subtitle.Style = "ReportSubtitle";
            subtitle.Format.SpaceAfter = Unit.FromCentimeter(0.4);

            section.AddParagraph("Podsumowanie", "SectionHeading");

            Table summaryTable = section.AddTable();
            summaryTable.Borders.Width = 0.5;
            summaryTable.Borders.Color = Colors.LightGray;
            summaryTable.Rows.LeftIndent = 0;

            summaryTable.AddColumn(Unit.FromCentimeter(6));
            summaryTable.AddColumn(Unit.FromCentimeter(10));

            AddKeyValueRow(summaryTable, "Plik", currentFilePath);
            AddKeyValueRow(summaryTable, "Liczba pakietów", lastPacketCount.ToString());
            AddKeyValueRow(summaryTable, "Ramki deautoryzacji", lastDeauthCount.ToString());
            AddKeyValueRow(summaryTable, "Podejrzane serie ramek", lastSuspiciousBurstCount.ToString());
            AddKeyValueRow(summaryTable, "Błędy parsowania", lastParseErrorCount.ToString());

            section.AddParagraph();
            section.AddParagraph("Ocena zdarzeń", "SectionHeading");

            Table statusTable = section.AddTable();
            statusTable.Borders.Width = 0.5;
            statusTable.Borders.Color = Colors.LightGray;
            statusTable.Rows.LeftIndent = 0;

            statusTable.AddColumn(Unit.FromCentimeter(8));
            statusTable.AddColumn(Unit.FromCentimeter(8));

            Row header = statusTable.AddRow();
            header.Shading.Color = Colors.DarkSlateBlue;
            header.Format.Font.Color = Colors.White;
            header.Format.Font.Bold = true;
            header.Cells[0].AddParagraph("Metryka");
            header.Cells[1].AddParagraph("Wartość");

            AddStatusRow(statusTable, "Czy wykryto deautoryzację", lastDeauthCount > 0 ? "Tak" : "Nie");
            AddStatusRow(statusTable, "Czy wykryto podejrzaną serię", lastSuspiciousBurstCount > 0 ? "Tak" : "Nie");
            AddStatusRow(statusTable, "Czy były błędy parsowania", lastParseErrorCount > 0 ? "Tak" : "Nie");

            section.AddParagraph();
            section.AddParagraph("Logi analizy", "SectionHeading");

            string[] logLines = rtbLogs.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (logLines.Length == 0)
            {
                Paragraph emptyLogs = section.AddParagraph("Brak logów.");
                emptyLogs.Style = "LogText";
            }
            else
            {
                foreach (string line in logLines)
                {
                    Paragraph logParagraph = section.AddParagraph(line);
                    logParagraph.Style = "LogText";

                    if (line.Contains("ERROR"))
                        logParagraph.Format.Font.Color = Colors.DarkRed;
                    else if (line.Contains("ALERT"))
                        logParagraph.Format.Font.Color = Colors.Red;
                    else if (line.Contains("NOTICE"))
                        logParagraph.Format.Font.Color = Colors.DarkOrange;
                    else if (line.Contains("SUCCESS"))
                        logParagraph.Format.Font.Color = Colors.ForestGreen;
                }
            }

            Paragraph footerNote = section.AddParagraph();
            footerNote.Format.SpaceBefore = Unit.FromCentimeter(0.6);
            footerNote.Format.Font.Size = 8;
            footerNote.Format.Font.Color = Colors.Gray;
            footerNote.AddText("Raport wygenerowany automatycznie przez NetQin.");

            var renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.Save(outputPath);
        }

        private void DefinePdfStyles(Document document)
        {
            Style normal = document.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 10;

            Style titleStyle = document.Styles.AddStyle("ReportTitle", "Normal");
            titleStyle.Font.Name = "Arial";
            titleStyle.Font.Size = 18;
            titleStyle.Font.Bold = true;
            titleStyle.Font.Color = Colors.DarkSlateBlue;
            titleStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.15);

            Style subtitleStyle = document.Styles.AddStyle("ReportSubtitle", "Normal");
            subtitleStyle.Font.Name = "Arial";
            subtitleStyle.Font.Size = 9;
            subtitleStyle.Font.Color = Colors.Gray;

            Style sectionStyle = document.Styles.AddStyle("SectionHeading", "Normal");
            sectionStyle.Font.Name = "Arial";
            sectionStyle.Font.Size = 12;
            sectionStyle.Font.Bold = true;
            sectionStyle.Font.Color = Colors.Black;
            sectionStyle.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.25);
            sectionStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.2);

            Style logStyle = document.Styles.AddStyle("LogText", "Normal");
            logStyle.Font.Name = "Courier New";
            logStyle.Font.Size = 8.5;
            logStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.05);
        }

        private void AddKeyValueRow(Table table, string key, string value)
        {
            Row row = table.AddRow();
            row.TopPadding = 4;
            row.BottomPadding = 4;
            row.Cells[0].Shading.Color = Colors.GhostWhite;
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].AddParagraph(key);
            row.Cells[1].AddParagraph(value);
        }

        private void AddStatusRow(Table table, string label, string value)
        {
            Row row = table.AddRow();
            row.TopPadding = 4;
            row.BottomPadding = 4;
            row.Cells[0].AddParagraph(label);
            row.Cells[1].AddParagraph(value);

            if (value == "Tak")
                row.Cells[1].Format.Font.Color = Colors.DarkRed;
            else
                row.Cells[1].Format.Font.Color = Colors.ForestGreen;
        }
    }
}