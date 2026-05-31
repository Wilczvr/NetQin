using System;
using System.Collections.Generic;
using System.Linq;
using WinColor = System.Drawing.Color;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using NetQin.Models;
using NetQin.Services;

namespace NetQin
{
    public partial class Form1 : Form
    {
        private const int MaxRenderedPacketRows = 10000;
        private const int FilterDebounceMilliseconds = 250;
        private const int EmSetCueBanner = 0x1501;

        private string currentFilePath = string.Empty;
        private bool isAnalysisRunning = false;
        private bool isReportExportRunning = false;
        private bool suppressFilterRefresh = false;
        private readonly ToolTip fileNameToolTip = new ToolTip();
        private readonly Timer filterDebounceTimer = new Timer();

        private readonly PcapAnalyzerService _pcapAnalyzer = new PcapAnalyzerService();
        private readonly DetectionEngine _detectionEngine = new DetectionEngine();
        private readonly IncidentCorrelationService _incidentCorrelationService = new IncidentCorrelationService();
        private readonly DetectionSettings _detectionSettings = new DetectionSettings();
        private AnalysisResult _currentResult;

        private Panel panelDisassoc;
        private Label lblDisassocCount;
        private Label lblDisassocCaption;

        private Panel panelFilters;
        private TextBox txtFilterSearch;
        private TextBox txtFilterSsid;
        private TextBox txtFilterBssid;
        private ComboBox cmbFrameType;
        private CheckBox chkOnlyDeauth;
        private CheckBox chkOnlyDisassoc;
        private CheckBox chkOnlySuspicious;
        private CheckBox chkOnlyBeacon;
        private CheckBox chkOnlyAuthAssoc;
        private Button btnClearFilters;
        private Label lblVisibleCount;

        private int dashboardLeft;
        private int dashboardTop;
        private int dashboardFullWidth;
        private int dashboardTileHeight;
        private int dashboardGap = 14;
        private int statusOriginalBottom;

        private int gridOriginalLeft;
        private int gridOriginalTop;

        public Form1()
        {
            InitializeComponent();
            this.MinimumSize = new System.Drawing.Size(1388, 937);

            InitializeDynamicDashboardCards();
            InitializeFilterBar();
            InitializeFilterDebounce();

            gridOriginalLeft = dgvPackets.Left;
            gridOriginalTop = dgvPackets.Top;

            this.Resize += (s, args) => ApplyDashboardLayout();

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

            ConfigurePacketGridColumns();

            btnExportReport.Enabled = false;

            ConfigurePremiumUi();
            ApplyDashboardLayout();
            UpdateDashboard();
            UpdateVisibleCount(0, 0);

            this.Disposed += (s, e) =>
            {
                filterDebounceTimer.Dispose();
                fileNameToolTip.Dispose();
            };
        }

        private void ConfigurePacketGridColumns()
        {
            dgvPackets.Columns.Clear();

            dgvPackets.Columns.Add(CreateTextColumn("colNumber", "Nr", 10));
            dgvPackets.Columns.Add(CreateTextColumn("colTime", "Czas", 16));
            dgvPackets.Columns.Add(CreateTextColumn("colSource", "MAC Nadawcy", 20));
            dgvPackets.Columns.Add(CreateTextColumn("colDestination", "MAC Odbiorcy", 20));
            dgvPackets.Columns.Add(CreateTextColumn("colSsid", "SSID", 18));
            dgvPackets.Columns.Add(CreateTextColumn("colBssid", "BSSID", 20));
            dgvPackets.Columns.Add(CreateTextColumn("colSubtype", "Subtype", 18));
            dgvPackets.Columns.Add(CreateTextColumn("colChannel", "Channel", 10));
            dgvPackets.Columns.Add(CreateTextColumn("colInfo", "Info", 44));
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, float fillWeight)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                MinimumWidth = 6,
                ReadOnly = true,
                FillWeight = fillWeight
            };
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

            lblDisassocCount.Font = new System.Drawing.Font("Segoe UI Semibold", 27F, System.Drawing.FontStyle.Bold);
            lblDisassocCaption.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
            lblDisassocCaption.ForeColor = WinColor.FromArgb(172, 181, 215);

            lblStatusValue.Font = new System.Drawing.Font("Segoe UI Semibold", 22F, System.Drawing.FontStyle.Bold);
            lblStatusDescription.AutoSize = true;
            lblStatusDescription.MaximumSize = new System.Drawing.Size(0, 0);

