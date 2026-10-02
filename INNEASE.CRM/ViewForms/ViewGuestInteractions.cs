using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Staff: Manage Guest Interactions — log check-ins, check-outs, and notes.
    /// </summary>
    public class ViewGuestInteractions : UserControl
    {
        private DataGridView dgvInteractions;
        private ComboBox cmbGuest, cmbType;
        private TextBox txtDetails;
        private User _currentUser;

        public ViewGuestInteractions(User currentUser) { _currentUser = currentUser; Init(); }
        public ViewGuestInteractions() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            // Main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(8),
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));  // Header
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content
            this.Controls.Add(mainPanel);

            // ── Header ───────────────────────────────────────────────
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(headerPanel, 0, 0);

            var lblTitle = new Label
            {
                Text = "Guest Interactions",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(4, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Log check-ins, check-outs, calls, and notes",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(4, 48)
            };
            headerPanel.Controls.Add(lblSub);

            // ── Content Area (Split: Form on left, Grid on right) ───────
            var contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));  // Form width
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Grid fills rest
            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Dock = DockStyle.Fill;
            cardForm.Padding = new Padding(12);

            var lblForm = ThemeHelper.CreateSectionHeader("Log Interaction");
            lblForm.Dock = DockStyle.Top;
            cardForm.Controls.Add(lblForm);

            var formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // Labels
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Fields
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardForm.Controls.Add(formPanel);

            // Guest
            var lblGuest = new Label { Text = "Guest", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            cmbGuest = new ComboBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT FullName FROM Guests ORDER BY FullName", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) cmbGuest.Items.Add(reader.GetString(0));
                }
            }
            catch { }
            if (cmbGuest.Items.Count > 0) cmbGuest.SelectedIndex = 0;
            formPanel.Controls.Add(lblGuest, 0, 0);
            formPanel.Controls.Add(cmbGuest, 1, 0);

            // Type
            var lblType = new Label { Text = "Type", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            cmbType = new ComboBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "CheckIn", "CheckOut", "Note", "Call", "Email" });
            cmbType.SelectedIndex = 0;
            formPanel.Controls.Add(lblType, 0, 1);
            formPanel.Controls.Add(cmbType, 1, 1);

            // Details
            var lblDetails = new Label { Text = "Details", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            txtDetails = new TextBox
            {
                Font = ThemeHelper.FontBody,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            formPanel.Controls.Add(lblDetails, 0, 2);
            formPanel.Controls.Add(txtDetails, 1, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            formPanel.Controls.Add(buttonPanel, 0, 3);
            formPanel.SetColumnSpan(buttonPanel, 2);

            var btnSave = ThemeHelper.CreatePrimaryButton("Log Entry", 120, 40);
            btnSave.Margin = new Padding(0, 0, 8, 0);
            btnSave.Click += BtnSave_Click;
            buttonPanel.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 40);
            btnClear.Click += (s, e) => ClearForm();
            buttonPanel.Controls.Add(btnClear);

            contentPanel.Controls.Add(cardForm, 0, 0);

            // ── Grid ─────────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(12);

            dgvInteractions = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvInteractions);
            cardGrid.Controls.Add(dgvInteractions);

            var lblGrid = ThemeHelper.CreateSectionHeader("Interaction Log");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            contentPanel.Controls.Add(cardGrid, 1, 0);
        }

        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, InteractionDate, InteractionType, GuestName, Notes, StaffMember FROM GuestInteractions ORDER BY InteractionDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Id      = reader.GetInt32(0),
                            Date    = reader.GetDateTime(1).ToString("MMM dd, HH:mm"),
                            Type    = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Guest   = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Details = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Staff   = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvInteractions.DataSource = null;
            dgvInteractions.DataSource = rows;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbGuest.SelectedItem == null)
            {
                MessageBox.Show("Please select a guest.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string guestName = cmbGuest.SelectedItem.ToString();
            int guestId = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT Id FROM Guests WHERE FullName=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", guestName);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) guestId = (int)obj;
                    }

                    using (var cmd = new SqlCommand("INSERT INTO GuestInteractions (GuestId, GuestName, InteractionType, Notes, StaffMember) VALUES (@gId, @gn, @t, @d, @s)", conn))
                    {
                        cmd.Parameters.AddWithValue("@gId", guestId);
                        cmd.Parameters.AddWithValue("@gn",  guestName);
                        cmd.Parameters.AddWithValue("@t",   cmbType.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@d",   txtDetails.Text.Trim());
                        cmd.Parameters.AddWithValue("@s",   _currentUser?.FullName ?? "Staff");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Interaction logged!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearForm()
        {
            if (cmbGuest.Items.Count > 0) cmbGuest.SelectedIndex = 0;
            cmbType.SelectedIndex = 0;
            txtDetails.Text = "";
        }
    }
}
