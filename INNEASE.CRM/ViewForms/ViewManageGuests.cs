using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Admin: Guest directory with contact info, profile views, and CRUD operations.
    /// </summary>
    public class ViewManageGuests : UserControl
    {
        private DataGridView dgvGuests;
        private int? _editingId = null;
        private string _loyaltyFilter = "All tiers";

        public ViewManageGuests(User currentUser) { Init(); }
        public ViewManageGuests() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            // Main layout using TableLayoutPanel for edge-to-edge responsive design
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(16),
                Margin = new Padding(0)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // KPI cards
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Search/filter bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content area
            this.Controls.Add(mainLayout);

            // ── Header ───────────────────────────────────────────────
            var headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            mainLayout.Controls.Add(headerPanel, 0, 0);

            var lblTitle = new Label
            {
                Text = "Guests",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Manage guest profiles, preferences and loyalty",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 45)
            };
            headerPanel.Controls.Add(lblSubtitle);

            // ── Summary Cards Row ────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0)
            };
            mainLayout.Controls.Add(kpiPanel, 0, 1);

            int totalGuests = 0, returningGuests = 0, vipGuests = 0, newThisMonth = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests", conn))
                        totalGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests WHERE LoyaltyPoints > 100", conn))
                        returningGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests WHERE IsVIP=1", conn))
                        vipGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests WHERE MONTH(DateRegistered)=@m AND YEAR(DateRegistered)=@y", conn))
                    {
                        cmd.Parameters.AddWithValue("@m", DateTime.Today.Month);
                        cmd.Parameters.AddWithValue("@y", DateTime.Today.Year);
                        newThisMonth = (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total guests", totalGuests.ToString(), ThemeHelper.Primary, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Returning guests", returningGuests.ToString(), ThemeHelper.Info, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("VIP guests", vipGuests.ToString(), ThemeHelper.Accent, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("New this month", newThisMonth.ToString(), ThemeHelper.Success, 220, 100));

            // ── Search and Filter Bar ─────────────────────────────────
            var searchFilterPanel = new Panel { Dock = DockStyle.Fill, Height = 50, Margin = new Padding(0) };
            mainLayout.Controls.Add(searchFilterPanel, 0, 2);

            var txtSearch = ThemeHelper.CreateSearchBar(400, "Search guest by name, email or phone");
            txtSearch.Location = new Point(0, 5);
            searchFilterPanel.Controls.Add(txtSearch);

            var cmbLoyaltyFilter = ThemeHelper.CreateFilterComboBox(new[] { "All tiers", "First-Timer", "Regular", "VIP" }, 150);
            cmbLoyaltyFilter.Location = new Point(420, 5);
            cmbLoyaltyFilter.SelectedIndexChanged += (s, e) =>
            {
                _loyaltyFilter = cmbLoyaltyFilter.SelectedItem?.ToString() ?? "All tiers";
                RefreshGrid();
            };
            searchFilterPanel.Controls.Add(cmbLoyaltyFilter);

            // ── Content Area (Split: Form on left, Grid on right) ─────
            var contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0)
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));  // Form width
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Grid fills rest
            mainLayout.Controls.Add(contentPanel, 0, 3);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Dock = DockStyle.Fill;
            cardForm.Padding = new Padding(20);

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Guest Registration");
            lblFormTitle.Dock = DockStyle.Top;
            cardForm.Controls.Add(lblFormTitle);

            var formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0)
            };
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // Labels
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Fields
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardForm.Controls.Add(formPanel);

            // Full Name
            var lblName = new Label { Text = "Full Name", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var txtFullName = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblName, 0, 0);
            formPanel.Controls.Add(txtFullName, 1, 0);

            // Email
            var lblEmail = new Label { Text = "Email", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var txtEmail = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblEmail, 0, 1);
            formPanel.Controls.Add(txtEmail, 1, 1);

            // Phone
            var lblPhone = new Label { Text = "Phone", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var txtPhone = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblPhone, 0, 2);
            formPanel.Controls.Add(txtPhone, 1, 2);

            // Guest Tier
            var lblTier = new Label { Text = "Guest Tier", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var cmbLoyalty = new ComboBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLoyalty.Items.AddRange(new[] { "First-Timer", "Regular", "VIP" });
            if (cmbLoyalty.Items.Count > 0) cmbLoyalty.SelectedIndex = 0;
            formPanel.Controls.Add(lblTier, 0, 3);
            formPanel.Controls.Add(cmbLoyalty, 1, 3);

            // Status
            var lblStatus = new Label { Text = "Status", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var cmbStatus = new ComboBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new[] { "Active", "Inactive" });
            if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
            formPanel.Controls.Add(lblStatus, 0, 4);
            formPanel.Controls.Add(cmbStatus, 1, 4);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            formPanel.Controls.Add(buttonPanel, 0, 5);
            formPanel.SetColumnSpan(buttonPanel, 2);

            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 100, 38);
            btnSave.Margin = new Padding(0, 0, 8, 0);
            buttonPanel.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            buttonPanel.Controls.Add(btnClear);

            contentPanel.Controls.Add(cardForm, 0, 0);

            // ── Table Card ───────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(20);

            dgvGuests = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvGuests);
            dgvGuests.CellClick += DgvGuests_CellClick;
            cardGrid.Controls.Add(dgvGuests);

            var lblGridTitle = ThemeHelper.CreateSectionHeader("Guest Directory");
            lblGridTitle.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGridTitle);

            contentPanel.Controls.Add(cardGrid, 1, 0);

            // ── Form Events ──────────────────────────────────────────
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("Full Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string[] nameParts = txtFullName.Text.Trim().Split(' ');
                string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                bool isVip = cmbLoyalty.SelectedItem.ToString() == "VIP";
                int loyaltyPts = isVip ? 5000 : (cmbLoyalty.SelectedItem.ToString() == "Regular" ? 1000 : 100);

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("INSERT INTO Guests (FirstName,LastName,FullName,Email,Phone,Address,Nationality,IsVIP,LoyaltyPoints,Notes,DateRegistered) VALUES (@fn,@ln,@full,@e,@ph,'','',@vip,@lp,'',GETDATE())", conn))
                    {
                        cmd.Parameters.AddWithValue("@fn",   firstName);
                        cmd.Parameters.AddWithValue("@ln",   lastName);
                        cmd.Parameters.AddWithValue("@full", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@e",    txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@ph",   txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@vip",  isVip);
                        cmd.Parameters.AddWithValue("@lp",   loyaltyPts);
                        cmd.ExecuteNonQuery();
                    }
                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Create", $"Added guest: {txtFullName.Text.Trim()}", "Manage Guests");
                    RefreshGrid();
                    MessageBox.Show("Guest saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnClear.PerformClick();
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
            };

            btnClear.Click += (s, e) =>
            {
                txtFullName.Text = txtEmail.Text = txtPhone.Text = "";
                if (cmbLoyalty.Items.Count > 0) cmbLoyalty.SelectedIndex = 0;
                if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
            };
        }

        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = @"SELECT g.Id, ISNULL(g.FullName, ISNULL(g.FirstName,'')+' '+ISNULL(g.LastName,'')) AS GuestName,
                        g.Email, g.Phone, g.IsVIP, g.LoyaltyPoints,
                        (SELECT COUNT(*) FROM Reservations r WHERE r.GuestId=g.Id) AS Stays,
                        (SELECT TOP 1 CONVERT(VARCHAR,r2.CheckInDate,107) FROM Reservations r2 WHERE r2.GuestId=g.Id ORDER BY r2.CheckInDate DESC) AS LastStay
                        FROM Guests g";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool isVip = reader.GetBoolean(4);
                            int pts = reader.GetInt32(5);
                            string loyalty = isVip ? "VIP" : (pts > 500 ? "Regular" : "First-Timer");

                            if (_loyaltyFilter != "All tiers" && loyalty != _loyaltyFilter)
                                continue;

                            rows.Add(new
                            {
                                Id      = reader.GetInt32(0),
                                Guest   = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Email   = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Phone   = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Stays   = reader.GetInt32(6),
                                LastStay= reader.IsDBNull(7) ? "N/A" : reader.GetString(7),
                                Loyalty = loyalty
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading guests: " + ex.Message); }

            dgvGuests.DataSource = null;
            dgvGuests.DataSource = rows;
        }

        private void DgvGuests_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvGuests.Rows[e.RowIndex].Cells["Id"].Value;
            _editingId = id;
        }
    }
}