            rtbLogs.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular);

            txtFilterSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtFilterSsid.Font = new System.Drawing.Font("Segoe UI", 10F);
            txtFilterBssid.Font = new System.Drawing.Font("Segoe UI", 10F);
            cmbFrameType.Font = new System.Drawing.Font("Segoe UI", 9.5F);

            chkOnlyDeauth.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            chkOnlyDisassoc.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            chkOnlySuspicious.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            chkOnlyBeacon.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            chkOnlyAuthAssoc.Font = new System.Drawing.Font("Segoe UI", 9.5F);

            btnClearFilters.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            lblVisibleCount.Font = new System.Drawing.Font("Segoe UI", 9.5F);

            lblAppTitle.Text = "NetQin";
            lblSubtitle.Text = "Wi-Fi Threat Analysis Console";
        }

        private void InitializeDynamicDashboardCards()
        {
            dashboardLeft = panelPackets.Left;
            dashboardTop = panelPackets.Top;
            dashboardFullWidth = panelPackets.Width;
            dashboardTileHeight = panelPackets.Height;
            statusOriginalBottom = panelStatus.Bottom;

            panelDisassoc = new Panel
            {
                Name = "panelDisassoc",
                BackColor = WinColor.FromArgb(24, 28, 52),
                Parent = panelPackets.Parent
            };

            lblDisassocCount = new Label
            {
                Name = "lblDisassocCount",
                AutoSize = true,
                Text = "0",
                ForeColor = WinColor.White,
                BackColor = WinColor.Transparent
            };

            lblDisassocCaption = new Label
            {
                Name = "lblDisassocCaption",
                AutoSize = true,
                Text = "Disassociation",
                ForeColor = WinColor.FromArgb(172, 181, 215),
                BackColor = WinColor.Transparent
            };

            panelDisassoc.Controls.Add(lblDisassocCount);
            panelDisassoc.Controls.Add(lblDisassocCaption);
            panelDisassoc.BringToFront();
        }

        private void InitializeFilterBar()
        {
            panelFilters = new Panel
            {
                Name = "panelFilters",
                BackColor = WinColor.FromArgb(9, 16, 45),
                Parent = dgvPackets.Parent
            };

            txtFilterSearch = new TextBox
            {
                Name = "txtFilterSearch",
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                BackColor = WinColor.FromArgb(24, 28, 52),
                ForeColor = WinColor.White
            };

            txtFilterSsid = new TextBox
            {
                Name = "txtFilterSsid",
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                BackColor = WinColor.FromArgb(24, 28, 52),
                ForeColor = WinColor.White
            };

            txtFilterBssid = new TextBox
            {
                Name = "txtFilterBssid",
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                BackColor = WinColor.FromArgb(24, 28, 52),
                ForeColor = WinColor.White
            };

            cmbFrameType = new ComboBox
            {
                Name = "cmbFrameType",
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = WinColor.FromArgb(24, 28, 52),
                ForeColor = WinColor.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbFrameType.Items.AddRange(new object[]
            {
                "Wszystkie",
                "Beacon",
                "Deauth",
                "Disassoc",
                "Auth/Assoc",
                "Inne"
            });
            cmbFrameType.SelectedIndex = 0;

            chkOnlyDeauth = new CheckBox
            {
                Name = "chkOnlyDeauth",
                Text = "Tylko deauth",
                AutoSize = true,
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.FromArgb(255, 214, 102)
            };

            chkOnlyDisassoc = new CheckBox
            {
                Name = "chkOnlyDisassoc",
                Text = "Tylko disassoc",
                AutoSize = true,
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.DeepSkyBlue
            };

            chkOnlySuspicious = new CheckBox
            {
                Name = "chkOnlySuspicious",
                Text = "Tylko serie",
                AutoSize = true,
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.FromArgb(255, 120, 120)
            };

            chkOnlyBeacon = new CheckBox
            {
                Name = "chkOnlyBeacon",
                Text = "Tylko beacon",
                AutoSize = true,
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.MediumPurple
            };

            chkOnlyAuthAssoc = new CheckBox
            {
                Name = "chkOnlyAuthAssoc",
                Text = "Tylko auth/assoc",
                AutoSize = true,
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.LightGreen
            };

            btnClearFilters = new Button
            {
                Name = "btnClearFilters",
                Text = "Wyczyść filtry",
                FlatStyle = FlatStyle.Flat,
                BackColor = WinColor.FromArgb(88, 28, 135),
                ForeColor = WinColor.White,
                UseVisualStyleBackColor = false
            };
            btnClearFilters.FlatAppearance.BorderSize = 0;

            lblVisibleCount = new Label
            {
                Name = "lblVisibleCount",
                AutoSize = true,
                Text = "Widoczne: 0 / 0",
                BackColor = WinColor.Transparent,
                ForeColor = WinColor.FromArgb(172, 181, 215),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };

            panelFilters.Controls.Add(txtFilterSearch);
            panelFilters.Controls.Add(txtFilterSsid);
            panelFilters.Controls.Add(txtFilterBssid);
            panelFilters.Controls.Add(cmbFrameType);
            panelFilters.Controls.Add(chkOnlyDeauth);
            panelFilters.Controls.Add(chkOnlyDisassoc);
            panelFilters.Controls.Add(chkOnlySuspicious);
            panelFilters.Controls.Add(chkOnlyBeacon);
            panelFilters.Controls.Add(chkOnlyAuthAssoc);
            panelFilters.Controls.Add(btnClearFilters);
            panelFilters.Controls.Add(lblVisibleCount);

            txtFilterSearch.TextChanged += (s, e) => SchedulePacketFilterRefresh();
            txtFilterSsid.TextChanged += (s, e) => SchedulePacketFilterRefresh();
            txtFilterBssid.TextChanged += (s, e) => SchedulePacketFilterRefresh();
            cmbFrameType.SelectedIndexChanged += (s, e) => RefreshPacketFiltersImmediately();

            chkOnlyDeauth.CheckedChanged += (s, e) => RefreshPacketFiltersImmediately();
            chkOnlyDisassoc.CheckedChanged += (s, e) => RefreshPacketFiltersImmediately();
            chkOnlySuspicious.CheckedChanged += (s, e) => RefreshPacketFiltersImmediately();
            chkOnlyBeacon.CheckedChanged += (s, e) => RefreshPacketFiltersImmediately();
            chkOnlyAuthAssoc.CheckedChanged += (s, e) => RefreshPacketFiltersImmediately();

            btnClearFilters.Click += (s, e) => ClearFilters();

            fileNameToolTip.SetToolTip(txtFilterSearch, "Szukaj po MAC, SSID, BSSID, subtype, kanale lub Info");
            fileNameToolTip.SetToolTip(txtFilterSsid, "Filtruj po SSID");
            fileNameToolTip.SetToolTip(txtFilterBssid, "Filtruj po BSSID");
            fileNameToolTip.SetToolTip(cmbFrameType, "Filtruj po typie ramki");

            SetCueBanner(txtFilterSearch, "Szukaj po MAC, SSID, BSSID, typie lub Info");
            SetCueBanner(txtFilterSsid, "Filtr SSID");
            SetCueBanner(txtFilterBssid, "Filtr BSSID");
        }

        private void InitializeFilterDebounce()
        {
            filterDebounceTimer.Interval = FilterDebounceMilliseconds;
            filterDebounceTimer.Tick += (s, e) =>
            {
                filterDebounceTimer.Stop();
                ApplyPacketFilters();
            };
        }

        private void SchedulePacketFilterRefresh()
        {
            if (suppressFilterRefresh)
                return;

            filterDebounceTimer.Stop();
            filterDebounceTimer.Start();
        }

        private void RefreshPacketFiltersImmediately()
        {
            if (!suppressFilterRefresh)
                ApplyPacketFilters();
        }

        private static void SetCueBanner(TextBox textBox, string cueText)
        {
            if (textBox == null || string.IsNullOrWhiteSpace(cueText))
                return;

            SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, cueText);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private void ApplyDashboardLayout()
        {
            int rightMargin = 28;
            int bottomGapAboveLogs = 18;
            int logsBottomMargin = 26;
            int leftGapToDashboard = 22;

            int filterHeight = 110;
            int filterGapToGrid = 8;
            int row1Top = 4;
            int row2Top = 38;
            int row3Top = 72;

            dashboardFullWidth = Math.Max(360, this.ClientSize.Width - dashboardLeft - rightMargin);

            int leftAreaWidth = Math.Max(500, dashboardLeft - gridOriginalLeft - leftGapToDashboard);

            panelFilters.SetBounds(
                gridOriginalLeft,
                gridOriginalTop,
                leftAreaWidth,
                filterHeight);

            int visibleLabelWidth = 160;
            lblVisibleCount.MaximumSize = new System.Drawing.Size(visibleLabelWidth, 0);
            lblVisibleCount.AutoSize = false;
            lblVisibleCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lblVisibleCount.SetBounds(
                leftAreaWidth - visibleLabelWidth,
                row1Top + 2,
                visibleLabelWidth,
                24);

            int fullRowWidth = leftAreaWidth - 12;
            int firstRowWidth = leftAreaWidth - visibleLabelWidth - 12;

            txtFilterSearch.SetBounds(0, row1Top, Math.Max(220, firstRowWidth), 28);

            int secondRowGap = 8;
            int secondRowControlWidth = Math.Max(120, (fullRowWidth - (secondRowGap * 2)) / 3);

            txtFilterSsid.SetBounds(0, row2Top, secondRowControlWidth, 28);
            txtFilterBssid.SetBounds(txtFilterSsid.Right + secondRowGap, row2Top, secondRowControlWidth, 28);
            cmbFrameType.SetBounds(txtFilterBssid.Right + secondRowGap, row2Top, secondRowControlWidth, 29);

            chkOnlyDeauth.Left = 0;
            chkOnlyDeauth.Top = row3Top + 2;

            chkOnlyDisassoc.Left = chkOnlyDeauth.Right + 16;
            chkOnlyDisassoc.Top = row3Top + 2;

            chkOnlySuspicious.Left = chkOnlyDisassoc.Right + 16;
            chkOnlySuspicious.Top = row3Top + 2;

            chkOnlyBeacon.Left = chkOnlySuspicious.Right + 16;
            chkOnlyBeacon.Top = row3Top + 2;

            int clearButtonWidth = 140;
            int clearButtonLeft = leftAreaWidth - clearButtonWidth;

            btnClearFilters.SetBounds(
                clearButtonLeft,
                row3Top - 2,
                clearButtonWidth,
                30);

            chkOnlyAuthAssoc.Top = row3Top + 2;
            chkOnlyAuthAssoc.Left = chkOnlyBeacon.Right + 16;

            int authAssocMaxLeft = btnClearFilters.Left - chkOnlyAuthAssoc.Width - 16;
            if (chkOnlyAuthAssoc.Left > authAssocMaxLeft)
            {
                chkOnlyAuthAssoc.Left = Math.Max(chkOnlyBeacon.Right + 8, authAssocMaxLeft);
            }

            int gridTop = panelFilters.Bottom + filterGapToGrid;
            int gridHeight = Math.Max(220, rtbLogs.Top - gridTop - 10);

            dgvPackets.SetBounds(
                gridOriginalLeft,
                gridTop,
                leftAreaWidth,
                gridHeight);

            int availableHeight = rtbLogs.Top - dashboardTop - bottomGapAboveLogs;
            int statusHeight = Math.Max(170, Math.Min(280, (int)(availableHeight * 0.44)));
            int tileHeight = Math.Max(98, (availableHeight - statusHeight - (dashboardGap * 2)) / 2);
            int tileWidth = (dashboardFullWidth - dashboardGap) / 2;

            panelPackets.SetBounds(
                dashboardLeft,
                dashboardTop,
                tileWidth,
                tileHeight);

            panelDeauth.SetBounds(
                dashboardLeft + tileWidth + dashboardGap,
                dashboardTop,
                tileWidth,
                tileHeight);

            panelDisassoc.SetBounds(
                dashboardLeft,
                dashboardTop + tileHeight + dashboardGap,
                tileWidth,
                tileHeight);

            panelSuspicious.SetBounds(
                dashboardLeft + tileWidth + dashboardGap,
                dashboardTop + tileHeight + dashboardGap,
                tileWidth,
                tileHeight);

            int statusTop = dashboardTop + (tileHeight * 2) + (dashboardGap * 2);
            panelStatus.SetBounds(
                dashboardLeft,
                statusTop,
                dashboardFullWidth,
                statusHeight);

            lblDisassocCount.Left = 18;
            lblDisassocCount.Top = 12;
            lblDisassocCaption.Left = 18;
            lblDisassocCaption.Top = lblDisassocCount.Bottom + 6;
            lblDisassocCaption.MaximumSize = new System.Drawing.Size(panelDisassoc.Width - 30, 0);
            lblDisassocCaption.AutoSize = true;

            lblStatusValue.Left = 18;
            lblStatusValue.Top = 44;

            lblStatusDescription.Left = 18;
            lblStatusDescription.Top = lblStatusValue.Bottom + 10;
            lblStatusDescription.MaximumSize = new System.Drawing.Size(panelStatus.Width - 36, 0);
            lblStatusDescription.AutoSize = true;

            rtbLogs.SetBounds(
                rtbLogs.Left,
                rtbLogs.Top,
                this.ClientSize.Width - rtbLogs.Left - rightMargin,
                this.ClientSize.Height - rtbLogs.Top - logsBottomMargin);
        }

        private async void btnLoadPcap_Click(object sender, EventArgs e)
        {
            if (isAnalysisRunning || isReportExportRunning)
            {
                LogMessage(
                    "WARN",
                    isAnalysisRunning ? "Analiza już trwa." : "Poczekaj na zakończenie generowania raportu.",
                    WinColor.Orange);
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
                    string filePath = currentFilePath;
                    _currentResult = await Task.Run(() => AnalyzeFile(filePath));

                    RenderAnalysisResult(_currentResult);

                    LogMessage(
                        "SUCCESS",
                        $"Załadowano {_currentResult.Statistics.TotalPackets} pakietów. " +
                        $"Ramki deautoryzacji: {_currentResult.Statistics.DeauthCount}. " +
                        $"Ramki disassociation: {_currentResult.Statistics.DisassocCount}. " +
                        $"Podejrzane serie: {_currentResult.Statistics.SuspiciousBurstCount}. " +
                        $"Korelacje: {_currentResult.CorrelatedIncidents.Count}. " +
                        $"Błędy parsowania: {_currentResult.Statistics.ParseErrorCount}.",
                        WinColor.Lime
                    );
                }
                catch (Exception ex)
                {
                    _currentResult = null;
                    LogMessage("ERROR", $"Nie udało się przeanalizować pliku PCAP. ({ex.Message})", WinColor.Red);
                    UpdateDashboard();
                    UpdateVisibleCount(0, 0);
                }
                finally
                {
                    isAnalysisRunning = false;
                    btnLoadPcap.Enabled = true;
                    btnExportReport.Enabled = _currentResult != null;
                }
            }
        }

        private AnalysisResult AnalyzeFile(string filePath)
        {
            var result = _pcapAnalyzer.Analyze(filePath, _detectionSettings);

            result.Incidents = _detectionEngine.Detect(result.Packets, _detectionSettings);
            result.CorrelatedIncidents = _incidentCorrelationService.Correlate(result.Incidents);
            result.Statistics.SuspiciousBurstCount = result.Incidents.Count;

            foreach (var incident in result.Incidents)
            {
                result.Logs.Add(new AnalysisLogEntry
                {
                    Timestamp = incident.EndTime,
                    Level = AnalysisLogLevel.Warning,
                    Message = string.Format("[{0}/{1}] {2}", incident.RiskLevel, incident.RiskScore, incident.Description)
                });
            }

            foreach (var correlated in result.CorrelatedIncidents)
            {
                result.Logs.Add(new AnalysisLogEntry
                {
                    Timestamp = correlated.EndTime,
                    Level = AnalysisLogLevel.Warning,
                    Message = string.Format(
                        "[CORRELATED {0}/{1}] {2} | Reguły źródłowe: {3}",
                        correlated.RiskLevel,
                        correlated.RiskScore,
                        correlated.Description,
                        correlated.SourceRulesDisplay)
                });
            }

            return result;
        }

        private void RenderAnalysisResult(AnalysisResult result)
        {
            rtbLogs.Clear();

            foreach (var log in result.Logs)
            {
                LogMessage(
                    log.Level.ToString().ToUpper(),
                    log.Message,
                    MapLogLevelToColor(log.Level),
                    log.Timestamp);
            }

            ApplyPacketFilters();
            UpdateDashboard();
        }

        private void ApplyPacketFilters()
        {
            filterDebounceTimer.Stop();

            if (_currentResult == null || _currentResult.Packets == null)
            {
                ClearPacketGrid();
                UpdateVisibleCount(0, 0);
                return;
            }

            IEnumerable<PacketRecord> filtered = _currentResult.Packets;

            string query = txtFilterSearch.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(query))
            {
                filtered = filtered.Where(p =>
                    ContainsIgnoreCase(p.SourceMac, query) ||
                    ContainsIgnoreCase(p.DestinationMac, query) ||
                    ContainsIgnoreCase(p.Ssid, query) ||
                    ContainsIgnoreCase(p.Bssid, query) ||
                    ContainsIgnoreCase(p.FrameSubtype, query) ||
                    ContainsIgnoreCase(p.FrameType, query) ||
                    ContainsIgnoreCase(p.Info, query) ||
                    (p.Channel.HasValue && p.Channel.Value.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            string ssidQuery = txtFilterSsid.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ssidQuery))
            {
                filtered = filtered.Where(p => ContainsIgnoreCase(p.Ssid, ssidQuery));
            }

            string bssidQuery = txtFilterBssid.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(bssidQuery))
            {
                filtered = filtered.Where(p => ContainsIgnoreCase(p.Bssid, bssidQuery));
            }

            string frameTypeSelection = cmbFrameType.SelectedItem as string ?? "Wszystkie";
            if (!string.Equals(frameTypeSelection, "Wszystkie", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p => MatchesFrameSelection(p, frameTypeSelection));
            }

            bool onlyDeauth = chkOnlyDeauth.Checked;
            bool onlyDisassoc = chkOnlyDisassoc.Checked;
            bool onlyBeacon = chkOnlyBeacon.Checked;
            bool onlyAuthAssoc = chkOnlyAuthAssoc.Checked;

            if (onlyDeauth || onlyDisassoc || onlyBeacon || onlyAuthAssoc)
            {
                filtered = filtered.Where(p =>
                    (onlyDeauth && p.IsDeauth) ||
                    (onlyDisassoc && p.IsDisassoc) ||
                    (onlyBeacon && p.IsBeacon) ||
                    (onlyAuthAssoc && IsAuthAssocPacket(p)));
            }

            if (chkOnlySuspicious.Checked)
            {
                filtered = filtered.Where(p => p.IsSuspiciousBurst);
            }

            var renderedPackets = new List<PacketRecord>();
            int visiblePacketCount = 0;

            foreach (var packet in filtered)
            {
                visiblePacketCount++;

                if (renderedPackets.Count < MaxRenderedPacketRows)
                    renderedPackets.Add(packet);
            }

            RenderPacketRows(renderedPackets);
            UpdateVisibleCount(
                visiblePacketCount,
                _currentResult.Packets.Count,
                renderedPackets.Count);
        }

        private void RenderPacketRows(List<PacketRecord> packets)
        {
            dgvPackets.SuspendLayout();

            try
            {
                dgvPackets.Rows.Clear();

                foreach (var packet in packets)
                {
                    int rowIndex = dgvPackets.Rows.Add(
                        packet.Number,
                        packet.Timestamp.ToString("HH:mm:ss.fff"),
                        packet.SourceMac,
                        packet.DestinationMac,
                        string.IsNullOrWhiteSpace(packet.Ssid) ? "?" : packet.Ssid,
                        string.IsNullOrWhiteSpace(packet.Bssid) ? "?" : packet.Bssid,
                        GetPacketSubtypeDisplay(packet),
                        packet.Channel.HasValue ? packet.Channel.Value.ToString() : "?",
                        packet.Info);

                    if (packet.IsSuspiciousBurst)
                    {
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(60, 20, 20);
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.Red;
                    }
                    else if (packet.IsDeauth)
                    {
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(60, 45, 0);
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.Orange;
                    }
                    else if (packet.IsDisassoc)
                    {
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(45, 45, 80);
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.DeepSkyBlue;
                    }
                    else if (packet.IsBeacon)
                    {
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(44, 34, 74);
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.Plum;
                    }
                    else if (IsAuthAssocPacket(packet))
                    {
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.BackColor = WinColor.FromArgb(26, 58, 36);
                        dgvPackets.Rows[rowIndex].DefaultCellStyle.ForeColor = WinColor.LightGreen;
                    }
                }
            }
            finally
            {
                dgvPackets.ResumeLayout();
            }
        }

        private void ClearPacketGrid()
        {
            dgvPackets.Rows.Clear();
        }

        private void ClearFilters()
        {
            suppressFilterRefresh = true;

            try
            {
                txtFilterSearch.Text = string.Empty;
                txtFilterSsid.Text = string.Empty;
                txtFilterBssid.Text = string.Empty;
                cmbFrameType.SelectedIndex = 0;

                chkOnlyDeauth.Checked = false;
                chkOnlyDisassoc.Checked = false;
                chkOnlySuspicious.Checked = false;
                chkOnlyBeacon.Checked = false;
                chkOnlyAuthAssoc.Checked = false;
            }
            finally
            {
                suppressFilterRefresh = false;
            }

            ApplyPacketFilters();
        }

        private void UpdateVisibleCount(int visibleCount, int totalCount, int renderedCount = -1)
        {
            lblVisibleCount.Text = $"Widoczne: {visibleCount} / {totalCount}";

            bool isTruncated = renderedCount >= 0 && renderedCount < visibleCount;
            fileNameToolTip.SetToolTip(
                lblVisibleCount,
                isTruncated
                    ? $"Dla płynności interfejsu wyświetlono pierwsze {renderedCount} z {visibleCount} pasujących pakietów."
                    : $"Wyświetlono {visibleCount} z {totalCount} pakietów.");
        }

        private void ResetView()
        {
            filterDebounceTimer.Stop();
            dgvPackets.Rows.Clear();
            rtbLogs.Clear();
            _currentResult = null;

            lblPacketCount.Text = "0";
            lblDeauthCount.Text = "0";
            lblDisassocCount.Text = "0";
            lblSuspiciousCount.Text = "0";

            panelPackets.BackColor = WinColor.FromArgb(24, 28, 52);
            panelDeauth.BackColor = WinColor.FromArgb(24, 28, 52);
            panelDisassoc.BackColor = WinColor.FromArgb(24, 28, 52);
            panelSuspicious.BackColor = WinColor.FromArgb(24, 28, 52);

            panelStatus.BackColor = WinColor.FromArgb(24, 28, 52);
            lblStatusValue.Text = "BRAK DANYCH";
            lblStatusValue.ForeColor = WinColor.LightGray;
            lblStatusDescription.Text = "Oczekiwanie na analizę pliku...";

            UpdateVisibleCount(0, 0);

            fileNameToolTip.SetToolTip(textBox1, string.IsNullOrWhiteSpace(currentFilePath) ? "Brak wczytanego pliku" : currentFilePath);
        }

        private void UpdateDashboard()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)UpdateDashboard);
                return;
            }

            var stats = _currentResult?.Statistics ?? new AnalysisStatistics();
            var incidents = _currentResult?.Incidents ?? new List<DetectionIncident>();
            var correlatedIncidents = _currentResult?.CorrelatedIncidents ?? new List<CorrelatedIncident>();

            var topIncident = incidents.OrderByDescending(i => i.RiskScore).FirstOrDefault();
            var topCorrelated = correlatedIncidents.OrderByDescending(i => i.RiskScore).FirstOrDefault();

            bool correlatedDominates = topCorrelated != null &&
                                       (topIncident == null || topCorrelated.RiskScore >= topIncident.RiskScore);

            bool hasDisconnectEvents = stats.DeauthCount > 0 || stats.DisassocCount > 0;
            string wirelessSummary = BuildWirelessSummary(stats);

            lblPacketCount.Text = stats.TotalPackets.ToString();
            lblDeauthCount.Text = stats.DeauthCount.ToString();
            lblDisassocCount.Text = stats.DisassocCount.ToString();
            lblSuspiciousCount.Text = stats.SuspiciousBurstCount.ToString();

            lblPacketCount.ForeColor = WinColor.White;
            lblDeauthCount.ForeColor = stats.DeauthCount > 0 ? WinColor.FromArgb(255, 183, 0) : WinColor.White;
            lblDisassocCount.ForeColor = stats.DisassocCount > 0 ? WinColor.DeepSkyBlue : WinColor.White;
            lblSuspiciousCount.ForeColor = stats.SuspiciousBurstCount > 0 ? WinColor.FromArgb(255, 80, 80) : WinColor.White;

            lblPacketsCaption.ForeColor = WinColor.FromArgb(172, 181, 215);
            lblDeauthCaption.ForeColor = WinColor.FromArgb(172, 181, 215);
            lblSuspiciousCaption.ForeColor = WinColor.FromArgb(172, 181, 215);
            lblDisassocCaption.ForeColor = WinColor.FromArgb(172, 181, 215);

            panelPackets.BackColor = WinColor.FromArgb(24, 28, 52);
            panelDeauth.BackColor = stats.DeauthCount > 0
                ? WinColor.FromArgb(74, 52, 0)
                : WinColor.FromArgb(24, 28, 52);
            panelDisassoc.BackColor = stats.DisassocCount > 0
                ? WinColor.FromArgb(19, 57, 91)
                : WinColor.FromArgb(24, 28, 52);
            panelSuspicious.BackColor = (stats.SuspiciousBurstCount > 0 || correlatedIncidents.Any())
                ? WinColor.FromArgb(74, 24, 24)
                : WinColor.FromArgb(24, 28, 52);

            if (correlatedDominates && topCorrelated != null)
            {
                panelStatus.BackColor = GetDashboardStatusColor(topCorrelated.RiskLevel);
                lblStatusValue.Text = topCorrelated.RiskLevel == RiskLevel.Critical || topCorrelated.RiskLevel == RiskLevel.High
                    ? "ALERT"
                    : "UWAGA";
                lblStatusValue.ForeColor = GetDashboardAccentColor(topCorrelated.RiskLevel);
                lblStatusDescription.Text =
                    string.Format(
                        "Najwyższe ryzyko: {0} ({1}/100). Korelacja: {2}. SSID: {3}, BSSID: {4}. Reguły: {5}. {6}\n{7}",
                        topCorrelated.RiskLevel,
                        topCorrelated.RiskScore,
                        topCorrelated.CorrelationType,
                        string.IsNullOrWhiteSpace(topCorrelated.Ssid) ? "?" : topCorrelated.Ssid,
                        string.IsNullOrWhiteSpace(topCorrelated.Bssid) ? "?" : topCorrelated.Bssid,
                        topCorrelated.SourceRulesDisplay,
                        topCorrelated.Description,
                        wirelessSummary);
            }
            else if (stats.SuspiciousBurstCount > 0 && topIncident != null)
            {
                panelStatus.BackColor = GetDashboardStatusColor(topIncident.RiskLevel);
                lblStatusValue.Text = topIncident.RiskLevel == RiskLevel.Critical || topIncident.RiskLevel == RiskLevel.High
                    ? "ALERT"
                    : "UWAGA";
                lblStatusValue.ForeColor = GetDashboardAccentColor(topIncident.RiskLevel);
                lblStatusDescription.Text =
                    string.Format(
                        "Najwyższe ryzyko: {0} ({1}/100). Reguła: {2}. SSID: {3}, BSSID: {4}. {5}\n{6}",
                        topIncident.RiskLevel,
                        topIncident.RiskScore,
                        topIncident.RuleId,
                        string.IsNullOrWhiteSpace(topIncident.Ssid) ? "?" : topIncident.Ssid,
                        string.IsNullOrWhiteSpace(topIncident.Bssid) ? "?" : topIncident.Bssid,
                        topIncident.Description,
                        wirelessSummary);
            }
            else if (hasDisconnectEvents)
            {
                panelStatus.BackColor = WinColor.FromArgb(74, 52, 0);
                lblStatusValue.Text = "UWAGA";
                lblStatusValue.ForeColor = WinColor.FromArgb(255, 183, 0);
                lblStatusDescription.Text =
                    $"Wykryto zdarzenia rozłączające Wi-Fi. Deauth: {stats.DeauthCount}, Disassoc: {stats.DisassocCount}. Zdarzenie nie wygląda jeszcze na serię alarmową.\n{wirelessSummary}";
            }
            else if (stats.TotalPackets > 0)
            {
                panelStatus.BackColor = WinColor.FromArgb(18, 58, 32);
                lblStatusValue.Text = "BRAK ZAGROŻENIA";
                lblStatusValue.ForeColor = WinColor.LimeGreen;
                lblStatusDescription.Text =
                    $"Nie wykryto ramek deauth, disassoc ani podejrzanych serii. Analiza nie wskazuje aktywnego zagrożenia.\n{wirelessSummary}";
            }
            else
            {
                panelStatus.BackColor = WinColor.FromArgb(24, 28, 52);
                lblStatusValue.Text = "BRAK DANYCH";
                lblStatusValue.ForeColor = WinColor.LightGray;
                lblStatusDescription.Text = "Oczekiwanie na analizę pliku...";
            }
        }

        private static string BuildWirelessSummary(AnalysisStatistics stats)
        {
            return string.Format(
                "Podsumowanie 802.11: Beacon {0}, Auth {1}, AssocReq {2}, AssocResp {3}.",
                stats.BeaconCount,
                stats.AuthenticationCount,
                stats.AssociationRequestCount,
                stats.AssociationResponseCount);
        }

        private static bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAuthAssocPacket(PacketRecord packet)
        {
            return packet != null &&
                   (packet.IsAuthentication ||
                    packet.IsAssociationRequest ||
                    packet.IsAssociationResponse);
        }

        private static bool MatchesFrameSelection(PacketRecord packet, string selection)
        {
            switch ((selection ?? string.Empty).Trim())
            {
                case "Beacon":
                    return packet.IsBeacon;
                case "Deauth":
                    return packet.IsDeauth;
                case "Disassoc":
                    return packet.IsDisassoc;
                case "Auth/Assoc":
                    return IsAuthAssocPacket(packet);
                case "Inne":
                    return !packet.IsBeacon &&
                           !packet.IsDeauth &&
                           !packet.IsDisassoc &&
                           !IsAuthAssocPacket(packet);
                default:
                    return true;
            }
        }

        private static string GetPacketSubtypeDisplay(PacketRecord packet)
        {
            if (packet == null)
                return "?";

            if (!string.IsNullOrWhiteSpace(packet.FrameSubtype))
                return packet.FrameSubtype;

            if (!string.IsNullOrWhiteSpace(packet.FrameType))
                return packet.FrameType;

            if (packet.IsBeacon)
                return "Beacon";
            if (packet.IsDeauth)
                return "Deauthentication";
            if (packet.IsDisassoc)
                return "Disassociation";
            if (packet.IsAuthentication)
                return "Authentication";
            if (packet.IsAssociationRequest)
                return "Association Request";
            if (packet.IsAssociationResponse)
                return "Association Response";

            return "?";
        }

        private static string FormatRisk(DetectionIncident incident)
        {
            if (incident == null)
                return "Low (0)";

            return string.Format("{0} ({1})", incident.RiskLevel, incident.RiskScore);
        }

        private static string FormatRisk(CorrelatedIncident incident)
        {
            if (incident == null)
                return "Low (0)";

            return string.Format("{0} ({1})", incident.RiskLevel, incident.RiskScore);
        }

        private static WinColor GetDashboardStatusColor(RiskLevel level)
        {
            switch (level)
            {
                case RiskLevel.Critical:
                    return WinColor.FromArgb(95, 18, 18);
                case RiskLevel.High:
                    return WinColor.FromArgb(74, 24, 24);
                case RiskLevel.Medium:
                    return WinColor.FromArgb(74, 52, 0);
                default:
                    return WinColor.FromArgb(18, 58, 32);
            }
        }

        private static WinColor GetDashboardAccentColor(RiskLevel level)
        {
            switch (level)
            {
                case RiskLevel.Critical:
                    return WinColor.FromArgb(255, 64, 64);
                case RiskLevel.High:
                    return WinColor.FromArgb(255, 80, 80);
                case RiskLevel.Medium:
                    return WinColor.FromArgb(255, 183, 0);
                default:
                    return WinColor.LimeGreen;
            }
        }

        private static Color GetPdfRiskColor(RiskLevel level)
        {
            switch (level)
            {
                case RiskLevel.Critical:
                    return Colors.DarkRed;
                case RiskLevel.High:
                    return Colors.IndianRed;
                case RiskLevel.Medium:
                    return Colors.DarkOrange;
                default:
                    return Colors.ForestGreen;
            }
        }

        private WinColor MapLogLevelToColor(AnalysisLogLevel level)
        {
            switch (level)
            {
                case AnalysisLogLevel.Info:
                    return WinColor.LightBlue;
                case AnalysisLogLevel.Notice:
                    return WinColor.Orange;
                case AnalysisLogLevel.Warning:
                    return WinColor.OrangeRed;
                case AnalysisLogLevel.Error:
                    return WinColor.IndianRed;
                default:
                    return WinColor.White;
            }
        }

        private void LogMessage(string level, string message, WinColor color, DateTime? timestamp = null)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke((MethodInvoker)(() => LogMessage(level, message, color, timestamp)));
                return;
            }

            rtbLogs.SelectionColor = color;
            rtbLogs.AppendText($"[{(timestamp ?? DateTime.Now):HH:mm:ss}] {level}: {message}\n");
            rtbLogs.ScrollToCaret();
        }

        private async void btnExportReport_Click(object sender, EventArgs e)
        {
            if (_currentResult == null)
            {
                LogMessage("WARN", "Najpierw załaduj i przeanalizuj plik PCAP.", WinColor.Orange);
                return;
            }

            if (isAnalysisRunning || isReportExportRunning)
            {
                LogMessage("WARN", "Poczekaj na zakończenie bieżącej operacji.", WinColor.Orange);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Plik PDF (*.pdf)|*.pdf";
                saveFileDialog.Title = "Zapisz raport PDF";
                saveFileDialog.FileName = $"raport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                var reportResult = _currentResult;
                string reportFilePath = currentFilePath;
                string reportLogs = rtbLogs.Text;

                isReportExportRunning = true;
                btnLoadPcap.Enabled = false;
                btnExportReport.Enabled = false;

                try
                {
                    await Task.Run(() => CreatePdfReport(
                        saveFileDialog.FileName,
                        reportResult,
                        reportFilePath,
                        reportLogs));

                    LogMessage("SUCCESS", $"Raport PDF zapisano: {saveFileDialog.FileName}", WinColor.Lime);
                }
                catch (Exception ex)
                {
                    LogMessage("ERROR", $"Nie udało się zapisać raportu PDF. ({ex.Message})", WinColor.Red);
                }
                finally
                {
                    isReportExportRunning = false;
                    btnLoadPcap.Enabled = !isAnalysisRunning;
                    btnExportReport.Enabled = _currentResult != null && !isAnalysisRunning;
                }
            }
        }

        private void CreatePdfReport(
            string outputPath,
            AnalysisResult reportResult,
            string reportFilePath,
            string reportLogs)
        {
            var result = reportResult ?? new AnalysisResult();
            var stats = result.Statistics ?? new AnalysisStatistics();
            var packets = result.Packets ?? new List<PacketRecord>();
            var incidents = result.Incidents ?? new List<DetectionIncident>();
            var correlatedIncidents = result.CorrelatedIncidents ?? new List<CorrelatedIncident>();

            var uniqueSsids = packets
                .Where(p => !string.IsNullOrWhiteSpace(p.Ssid) && p.Ssid != "?")
                .Select(p => p.Ssid)
                .Distinct()
                .Count();

            var uniqueBssids = packets
                .Where(p => !string.IsNullOrWhiteSpace(p.Bssid) && p.Bssid != "?")
                .Select(p => p.Bssid)
                .Distinct()
                .Count();

            var uniqueChannels = packets
                .Where(p => p.Channel.HasValue)
                .Select(p => p.Channel.Value)
                .Distinct()
                .Count();

            var topChannels = packets
                .Where(p => p.Channel.HasValue)
                .GroupBy(p => p.Channel.Value)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => string.Format("ch {0} ({1})", g.Key, g.Count()))
                .ToList();

            string topChannelsText = topChannels.Any()
                ? string.Join(", ", topChannels)
                : "brak danych";

            bool denseEnvironment = uniqueSsids >= 4 || uniqueBssids >= 6 || uniqueChannels >= 3;

            bool hasBeaconBurst = incidents.Any(i => i.RuleId == "BEACON_BURST");
            bool hasAuthAssocBurst = incidents.Any(i => i.RuleId == "AUTH_ASSOC_BURST");
            bool hasDeauthBurst = incidents.Any(i => i.RuleId == "DEAUTH_BURST");
            bool hasDisassocBurst = incidents.Any(i => i.RuleId == "DISASSOC_BURST");
            bool hasEvilTwin = incidents.Any(i => i.RuleId == "EVIL_TWIN_HEURISTIC");

            DetectionIncident highestRiskIncident = incidents.OrderByDescending(i => i.RiskScore).FirstOrDefault();
            CorrelatedIncident highestCorrelatedIncident = correlatedIncidents.OrderByDescending(i => i.RiskScore).FirstOrDefault();

            string highestOverallRisk = "Brak incydentów";
            if (highestRiskIncident != null)
                highestOverallRisk = FormatRisk(highestRiskIncident);

            if (highestCorrelatedIncident != null &&
                (highestRiskIncident == null || highestCorrelatedIncident.RiskScore >= highestRiskIncident.RiskScore))
            {
                highestOverallRisk = FormatRisk(highestCorrelatedIncident);
            }

            var document = new Document();
            document.Info.Title = "Raport analizy bezpieczeństwa Wi-Fi";
            document.Info.Subject = "Analiza ramek 802.11";
            document.Info.Author = "NetQin";

            DefinePdfStyles(document);

            Section section = document.AddSection();
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);

            Paragraph title = section.AddParagraph("Raport analizy bezpieczeństwa sieci bezprzewodowej");
            title.Style = "ReportTitle";

            Paragraph subtitle = section.AddParagraph(string.Format(
                "Plik: {0} | Wygenerowano: {1:yyyy-MM-dd HH:mm:ss}",
                Path.GetFileName(string.IsNullOrWhiteSpace(reportFilePath) ? result.FilePath : reportFilePath),
                DateTime.Now));
            subtitle.Style = "ReportSubtitle";
            subtitle.Format.SpaceAfter = Unit.FromCentimeter(0.4);

            section.AddParagraph("1. Podsumowanie analizy", "SectionHeading");

            Table summaryTable = section.AddTable();
            summaryTable.Borders.Width = 0.5;
            summaryTable.Borders.Color = Colors.LightGray;
            summaryTable.Rows.LeftIndent = 0;

            summaryTable.AddColumn(Unit.FromCentimeter(6));
            summaryTable.AddColumn(Unit.FromCentimeter(10));

            AddKeyValueRow(summaryTable, "Plik", string.IsNullOrWhiteSpace(reportFilePath) ? result.FilePath : reportFilePath);
            AddKeyValueRow(summaryTable, "Łączna liczba pakietów", stats.TotalPackets.ToString());
            AddKeyValueRow(summaryTable, "Ramki deautoryzacji", stats.DeauthCount.ToString());
            AddKeyValueRow(summaryTable, "Ramki disassociation", stats.DisassocCount.ToString());
            AddKeyValueRow(summaryTable, "Ramki beacon", stats.BeaconCount.ToString());
            AddKeyValueRow(summaryTable, "Ramki authentication", stats.AuthenticationCount.ToString());
            AddKeyValueRow(summaryTable, "Association request", stats.AssociationRequestCount.ToString());
            AddKeyValueRow(summaryTable, "Association response", stats.AssociationResponseCount.ToString());
            AddKeyValueRow(summaryTable, "Unikalne SSID", uniqueSsids.ToString());
            AddKeyValueRow(summaryTable, "Unikalne BSSID", uniqueBssids.ToString());
            AddKeyValueRow(summaryTable, "Wykryte incydenty", stats.SuspiciousBurstCount.ToString());
            AddKeyValueRow(summaryTable, "Incydenty skorelowane", correlatedIncidents.Count.ToString());
            AddKeyValueRow(summaryTable, "Najwyższe ryzyko", highestOverallRisk);
            AddKeyValueRow(summaryTable, "Błędy parsowania", stats.ParseErrorCount.ToString());

            section.AddParagraph();
            section.AddParagraph("2. Charakterystyka środowiska", "SectionHeading");

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

            AddStatusRow(statusTable, "Liczba unikalnych SSID", uniqueSsids.ToString());
            AddStatusRow(statusTable, "Liczba unikalnych BSSID", uniqueBssids.ToString());
            AddStatusRow(statusTable, "Liczba unikalnych kanałów", uniqueChannels.ToString());
            AddStatusRow(statusTable, "Dominujące kanały", topChannelsText);
            AddStatusRow(statusTable, "Charakter środowiska", denseEnvironment ? "Zagęszczone / wielo-AP" : "Prostsze / mniej zagęszczone");
            AddStatusRow(statusTable, "Błędy parsowania", stats.ParseErrorCount > 0 ? "Obecne" : "Brak");

            section.AddParagraph();
            section.AddParagraph("3. Ocena ryzyka", "SectionHeading");

            string riskNarrative;

            if (correlatedIncidents.Any())
            {
                riskNarrative = string.Format(
                    "W analizowanym ruchu wykryto {0} incydent(y) skorelowane, co podnosi wiarygodność oceny ryzyka. " +
                    "Najwyższy poziom ryzyka oszacowano jako: {1}. " +
                    "Korelacja wielu sygnałów sugeruje, że zaobserwowane zdarzenia nie mają wyłącznie charakteru przypadkowego.",
                    correlatedIncidents.Count,
                    highestOverallRisk);
            }
            else if (incidents.Any())
            {
                riskNarrative = string.Format(
                    "W analizowanym ruchu wykryto {0} incydent(y) regułowych. " +
                    "Najwyższy poziom ryzyka oszacowano jako: {1}. " +
                    "Ocena została oparta na wzorcach ruchu 802.11 i powinna być interpretowana jako wskazanie podwyższonego ryzyka, a nie samodzielny dowód ataku.",
                    incidents.Count,
                    highestOverallRisk);
            }
            else
            {
                riskNarrative = "Nie wykryto incydentów spełniających zdefiniowane reguły alarmowe. Oznacza to brak przesłanek do podniesienia poziomu ryzyka na podstawie badanego zrzutu.";
            }

            Paragraph riskParagraph = section.AddParagraph(riskNarrative);
            riskParagraph.Style = "Normal";
            riskParagraph.Format.SpaceAfter = Unit.FromCentimeter(0.2);

            section.AddParagraph();
            section.AddParagraph("4. Wykryte incydenty regułowe", "SectionHeading");

            if (incidents.Any())
            {
                Table incidentTable = section.AddTable();
                incidentTable.Borders.Width = 0.5;
                incidentTable.Borders.Color = Colors.LightGray;
                incidentTable.Rows.LeftIndent = 0;

                incidentTable.AddColumn(Unit.FromCentimeter(2.3));
                incidentTable.AddColumn(Unit.FromCentimeter(3.2));
                incidentTable.AddColumn(Unit.FromCentimeter(2.8));
                incidentTable.AddColumn(Unit.FromCentimeter(4.3));
                incidentTable.AddColumn(Unit.FromCentimeter(1.4));
                incidentTable.AddColumn(Unit.FromCentimeter(2.4));

                Row incidentHeader = incidentTable.AddRow();
                incidentHeader.Shading.Color = Colors.DarkSlateBlue;
                incidentHeader.Format.Font.Color = Colors.White;
                incidentHeader.Format.Font.Bold = true;

                incidentHeader.Cells[0].AddParagraph("Reguła");
                incidentHeader.Cells[1].AddParagraph("Ryzyko");
                incidentHeader.Cells[2].AddParagraph("Źródło");
                incidentHeader.Cells[3].AddParagraph("SSID / BSSID");
                incidentHeader.Cells[4].AddParagraph("Ilość");
                incidentHeader.Cells[5].AddParagraph("Czas");

                foreach (var incident in incidents.OrderByDescending(i => i.RiskScore).ThenBy(i => i.StartTime))
                {
                    AddIncidentRow(incidentTable, incident);
                }

                section.AddParagraph();
                Paragraph baseDetailsHeader = section.AddParagraph("Szczegóły incydentów regułowych");
                baseDetailsHeader.Format.Font.Bold = true;
                baseDetailsHeader.Format.SpaceBefore = Unit.FromCentimeter(0.2);
                baseDetailsHeader.Format.SpaceAfter = Unit.FromCentimeter(0.12);

                foreach (var incident in incidents.OrderByDescending(i => i.RiskScore).ThenBy(i => i.StartTime))
                {
                    Paragraph p = section.AddParagraph();
                    p.Style = "LogText";

                    p.AddFormattedText(
                        string.Format("[{0}] {1}", incident.RuleId, incident.Title),
                        TextFormat.Bold);

                    p.AddLineBreak();
                    p.AddText(string.Format(
                        "Źródło: {0} | Cel: {1} | SSID: {2} | BSSID: {3} | Reason: {4} | Pakiety: {5}",
                        string.IsNullOrWhiteSpace(incident.SourceMac) ? "?" : incident.SourceMac,
                        string.IsNullOrWhiteSpace(incident.TargetMac) ? "?" : incident.TargetMac,
                        string.IsNullOrWhiteSpace(incident.Ssid) ? "?" : incident.Ssid,
                        string.IsNullOrWhiteSpace(incident.Bssid) ? "?" : incident.Bssid,
                        string.IsNullOrWhiteSpace(incident.ReasonCode) ? "?" : incident.ReasonCode,
                        incident.PacketCount));

                    p.AddLineBreak();
                    p.AddText(string.Format(
                        "Zakres czasu: {0:HH:mm:ss.fff} - {1:HH:mm:ss.fff}",
                        incident.StartTime,
                        incident.EndTime));

                    p.AddLineBreak();
                    p.AddText("Ryzyko: " + FormatRisk(incident));

                    p.AddLineBreak();
                    p.AddText("Tagi: " + incident.TagsDisplay);

                    p.AddLineBreak();
                    p.AddText("Opis: " + incident.Description);

                    p.AddLineBreak();
                    p.AddText("Znaczenie analityczne: " + GetAnalyticalMeaningForIncident(incident));

                    if (!string.IsNullOrWhiteSpace(incident.Recommendation))
                    {
                        p.AddLineBreak();
                        p.AddText("Rekomendacja: " + incident.Recommendation);
                    }
                }
            }
            else
            {
                Paragraph noIncidents = section.AddParagraph("Nie wykryto incydentów spełniających zdefiniowane reguły.");
                noIncidents.Style = "LogText";
            }

            section.AddParagraph();
            section.AddParagraph("5. Incydenty skorelowane", "SectionHeading");

            if (correlatedIncidents.Any())
            {
                foreach (var correlated in correlatedIncidents.OrderByDescending(i => i.RiskScore).ThenBy(i => i.StartTime))
                {
                    Paragraph p = section.AddParagraph();
                    p.Style = "LogText";

                    p.AddFormattedText(
                        string.Format("[{0}] {1}", correlated.CorrelationType, correlated.Title),
                        TextFormat.Bold);

                    p.AddLineBreak();
                    p.AddText(string.Format(
                        "SSID: {0} | BSSID: {1} | Źródło: {2}",
                        string.IsNullOrWhiteSpace(correlated.Ssid) ? "?" : correlated.Ssid,
                        string.IsNullOrWhiteSpace(correlated.Bssid) ? "?" : correlated.Bssid,
                        string.IsNullOrWhiteSpace(correlated.SourceMac) ? "?" : correlated.SourceMac));

                    p.AddLineBreak();
                    p.AddText(string.Format(
                        "Zakres czasu: {0:HH:mm:ss.fff} - {1:HH:mm:ss.fff}",
                        correlated.StartTime,
                        correlated.EndTime));

                    p.AddLineBreak();
                    p.AddText("Ryzyko: " + FormatRisk(correlated));

                    p.AddLineBreak();
                    p.AddText("Reguły źródłowe: " + correlated.SourceRulesDisplay);

                    p.AddLineBreak();
                    p.AddText("Tagi: " + correlated.TagsDisplay);

                    p.AddLineBreak();
                    p.AddText("Opis: " + correlated.Description);

                    p.AddLineBreak();
                    p.AddText("Znaczenie analityczne: " + GetAnalyticalMeaningForCorrelation(correlated));

                    if (!string.IsNullOrWhiteSpace(correlated.Recommendation))
                    {
                        p.AddLineBreak();
                        p.AddText("Rekomendacja: " + correlated.Recommendation);
                    }
                }
            }
            else
            {
                Paragraph noCorrelations = section.AddParagraph("Nie wykryto incydentów skorelowanych.");
                noCorrelations.Style = "LogText";
            }

            section.AddParagraph();
            section.AddParagraph("6. Rekomendacje", "SectionHeading");

            List<string> recommendations = BuildGlobalRecommendations(incidents, correlatedIncidents);

            if (recommendations.Any())
            {
                foreach (string rec in recommendations)
                {
                    Paragraph recParagraph = section.AddParagraph("• " + rec);
                    recParagraph.Style = "Normal";
                }
            }
            else
            {
                Paragraph noRec = section.AddParagraph("Na podstawie badanego zrzutu nie sformułowano dodatkowych rekomendacji operacyjnych.");
                noRec.Style = "Normal";
            }

            section.AddParagraph();
            section.AddParagraph("7. Ograniczenia analizy", "SectionHeading");

            Paragraph limitations = section.AddParagraph(
                "Analiza została przeprowadzona wyłącznie na podstawie dostarczonego zrzutu PCAP/PCAPNG. " +
                "Wynik zależy od kompletności przechwyconego ruchu, poprawności dekodowania ramek 802.11 oraz dostępności metadanych takich jak SSID, BSSID i kanał. " +
                "Wykrycia heurystyczne, w szczególności związane z Evil Twin, mogą generować fałszywe alarmy w środowiskach z wieloma legalnymi punktami dostępowymi działającymi pod tym samym SSID. " +
                "Raport nie stanowi samodzielnego potwierdzenia ataku i powinien być interpretowany łącznie z logami AP, kontrolera oraz wiedzą o legalnej infrastrukturze.");
            limitations.Style = "Normal";
            limitations.Format.SpaceAfter = Unit.FromCentimeter(0.2);

            section.AddParagraph();
            section.AddParagraph("8. Wnioski końcowe", "SectionHeading");

            string conclusion;
            if (correlatedIncidents.Any())
            {
                conclusion = string.Format(
                    "W badanym materiale wykryto zarówno incydenty regułowe, jak i incydenty skorelowane. " +
                    "Najwyższe oszacowane ryzyko wyniosło: {0}. " +
                    "Obecność korelacji zwiększa istotność analityczną wykrytych zdarzeń i wskazuje na potrzebę dalszej weryfikacji z użyciem danych z infrastruktury sieciowej.",
                    highestOverallRisk);
            }
            else if (incidents.Any())
            {
                conclusion = string.Format(
                    "W badanym materiale wykryto incydenty regułowe o najwyższym poziomie ryzyka: {0}. " +
                    "Zaobserwowane wzorce mogą wskazywać na anomalie bezpieczeństwa w sieci bezprzewodowej, jednak ich interpretacja wymaga uwzględnienia kontekstu środowiska i legalnej infrastruktury.",
                    highestOverallRisk);
            }
            else if (stats.DeauthCount > 0 || stats.DisassocCount > 0)
            {
                conclusion = "Zaobserwowano pojedyncze zdarzenia rozłączające, ale bez wykrycia serii spełniających próg alarmowy. Wynik nie daje podstaw do sformułowania silnego alarmu, ale uzasadnia ostrożną obserwację środowiska.";
            }
            else
            {
                conclusion = "W badanym materiale nie wykryto zdarzeń spełniających zdefiniowane reguły alarmowe. Na podstawie analizowanego zrzutu nie stwierdzono przesłanek do podniesienia oceny ryzyka.";
            }

            Paragraph conclusionParagraph = section.AddParagraph(conclusion);
            conclusionParagraph.Style = "Normal";
            conclusionParagraph.Format.SpaceAfter = Unit.FromCentimeter(0.3);

            section.AddParagraph();
            section.AddParagraph("9. Logi analizy", "SectionHeading");

            string[] logLines = (reportLogs ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

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

                    if (line.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
                        logParagraph.Format.Font.Color = Colors.DarkRed;
                    else if (line.IndexOf("CORRELATED", StringComparison.OrdinalIgnoreCase) >= 0)
                        logParagraph.Format.Font.Color = Colors.DarkRed;
                    else if (line.IndexOf("ALERT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             line.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0)
                        logParagraph.Format.Font.Color = Colors.Red;
                    else if (line.IndexOf("NOTICE", StringComparison.OrdinalIgnoreCase) >= 0)
                        logParagraph.Format.Font.Color = Colors.DarkOrange;
                    else if (line.IndexOf("SUCCESS", StringComparison.OrdinalIgnoreCase) >= 0)
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

        private static string GetAnalyticalMeaningForIncident(DetectionIncident incident)
        {
            if (incident == null)
                return "Brak danych.";

            switch (incident.RuleId)
            {
                case "EVIL_TWIN_HEURISTIC":
                    return "Wykryto wzorzec zgodny z możliwą próbą podszycia się pod legalny punkt dostępowy. Wynik ma charakter heurystyczny i wymaga porównania z inwentaryzacją infrastruktury.";
                case "DEAUTH_BURST":
                    return "Seria ramek deautoryzacji może wskazywać na próbę wymuszenia rozłączenia klientów lub zakłócenie pracy sieci.";
                case "DISASSOC_BURST":
                    return "Seria ramek disassociation może oznaczać niestandardowe rozłączanie klientów i wymaga dalszej weryfikacji źródła.";
                case "AUTH_ASSOC_BURST":
                    return "Podwyższona liczba ramek uwierzytelnienia lub asocjacji może wskazywać na nietypową aktywność klientów albo próbę wymuszenia ponownych połączeń.";
                case "BEACON_BURST":
                    return "Nadmierna liczba ramek beacon może wskazywać na anomalię środowiska radiowego lub próbę generowania fałszywej obecności sieci.";
                default:
                    return "Wykryto wzorzec ruchu odbiegający od przyjętych progów analitycznych.";
            }
        }

        private static string GetAnalyticalMeaningForCorrelation(CorrelatedIncident incident)
        {
            if (incident == null)
                return "Brak danych.";

            switch (incident.CorrelationType)
            {
                case "FORCED_RECONNECT":
                    return "Korelacja sugeruje scenariusz wymuszonego ponownego łączenia klientów po wcześniejszych zdarzeniach rozłączających.";
                case "FAKE_AP_CAMPAIGN":
                    return "Korelacja sugeruje współwystępowanie sygnałów zgodnych z kampanią fałszywego punktu dostępowego.";
                default:
                    return "Korelacja zwiększa istotność analityczną wykrytych sygnałów względem pojedynczych incydentów.";
            }
        }

        private static List<string> BuildGlobalRecommendations(
            List<DetectionIncident> incidents,
            List<CorrelatedIncident> correlatedIncidents)
        {
            var result = new List<string>();

            if (incidents.Any(i => i.RuleId == "EVIL_TWIN_HEURISTIC"))
            {
                result.Add("Zweryfikować legalność zaobserwowanych BSSID oraz porównać je z inwentaryzacją punktów dostępowych.");
                result.Add("Porównać SSID, BSSID i kanał z konfiguracją legalnej infrastruktury.");
            }

            if (incidents.Any(i => i.RuleId == "DEAUTH_BURST"))
            {
                result.Add("Sprawdzić źródło ramek deautoryzacji i zweryfikować zachowanie punktu dostępowego oraz klientów.");
            }

            if (incidents.Any(i => i.RuleId == "DISASSOC_BURST"))
            {
                result.Add("Zweryfikować, czy rozłączenia klientów wynikają z legalnego działania AP, czy z aktywności nieautoryzowanego nadawcy.");
            }

            if (incidents.Any(i => i.RuleId == "AUTH_ASSOC_BURST"))
            {
                result.Add("Sprawdzić, czy wzmożona liczba prób uwierzytelnienia i asocjacji nie wynika z awarii klientów, błędnej konfiguracji lub celowego wymuszania reconnectów.");
            }

            if (incidents.Any(i => i.RuleId == "BEACON_BURST"))
            {
                result.Add("Zweryfikować, czy wzmożona emisja beaconów wynika z legalnej infrastruktury, testów laboratoryjnych czy niestandardowej aktywności radiowej.");
            }

            if (correlatedIncidents.Any(i => i.CorrelationType == "FORCED_RECONNECT"))
            {
                result.Add("Porównać wyniki analizy z logami AP i procesem autoryzacji klientów w celu potwierdzenia wymuszonych ponownych połączeń.");
            }

            if (correlatedIncidents.Any(i => i.CorrelationType == "FAKE_AP_CAMPAIGN"))
            {
                result.Add("Sprawdzić zgodność SSID, BSSID i kanału z legalną infrastrukturą oraz potwierdzić obserwacje dodatkowymi źródłami danych.");
            }

            return result.Distinct().ToList();
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

            if (string.Equals(value, "Tak", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Obecne", StringComparison.OrdinalIgnoreCase))
            {
                row.Cells[1].Format.Font.Color = Colors.DarkRed;
            }
            else if (string.Equals(value, "Nie", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(value, "Brak", StringComparison.OrdinalIgnoreCase))
            {
                row.Cells[1].Format.Font.Color = Colors.ForestGreen;
            }
            else
            {
                row.Cells[1].Format.Font.Color = Colors.Black;
            }
        }

        private void AddIncidentRow(Table table, DetectionIncident incident)
        {
            Row row = table.AddRow();
            row.TopPadding = 4;
            row.BottomPadding = 4;

            row.Cells[0].AddParagraph(incident.RuleId);
            row.Cells[1].AddParagraph(FormatRisk(incident));
            row.Cells[2].AddParagraph(string.IsNullOrWhiteSpace(incident.SourceMac) ? "?" : incident.SourceMac);

            string ssidBssid = string.Format(
                "{0}\n{1}",
                string.IsNullOrWhiteSpace(incident.Ssid) ? "SSID: ?" : "SSID: " + incident.Ssid,
                string.IsNullOrWhiteSpace(incident.Bssid) ? "BSSID: ?" : "BSSID: " + incident.Bssid);

            row.Cells[3].AddParagraph(ssidBssid);
            row.Cells[4].AddParagraph(incident.PacketCount.ToString());
            row.Cells[5].AddParagraph(string.Format(
                "{0:HH:mm:ss.fff}\n{1:HH:mm:ss.fff}",
                incident.StartTime,
                incident.EndTime));

            row.Cells[1].Format.Font.Color = GetPdfRiskColor(incident.RiskLevel);
        }
    }
}
