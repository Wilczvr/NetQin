namespace NetQin
{
    partial class Form1
    {
        /// <summary>
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Wyczyść wszystkie używane zasoby.
        /// </summary>
        /// <param name="disposing">prawda, jeżeli zarządzane zasoby powinny zostać zlikwidowane; Fałsz w przeciwnym wypadku.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Kod generowany przez Projektanta formularzy systemu Windows

        /// <summary>
        /// Metoda wymagana do obsługi projektanta — nie należy modyfikować
        /// jej zawartości w edytorze kodu.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnLoadPcap = new System.Windows.Forms.Button();
            this.btnExportReport = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.dgvPackets = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rtbLogs = new System.Windows.Forms.RichTextBox();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblFileCaption = new System.Windows.Forms.Label();
            this.panelPackets = new System.Windows.Forms.Panel();
            this.lblPacketsCaption = new System.Windows.Forms.Label();
            this.lblPacketCount = new System.Windows.Forms.Label();
            this.panelDeauth = new System.Windows.Forms.Panel();
            this.lblDeauthCaption = new System.Windows.Forms.Label();
            this.lblDeauthCount = new System.Windows.Forms.Label();
            this.panelSuspicious = new System.Windows.Forms.Panel();
            this.lblSuspiciousCaption = new System.Windows.Forms.Label();
            this.lblSuspiciousCount = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.lblStatusDescription = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.lblStatusTitle = new System.Windows.Forms.Label();
            this.lblTableTitle = new System.Windows.Forms.Label();
            this.lblLogsTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPackets)).BeginInit();
            this.panelPackets.SuspendLayout();
            this.panelDeauth.SuspendLayout();
            this.panelSuspicious.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnLoadPcap
            // 
            this.btnLoadPcap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(49)))), ((int)(((byte)(188)))));
            this.btnLoadPcap.FlatAppearance.BorderSize = 0;
            this.btnLoadPcap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadPcap.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnLoadPcap.ForeColor = System.Drawing.Color.White;
            this.btnLoadPcap.Location = new System.Drawing.Point(738, 38);
            this.btnLoadPcap.Name = "btnLoadPcap";
            this.btnLoadPcap.Size = new System.Drawing.Size(190, 52);
            this.btnLoadPcap.TabIndex = 0;
            this.btnLoadPcap.Text = "Importuj plik PCAP";
            this.btnLoadPcap.UseVisualStyleBackColor = false;
            this.btnLoadPcap.Click += new System.EventHandler(this.btnLoadPcap_Click);
            // 
            // btnExportReport
            // 
            this.btnExportReport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(84)))), ((int)(((byte)(25)))), ((int)(((byte)(130)))));
            this.btnExportReport.Enabled = false;
            this.btnExportReport.FlatAppearance.BorderSize = 0;
            this.btnExportReport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportReport.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnExportReport.ForeColor = System.Drawing.Color.White;
            this.btnExportReport.Location = new System.Drawing.Point(1155, 38);
            this.btnExportReport.Name = "btnExportReport";
            this.btnExportReport.Size = new System.Drawing.Size(183, 52);
            this.btnExportReport.TabIndex = 1;
            this.btnExportReport.Text = "Generuj raport";
            this.btnExportReport.UseVisualStyleBackColor = false;
            this.btnExportReport.Click += new System.EventHandler(this.btnExportReport_Click);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.textBox1.ForeColor = System.Drawing.Color.White;
            this.textBox1.Location = new System.Drawing.Point(930, 59);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(210, 30);
            this.textBox1.TabIndex = 2;
            // 
            // dgvPackets
            // 
            this.dgvPackets.AllowUserToAddRows = false;
            this.dgvPackets.AllowUserToDeleteRows = false;
            this.dgvPackets.AllowUserToResizeRows = false;
            this.dgvPackets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPackets.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(24)))), ((int)(((byte)(44)))));
            this.dgvPackets.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvPackets.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvPackets.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(48)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(48)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvPackets.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPackets.ColumnHeadersHeight = 36;
            this.dgvPackets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvPackets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(64)))), ((int)(((byte)(110)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvPackets.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvPackets.EnableHeadersVisualStyles = false;
            this.dgvPackets.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(58)))), ((int)(((byte)(90)))));
            this.dgvPackets.Location = new System.Drawing.Point(30, 184);
            this.dgvPackets.MultiSelect = false;
            this.dgvPackets.Name = "dgvPackets";
            this.dgvPackets.ReadOnly = true;
            this.dgvPackets.RowHeadersVisible = false;
            this.dgvPackets.RowHeadersWidth = 51;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.dgvPackets.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvPackets.RowTemplate.Height = 34;
            this.dgvPackets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPackets.Size = new System.Drawing.Size(920, 370);
            this.dgvPackets.TabIndex = 3;
            // 
            // Column1
            // 
            this.Column1.FillWeight = 12F;
            this.Column1.HeaderText = "Nr";
            this.Column1.MinimumWidth = 6;
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.FillWeight = 18F;
            this.Column2.HeaderText = "Czas";
            this.Column2.MinimumWidth = 6;
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.FillWeight = 24F;
            this.Column3.HeaderText = "MAC Nadawcy";
            this.Column3.MinimumWidth = 6;
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // Column4
            // 
            this.Column4.FillWeight = 24F;
            this.Column4.HeaderText = "MAC Odbiorcy";
            this.Column4.MinimumWidth = 6;
            this.Column4.Name = "Column4";
            this.Column4.ReadOnly = true;
            // 
            // Column5
            // 
            this.Column5.FillWeight = 62F;
            this.Column5.HeaderText = "Info";
            this.Column5.MinimumWidth = 6;
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // rtbLogs
            // 
            this.rtbLogs.BackColor = System.Drawing.Color.Black;
            this.rtbLogs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbLogs.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.rtbLogs.ForeColor = System.Drawing.Color.Lime;
            this.rtbLogs.Location = new System.Drawing.Point(30, 689);
            this.rtbLogs.Name = "rtbLogs";
            this.rtbLogs.Size = new System.Drawing.Size(1308, 170);
            this.rtbLogs.TabIndex = 4;
            this.rtbLogs.Text = "";
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(34, 28);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Size = new System.Drawing.Size(136, 50);
            this.lblAppTitle.TabIndex = 5;
            this.lblAppTitle.Text = "NetQin";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblSubtitle.Location = new System.Drawing.Point(39, 82);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(241, 23);
            this.lblSubtitle.TabIndex = 6;
            this.lblSubtitle.Text = "Wi-Fi Threat Analysis Console";
            // 
            // lblFileCaption
            // 
            this.lblFileCaption.AutoSize = true;
            this.lblFileCaption.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblFileCaption.ForeColor = System.Drawing.Color.White;
            this.lblFileCaption.Location = new System.Drawing.Point(930, 35);
            this.lblFileCaption.Name = "lblFileCaption";
            this.lblFileCaption.Size = new System.Drawing.Size(90, 21);
            this.lblFileCaption.TabIndex = 7;
            this.lblFileCaption.Text = "Nazwa pliku";
            // 
            // panelPackets
            // 
            this.panelPackets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            this.panelPackets.Controls.Add(this.lblPacketsCaption);
            this.panelPackets.Controls.Add(this.lblPacketCount);
            this.panelPackets.Location = new System.Drawing.Point(978, 184);
            this.panelPackets.Name = "panelPackets";
            this.panelPackets.Size = new System.Drawing.Size(360, 96);
            this.panelPackets.TabIndex = 8;
            // 
            // lblPacketsCaption
            // 
            this.lblPacketsCaption.AutoSize = true;
            this.lblPacketsCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblPacketsCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblPacketsCaption.Location = new System.Drawing.Point(24, 59);
            this.lblPacketsCaption.Name = "lblPacketsCaption";
            this.lblPacketsCaption.Size = new System.Drawing.Size(68, 23);
            this.lblPacketsCaption.TabIndex = 1;
            this.lblPacketsCaption.Text = "Pakiety";
            // 
            // lblPacketCount
            // 
            this.lblPacketCount.AutoSize = true;
            this.lblPacketCount.Font = new System.Drawing.Font("Segoe UI Semibold", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblPacketCount.ForeColor = System.Drawing.Color.White;
            this.lblPacketCount.Location = new System.Drawing.Point(18, 6);
            this.lblPacketCount.Name = "lblPacketCount";
            this.lblPacketCount.Size = new System.Drawing.Size(47, 54);
            this.lblPacketCount.TabIndex = 0;
            this.lblPacketCount.Text = "0";
            // 
            // panelDeauth
            // 
            this.panelDeauth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            this.panelDeauth.Controls.Add(this.lblDeauthCaption);
            this.panelDeauth.Controls.Add(this.lblDeauthCount);
            this.panelDeauth.Location = new System.Drawing.Point(978, 298);
            this.panelDeauth.Name = "panelDeauth";
            this.panelDeauth.Size = new System.Drawing.Size(360, 96);
            this.panelDeauth.TabIndex = 9;
            // 
            // lblDeauthCaption
            // 
            this.lblDeauthCaption.AutoSize = true;
            this.lblDeauthCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblDeauthCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblDeauthCaption.Location = new System.Drawing.Point(24, 59);
            this.lblDeauthCaption.Name = "lblDeauthCaption";
            this.lblDeauthCaption.Size = new System.Drawing.Size(122, 23);
            this.lblDeauthCaption.TabIndex = 1;
            this.lblDeauthCaption.Text = "Deautoryzacje";
            // 
            // lblDeauthCount
            // 
            this.lblDeauthCount.AutoSize = true;
            this.lblDeauthCount.Font = new System.Drawing.Font("Segoe UI Semibold", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblDeauthCount.ForeColor = System.Drawing.Color.White;
            this.lblDeauthCount.Location = new System.Drawing.Point(18, 6);
            this.lblDeauthCount.Name = "lblDeauthCount";
            this.lblDeauthCount.Size = new System.Drawing.Size(47, 54);
            this.lblDeauthCount.TabIndex = 0;
            this.lblDeauthCount.Text = "0";
            // 
            // panelSuspicious
            // 
            this.panelSuspicious.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            this.panelSuspicious.Controls.Add(this.lblSuspiciousCaption);
            this.panelSuspicious.Controls.Add(this.lblSuspiciousCount);
            this.panelSuspicious.Location = new System.Drawing.Point(978, 412);
            this.panelSuspicious.Name = "panelSuspicious";
            this.panelSuspicious.Size = new System.Drawing.Size(360, 96);
            this.panelSuspicious.TabIndex = 10;
            // 
            // lblSuspiciousCaption
            // 
            this.lblSuspiciousCaption.AutoSize = true;
            this.lblSuspiciousCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblSuspiciousCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblSuspiciousCaption.Location = new System.Drawing.Point(24, 59);
            this.lblSuspiciousCaption.Name = "lblSuspiciousCaption";
            this.lblSuspiciousCaption.Size = new System.Drawing.Size(145, 23);
            this.lblSuspiciousCaption.TabIndex = 1;
            this.lblSuspiciousCaption.Text = "Podejrzane serie";
            // 
            // lblSuspiciousCount
            // 
            this.lblSuspiciousCount.AutoSize = true;
            this.lblSuspiciousCount.Font = new System.Drawing.Font("Segoe UI Semibold", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblSuspiciousCount.ForeColor = System.Drawing.Color.White;
            this.lblSuspiciousCount.Location = new System.Drawing.Point(18, 6);
            this.lblSuspiciousCount.Name = "lblSuspiciousCount";
            this.lblSuspiciousCount.Size = new System.Drawing.Size(47, 54);
            this.lblSuspiciousCount.TabIndex = 0;
            this.lblSuspiciousCount.Text = "0";
            // 
            // panelStatus
            // 
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(28)))), ((int)(((byte)(52)))));
            this.panelStatus.Controls.Add(this.lblStatusDescription);
            this.panelStatus.Controls.Add(this.lblStatusValue);
            this.panelStatus.Controls.Add(this.lblStatusTitle);
            this.panelStatus.Location = new System.Drawing.Point(978, 526);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(360, 126);
            this.panelStatus.TabIndex = 11;
            this.panelStatus.Visible = true;
            // 
            // lblStatusDescription
            // 
            this.lblStatusDescription.AutoSize = true;
            this.lblStatusDescription.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblStatusDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblStatusDescription.Location = new System.Drawing.Point(23, 83);
            this.lblStatusDescription.MaximumSize = new System.Drawing.Size(320, 0);
            this.lblStatusDescription.Name = "lblStatusDescription";
            this.lblStatusDescription.Size = new System.Drawing.Size(226, 21);
            this.lblStatusDescription.TabIndex = 2;
            this.lblStatusDescription.Text = "Oczekiwanie na analizę pliku...";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblStatusValue.ForeColor = System.Drawing.Color.LightGray;
            this.lblStatusValue.Location = new System.Drawing.Point(20, 35);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(188, 41);
            this.lblStatusValue.TabIndex = 1;
            this.lblStatusValue.Text = "BRAK DANYCH";
            // 
            // lblStatusTitle
            // 
            this.lblStatusTitle.AutoSize = true;
            this.lblStatusTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblStatusTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(181)))), ((int)(((byte)(215)))));
            this.lblStatusTitle.Location = new System.Drawing.Point(23, 9);
            this.lblStatusTitle.Name = "lblStatusTitle";
            this.lblStatusTitle.Size = new System.Drawing.Size(116, 23);
            this.lblStatusTitle.TabIndex = 0;
            this.lblStatusTitle.Text = "Ocena analizy";
            // 
            // lblTableTitle
            // 
            this.lblTableTitle.AutoSize = true;
            this.lblTableTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblTableTitle.ForeColor = System.Drawing.Color.White;
            this.lblTableTitle.Location = new System.Drawing.Point(25, 151);
            this.lblTableTitle.Name = "lblTableTitle";
            this.lblTableTitle.Size = new System.Drawing.Size(129, 25);
            this.lblTableTitle.TabIndex = 12;
            this.lblTableTitle.Text = "Ruch pakietów";
            // 
            // lblLogsTitle
            // 
            this.lblLogsTitle.AutoSize = true;
            this.lblLogsTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblLogsTitle.ForeColor = System.Drawing.Color.White;
            this.lblLogsTitle.Location = new System.Drawing.Point(25, 657);
            this.lblLogsTitle.Name = "lblLogsTitle";
            this.lblLogsTitle.Size = new System.Drawing.Size(140, 25);
            this.lblLogsTitle.TabIndex = 13;
            this.lblLogsTitle.Text = "Dziennik zdarzeń";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(12)))), ((int)(((byte)(16)))), ((int)(((byte)(34)))));
            this.ClientSize = new System.Drawing.Size(1370, 890);
            this.Controls.Add(this.lblLogsTitle);
            this.Controls.Add(this.lblTableTitle);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelSuspicious);
            this.Controls.Add(this.panelDeauth);
            this.Controls.Add(this.panelPackets);
            this.Controls.Add(this.lblFileCaption);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblAppTitle);
            this.Controls.Add(this.rtbLogs);
            this.Controls.Add(this.dgvPackets);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btnExportReport);
            this.Controls.Add(this.btnLoadPcap);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1388, 937);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NetQin";
            ((System.ComponentModel.ISupportInitialize)(this.dgvPackets)).EndInit();
            this.panelPackets.ResumeLayout(false);
            this.panelPackets.PerformLayout();
            this.panelDeauth.ResumeLayout(false);
            this.panelDeauth.PerformLayout();
            this.panelSuspicious.ResumeLayout(false);
            this.panelSuspicious.PerformLayout();
            this.panelStatus.ResumeLayout(false);
            this.panelStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoadPcap;
        private System.Windows.Forms.Button btnExportReport;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.DataGridView dgvPackets;
        private System.Windows.Forms.RichTextBox rtbLogs;
        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblFileCaption;
        private System.Windows.Forms.Panel panelPackets;
        private System.Windows.Forms.Label lblPacketsCaption;
        private System.Windows.Forms.Label lblPacketCount;
        private System.Windows.Forms.Panel panelDeauth;
        private System.Windows.Forms.Label lblDeauthCaption;
        private System.Windows.Forms.Label lblDeauthCount;
        private System.Windows.Forms.Panel panelSuspicious;
        private System.Windows.Forms.Label lblSuspiciousCaption;
        private System.Windows.Forms.Label lblSuspiciousCount;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblStatusDescription;
        private System.Windows.Forms.Label lblStatusValue;
        private System.Windows.Forms.Label lblStatusTitle;
        private System.Windows.Forms.Label lblTableTitle;
        private System.Windows.Forms.Label lblLogsTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
    }
}