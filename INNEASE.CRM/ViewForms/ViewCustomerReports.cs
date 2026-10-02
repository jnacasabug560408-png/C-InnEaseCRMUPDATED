using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Admin: Customer Reports — hospitality KPI dashboard with operational metrics.
    /// </summary>
    public class ViewCustomerReports : UserControl
    {
        private ComboBox cmbDateRange;
        private Button btnExport;
        private ContextMenuStrip exportMenu;
        private Panel cardPerf;
        private Panel cardTiers;
        private DataGridView dgvRateBreakdown;
        private Label lblDateRangeDisplay;
        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
        private Panel pnlCustomDate;
        private Button btnApplyCustomDate;

        private string _currentDateRange = "This Month";

        public ViewCustomerReports(User currentUser) { Init(); }
        public ViewCustomerReports() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            BuildUI();
        }

        private void BuildUI()
        {
            // Main vertical layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(8),
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Header area
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // KPI cards
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));   // Charts area
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));   // Rate breakdown table
            mainPanel.RowCount = 4;
            this.Controls.Add(mainPanel);

            // ── Header with Export Button ────────────────────────────
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(headerPanel, 0, 0);

            var lblTitle = new Label
            {
                Text = "Customer Reports",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(4, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Hospitality KPI Dashboard",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(4, 48)
            };
            headerPanel.Controls.Add(lblSub);

            // Date Range Filter
            cmbDateRange = new ComboBox
            {
                Font = ThemeHelper.FontBody,
                Size = new Size(150, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(650, 10)
            };
            cmbDateRange.Items.AddRange(new[] { "This Month", "Last 30 Days", "Quarter-to-Date", "Custom Date Range" });
            cmbDateRange.SelectedIndex = 0;
            cmbDateRange.SelectedIndexChanged += CmbDateRange_SelectedIndexChanged;
            headerPanel.Controls.Add(cmbDateRange);

            lblDateRangeDisplay = new Label
            {
                Text = "Oct 1 - Oct 31, 2026",
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(650, 50)
            };
            headerPanel.Controls.Add(lblDateRangeDisplay);

            // Custom Date Range Panel (hidden by default)
            pnlCustomDate = new Panel
            {
                Size = new Size(280, 36),
                Location = new Point(810, 10),
                Visible = false
            };

            dtpStartDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(120, 36),
                Location = new Point(0, 0),
                Value = DateTime.Today.AddDays(-30)
            };
            pnlCustomDate.Controls.Add(dtpStartDate);

            dtpEndDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(120, 36),
                Location = new Point(125, 0),
                Value = DateTime.Today
            };
            pnlCustomDate.Controls.Add(dtpEndDate);

            btnApplyCustomDate = ThemeHelper.CreateSecondaryButton("Apply", 30, 36);
            btnApplyCustomDate.Location = new Point(250, 0);
            btnApplyCustomDate.Click += BtnApplyCustomDate_Click;
            pnlCustomDate.Controls.Add(btnApplyCustomDate);

            headerPanel.Controls.Add(pnlCustomDate);

            // Export Button
            btnExport = ThemeHelper.CreateSecondaryButton("Export", 100, 38);
            btnExport.Location = new Point(820, 10);
            btnExport.Click += BtnExport_Click;
            headerPanel.Controls.Add(btnExport);

            // Export Context Menu
            exportMenu = new ContextMenuStrip();
            exportMenu.Items.Add("Export to CSV").Click += ExportToCSV_Click;
            exportMenu.Items.Add("Export to PDF").Click += ExportToPDF_Click;

            // ── KPI Cards (Full-width grid layout) ───────────────────
            var kpiPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                Padding = new Padding(0, 4, 0, 0),
            };
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainPanel.Controls.Add(kpiPanel, 0, 1);

            RefreshKPICards(kpiPanel);



            // ── Tables Area (Split into two columns) ───────────────
            var tablesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            tablesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tablesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.Controls.Add(tablesPanel, 0, 2);

            // ── Room Performance Table ─────────────────────────────
            cardPerf = ThemeHelper.CreateCardPanel();
            cardPerf.Dock = DockStyle.Fill;
            cardPerf.Padding = new Padding(12);
            tablesPanel.Controls.Add(cardPerf, 0, 0);

            RefreshRoomPerformanceTable();

            // ── Guest Tiers & Segments Table ────────────────────────
            cardTiers = ThemeHelper.CreateCardPanel();
            cardTiers.Dock = DockStyle.Fill;
            cardTiers.Padding = new Padding(12);
            tablesPanel.Controls.Add(cardTiers, 1, 0);

            RefreshGuestTiersTable();

            // ── Rate Breakdown Table ───────────────────────────────────
            var cardBreakdown = ThemeHelper.CreateCardPanel();
            cardBreakdown.Dock = DockStyle.Fill;
            cardBreakdown.Padding = new Padding(12);
            mainPanel.Controls.Add(cardBreakdown, 0, 3);

            var lblBreakdownHeader = ThemeHelper.CreateSectionHeader("Performance Rate Breakdown");
            lblBreakdownHeader.Dock = DockStyle.Top;
            cardBreakdown.Controls.Add(lblBreakdownHeader);

            dgvRateBreakdown = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridViewWithAccent(dgvRateBreakdown);
            dgvRateBreakdown.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            cardBreakdown.Controls.Add(dgvRateBreakdown);

            RefreshRateBreakdown();
        }

        private void RefreshKPICards(TableLayoutPanel kpiPanel)
        {
            kpiPanel.Controls.Clear();

            var filteredReservations = GetFilteredReservations().ToList();
            
            // 1. Calculate Period Context
            DateTime startDate = _currentDateRange == "Custom Date Range" ? dtpStartDate.Value : 
                (filteredReservations.Any() ? filteredReservations.Min(r => r.CheckInDate) : DateTime.Today);
            DateTime endDate = _currentDateRange == "Custom Date Range" ? dtpEndDate.Value : DateTime.Today;
            
            int daysInPeriod = Math.Max(1, (endDate - startDate).Days + 1);
            int totalAvailableRoomNights = 200 * daysInPeriod;

            // 2. Base Aggregations
            decimal totalRevenue = filteredReservations.Sum(r => r.TotalAmount);
            int totalBookings = filteredReservations.Count;
            int totalOccupiedRoomNights = filteredReservations.Sum(r => Math.Max(1, (r.CheckOutDate - r.CheckInDate).Days));

            // 3. KPI Formulas
            decimal occupancyRate = totalAvailableRoomNights > 0 
                ? ((decimal)totalOccupiedRoomNights / totalAvailableRoomNights) * 100 
                : 0;

            decimal adr = totalOccupiedRoomNights > 0 
                ? totalRevenue / totalOccupiedRoomNights 
                : 0;

            decimal revpar = totalAvailableRoomNights > 0 
                ? totalRevenue / totalAvailableRoomNights 
                : 0;

            decimal alos = totalBookings > 0 
                ? (decimal)totalOccupiedRoomNights / totalBookings 
                : 0;

            // 4. Create UI Cards
            var cardOcc = ThemeHelper.CreateKpiCard("Occupancy Rate", occupancyRate.ToString("F1") + "%", ThemeHelper.Success, 200, 85);
            var cardAdr = ThemeHelper.CreateKpiCard("ADR (Avg Daily Rate)", "₱" + adr.ToString("N0"), ThemeHelper.Primary, 200, 85);
            var cardRevPar = ThemeHelper.CreateKpiCard("RevPAR", "₱" + revpar.ToString("N0"), ThemeHelper.Info, 200, 85);
            var cardAlos = ThemeHelper.CreateKpiCard("Avg. Length of Stay", alos.ToString("F1") + " nights", ThemeHelper.Accent, 200, 85);

            // 5. Attach Drill-Down Events
            AttachDrillDownEvent(cardOcc, "Occupancy");
            AttachDrillDownEvent(cardAdr, "ADR");
            AttachDrillDownEvent(cardRevPar, "RevPAR");
            AttachDrillDownEvent(cardAlos, "ALOS");

            kpiPanel.Controls.Add(cardOcc, 0, 0);
            kpiPanel.Controls.Add(cardAdr, 1, 0);
            kpiPanel.Controls.Add(cardRevPar, 2, 0);
            kpiPanel.Controls.Add(cardAlos, 3, 0);
        }

        private void AttachDrillDownEvent(Control card, string kpiType)
        {
            card.Cursor = Cursors.Hand;
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 245, 248);
            card.MouseLeave += (s, e) => card.BackColor = Color.White;

            EventHandler clickHandler = (s, e) =>
            {
                if (FormMain.Instance == null) return;
                switch (kpiType)
                {
                    case "Occupancy":
                    case "ADR":
                    case "RevPAR":
                        FormMain.Instance.NavigateTo(typeof(ViewReservations));
                        break;
                    case "ALOS":
                        FormMain.Instance.NavigateTo(typeof(ViewManageGuests));
                        break;
                }
            };

            card.Click += clickHandler;
            foreach (Control child in card.Controls)
            {
                child.Cursor = Cursors.Hand;
                child.Click += clickHandler;
                child.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 245, 248);
                child.MouseLeave += (s, e) => card.BackColor = Color.White;
            }
        }

        private Chart BuildRoomPerformanceChart()
        {
            Chart barChart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            
            ChartArea area = new ChartArea("MainArea") { BackColor = Color.White };
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            barChart.ChartAreas.Add(area);

            barChart.Legends.Add(new Legend("Legend") { Docking = Docking.Top, Alignment = StringAlignment.Center });

            var revSeries = new Series("Revenue (₱)") { ChartType = SeriesChartType.Column, Color = ColorTranslator.FromHtml("#2D0B2B") };
            var nightsSeries = new Series("Nights Sold") { ChartType = SeriesChartType.Column, Color = ColorTranslator.FromHtml("#E91E63") };

            var roomData = GetFilteredReservations()
                .GroupBy(r => r.RoomType)
                .Select(g => new { 
                    Type = g.Key, 
                    Rev = g.Sum(r => r.TotalAmount), 
                    Nights = g.Sum(r => (r.CheckOutDate - r.CheckInDate).Days) 
                });

            foreach (var item in roomData)
            {
                revSeries.Points.AddXY(item.Type, (double)item.Rev);
                nightsSeries.Points.AddY(item.Nights);
            }

            barChart.Series.Add(revSeries);
            barChart.Series.Add(nightsSeries);
            return barChart;
        }

        private Chart BuildGuestDonutChart()
        {
            Chart donutChart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea area = new ChartArea("MainArea") { BackColor = Color.White };
            donutChart.ChartAreas.Add(area);

            Color[] brandPalette = { ColorTranslator.FromHtml("#2D0B2B"), ColorTranslator.FromHtml("#E91E63"), ColorTranslator.FromHtml("#F48FB1") };

            var series = new Series("Revenue")
            {
                ChartType = SeriesChartType.Doughnut,
                CustomProperties = "DoughnutRadius=50, PieLabelStyle=Outside"
            };

            var tierRevenue = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = @"
                        SELECT 
                            CASE 
                                WHEN g.IsVIP = 1 THEN 'VIP'
                                WHEN g.LoyaltyPoints > 500 THEN 'Regular'
                                ELSE 'First-Timer'
                            END AS Tier,
                            SUM(r.TotalAmount) AS Revenue
                        FROM Reservations r
                        JOIN Guests g ON r.GuestId = g.Id
                        GROUP BY 
                            CASE 
                                WHEN g.IsVIP = 1 THEN 'VIP'
                                WHEN g.LoyaltyPoints > 500 THEN 'Regular'
                                ELSE 'First-Timer'
                            END";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tierRevenue.Add(new { Tier = reader.GetString(0), Revenue = reader.GetDecimal(1) });
                        }
                    }
                }
            }
            catch { }

            decimal totalRev = tierRevenue.Sum(t => (decimal)t.Revenue);
            
            int colorIndex = 0;
            foreach (var item in tierRevenue)
            {
                int pointIndex = series.Points.AddXY(item.Tier, (double)item.Revenue);
                series.Points[pointIndex].Label = $"{item.Tier}\n{(totalRev > 0 ? ((decimal)item.Revenue/totalRev) : 0):P1}";
                series.Points[pointIndex].Color = brandPalette[colorIndex % brandPalette.Length];
                colorIndex++;
            }

            donutChart.Series.Add(series);

            Title centerTitle = new Title($"Total\n₱{totalRev:N0}")
            {
                Docking = Docking.Bottom,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#2D0B2B")
            };
            donutChart.Titles.Add(centerTitle);

            return donutChart;
        }

        private void RefreshRoomPerformanceTable()
        {
            if (cardPerf == null) return;
            cardPerf.Controls.Clear();
            cardPerf.Cursor = Cursors.Hand;
            cardPerf.Click -= CardPerf_Click; // prevent duplicates
            cardPerf.Click += CardPerf_Click;

            var lblPerfHeader = ThemeHelper.CreateSectionHeader("Room Performance Details");
            lblPerfHeader.Dock = DockStyle.Top;
            lblPerfHeader.Cursor = Cursors.Hand;
            lblPerfHeader.Click += CardPerf_Click;
            cardPerf.Controls.Add(lblPerfHeader);

            var chart = BuildRoomPerformanceChart();
            chart.Cursor = Cursors.Hand;
            chart.Click += CardPerf_Click;
            cardPerf.Controls.Add(chart);
        }

        private void CardPerf_Click(object sender, EventArgs e)
        {
            if (FormMain.Instance != null)
                FormMain.Instance.NavigateTo(typeof(ViewReservations));
        }

        private void RefreshGuestTiersTable()
        {
            if (cardTiers == null) return;
            cardTiers.Controls.Clear();
            cardTiers.Cursor = Cursors.Hand;
            cardTiers.Click -= CardTiers_Click;
            cardTiers.Click += CardTiers_Click;

            var lblTiersHeader = ThemeHelper.CreateSectionHeader("Guest Tiers & Segments");
            lblTiersHeader.Dock = DockStyle.Top;
            lblTiersHeader.Cursor = Cursors.Hand;
            lblTiersHeader.Click += CardTiers_Click;
            cardTiers.Controls.Add(lblTiersHeader);
            
            var chart = BuildGuestDonutChart();
            chart.Cursor = Cursors.Hand;
            chart.Click += CardTiers_Click;
            cardTiers.Controls.Add(chart);
        }

        private void CardTiers_Click(object sender, EventArgs e)
        {
            if (FormMain.Instance != null)
                FormMain.Instance.NavigateTo(typeof(ViewManageGuests));
        }

        private void RefreshRateBreakdown()
        {
            if (dgvRateBreakdown == null) return;

            // Room type catalogue: daily rate, weekly multiplier, monthly multiplier
            var catalogue = new[]
            {
                new { Type = "Standard",     DailyRate = 3000m },
                new { Type = "Deluxe",       DailyRate = 5000m },
                new { Type = "Suite",        DailyRate = 9000m },
                new { Type = "Presidential", DailyRate = 15000m },
            };

            var reservations = GetFilteredReservations().ToList();

            var rows = catalogue.Select(c =>
            {
                decimal totalRevenue = reservations
                    .Where(r => r.RoomType == c.Type)
                    .Sum(r => r.TotalAmount);

                return new
                {
                    RoomType    = c.Type,
                    DailyRate   = c.DailyRate,
                    WeeklyRate  = c.DailyRate * 7,
                    MonthlyRate = c.DailyRate * 30,
                    TotalRevenue = totalRevenue
                };
            }).ToList();

            dgvRateBreakdown.DataSource = rows;

            if (dgvRateBreakdown.Columns.Count == 0) return;

            // Column headers
            if (dgvRateBreakdown.Columns.Contains("RoomType"))    dgvRateBreakdown.Columns["RoomType"].HeaderText    = "Room Type";
            if (dgvRateBreakdown.Columns.Contains("DailyRate"))   dgvRateBreakdown.Columns["DailyRate"].HeaderText   = "Daily Rate (₱)";
            if (dgvRateBreakdown.Columns.Contains("WeeklyRate"))  dgvRateBreakdown.Columns["WeeklyRate"].HeaderText  = "Weekly Rate (₱)";
            if (dgvRateBreakdown.Columns.Contains("MonthlyRate")) dgvRateBreakdown.Columns["MonthlyRate"].HeaderText = "Monthly Rate (₱)";
            if (dgvRateBreakdown.Columns.Contains("TotalRevenue"))dgvRateBreakdown.Columns["TotalRevenue"].HeaderText = "Total Revenue (₱)";

            // Formatting & alignment
            string[] currencyCols = { "DailyRate", "WeeklyRate", "MonthlyRate", "TotalRevenue" };
            foreach (var col in currencyCols)
            {
                if (dgvRateBreakdown.Columns.Contains(col))
                {
                    dgvRateBreakdown.Columns[col].DefaultCellStyle.Format = "#,##0.00";
                    dgvRateBreakdown.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Accent top row
            if (dgvRateBreakdown.Rows.Count > 0)
                dgvRateBreakdown.Rows[dgvRateBreakdown.Rows.Count - 1].DefaultCellStyle.ForeColor =
                    ColorTranslator.FromHtml("#E91E63");
        }

        private System.Collections.Generic.IEnumerable<Reservation> GetFilteredReservations()
        {
            var reservations = new List<Reservation>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = "SELECT RoomType, CheckInDate, CheckOutDate, TotalAmount FROM Reservations WHERE 1=1";
                    
                    if (_currentDateRange == "This Month")
                        sql += " AND MONTH(CheckInDate) = MONTH(GETDATE()) AND YEAR(CheckInDate) = YEAR(GETDATE())";
                    else if (_currentDateRange == "Last 30 Days")
                        sql += " AND CheckInDate >= DATEADD(day, -30, GETDATE())";
                    else if (_currentDateRange == "Quarter-to-Date")
                        sql += " AND CheckInDate >= DATEADD(qq, DATEDIFF(qq, 0, GETDATE()), 0)";
                    else if (_currentDateRange == "Custom Date Range")
                        sql += " AND CheckInDate >= @start AND CheckInDate <= @end";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (_currentDateRange == "Custom Date Range")
                        {
                            cmd.Parameters.AddWithValue("@start", dtpStartDate.Value.Date);
                            cmd.Parameters.AddWithValue("@end", dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1));
                        }
                        
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                reservations.Add(new Reservation
                                {
                                    RoomType     = r.IsDBNull(0) ? "" : r.GetString(0),
                                    CheckInDate  = r.GetDateTime(1),
                                    CheckOutDate = r.GetDateTime(2),
                                    TotalAmount  = r.GetDecimal(3)
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return reservations;
        }

        private void CmbDateRange_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentDateRange = cmbDateRange.SelectedItem.ToString();

            // Show/hide custom date picker
            pnlCustomDate.Visible = (_currentDateRange == "Custom Date Range");

            // Update date range display
            switch (_currentDateRange)
            {
                case "This Month":
                    lblDateRangeDisplay.Text = $"Oct 1 - Oct 31, {DateTime.Today.Year}";
                    break;
                case "Last 30 Days":
                    lblDateRangeDisplay.Text = $"{DateTime.Today.AddDays(-30):MMM dd} - {DateTime.Today:MMM dd, yyyy}";
                    break;
                case "Quarter-to-Date":
                    var currentQuarter = (DateTime.Today.Month - 1) / 3 + 1;
                    var quarterStart = new DateTime(DateTime.Today.Year, (currentQuarter - 1) * 3 + 1, 1);
                    lblDateRangeDisplay.Text = $"{quarterStart:MMM dd} - {DateTime.Today:MMM dd, yyyy}";
                    break;
                case "Custom Date Range":
                    lblDateRangeDisplay.Text = "Select date range and click Apply";
                    break;
            }

            // Refresh all data (except for custom date range which requires Apply button)
            if (_currentDateRange != "Custom Date Range")
            {
                var mainPanel = (TableLayoutPanel)this.Controls[0];
                var kpiPanel = (TableLayoutPanel)mainPanel.GetControlFromPosition(0, 1);
                RefreshKPICards(kpiPanel);
                RefreshRoomPerformanceTable();
                RefreshGuestTiersTable();
                RefreshRateBreakdown();
            }
        }

        private void BtnApplyCustomDate_Click(object sender, EventArgs e)
        {
            if (dtpStartDate.Value > dtpEndDate.Value)
            {
                MessageBox.Show("Start date must be before end date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblDateRangeDisplay.Text = $"{dtpStartDate.Value:MMM dd, yyyy} - {dtpEndDate.Value:MMM dd, yyyy}";

            var mainPanel = (TableLayoutPanel)this.Controls[0];
            var kpiPanel = (TableLayoutPanel)mainPanel.GetControlFromPosition(0, 1);
            RefreshKPICards(kpiPanel);
            RefreshRoomPerformanceTable();
            RefreshGuestTiersTable();
            RefreshRateBreakdown();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // Text search was bound to grids which are now replaced by charts. 
            // In a real scenario, this could filter chart series or we can hide the search bar.
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            exportMenu.Show(btnExport, new Point(0, btnExport.Height));
        }

        private void ExportToCSV_Click(object sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"CustomerReports_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName))
                    {
                        // Export logic will need to be updated to support charts or raw data.
                        MessageBox.Show("CSV Export needs to be re-wired to raw data lists instead of the UI Grid.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToPDF_Click(object sender, EventArgs e)
        {
            MessageBox.Show("PDF export feature requires a PDF library (e.g., iTextSharp, PdfSharp). This is a placeholder for the implementation.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
