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
    /// Manager/Staff: Request Supplies - submit and track supply requests.
    /// </summary>
    public class ViewRequestSupplies : UserControl
    {
        private DataGridView dgvRequests;
        private ComboBox cmbSupply, cmbPriority, cmbDepartment;
        private NumericUpDown nudQuantity;
        private TextBox txtReason;
        private User _currentUser;

        public ViewRequestSupplies(User currentUser)
        {
            _currentUser = currentUser;
            Init();
        }

        public ViewRequestSupplies() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.AutoScroll = true;
            this.Resize += (s, e) => UpdateLayout();
            BuildUI();
            RefreshGrid();
        }

        private void UpdateLayout()
        {
            // Update KPI panel width
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is FlowLayoutPanel kpiPanel && kpiPanel.Location.Y == 70)
                {
                    kpiPanel.Width = this.Width - 40;
                }
                // Update grid card width
                if (ctrl is Panel cardGrid && cardGrid.Location.X == 420)
                {
                    cardGrid.Width = this.Width - 460;
                    cardGrid.Height = this.Height - 215;
                }
            }
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Request Supplies",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Submit and track supply requests",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 35)
            };
            this.Controls.Add(lblSubtitle);

            // ── Summary Cards Row ────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(this.Width - 40, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            int totalRequests = 0, pendingRequests = 0, approvedRequests = 0, myRequests = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SupplyRequests", conn))
                        totalRequests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SupplyRequests WHERE Status='Pending'", conn))
                        pendingRequests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SupplyRequests WHERE Status='Approved'", conn))
                        approvedRequests = (int)cmd.ExecuteScalar();
                    if (_currentUser != null)
                    {
                        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SupplyRequests WHERE RequestedBy=@u", conn))
                        {
                            cmd.Parameters.AddWithValue("@u", _currentUser.FullName);
                            myRequests = (int)cmd.ExecuteScalar();
                        }
                    }
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Requests", totalRequests.ToString(), ThemeHelper.Primary, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Pending", pendingRequests.ToString(), ThemeHelper.Warning, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Approved", approvedRequests.ToString(), ThemeHelper.Success, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("My Requests", myRequests.ToString(), ThemeHelper.Info, 220, 100));

            this.Controls.Add(kpiPanel);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 195);
            cardForm.Size = new Size(400, 280);
            cardForm.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("New Request");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;

            // Supply
            var lblSupply = new Label { Text = "Supply Item", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblSupply);
            cmbSupply = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(250, 28), Location = new Point(130, y), DropDownStyle = ComboBoxStyle.DropDownList };
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Name FROM Supplies WHERE IsActive=1 ORDER BY Name", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) cmbSupply.Items.Add(r.GetString(0));
                }
            }
            catch { }
            if (cmbSupply.Items.Count > 0) cmbSupply.SelectedIndex = 0;
            cardForm.Controls.Add(cmbSupply);
            y += 34;

            // Quantity
            var lblQuantity = new Label { Text = "Quantity", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblQuantity);
            nudQuantity = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 1000, Minimum = 1, DecimalPlaces = 0, Value = 10 };
            cardForm.Controls.Add(nudQuantity);
            y += 34;

            // Department
            var lblDept = new Label { Text = "Department", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblDept);
            cmbDepartment = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(250, 28), Location = new Point(130, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDepartment.Items.AddRange(new[] { "Housekeeping", "Food & Beverage", "Front Desk", "Maintenance", "Kitchen", "Laundry" });
            if (cmbDepartment.Items.Count > 0) cmbDepartment.SelectedIndex = 0;
            cardForm.Controls.Add(cmbDepartment);
            y += 34;

            // Priority
            var lblPriority = new Label { Text = "Priority", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblPriority);
            cmbPriority = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPriority.Items.AddRange(new[] { "Low", "Normal", "High", "Urgent" });
            if (cmbPriority.Items.Count > 0) cmbPriority.SelectedIndex = 1;
            cardForm.Controls.Add(cmbPriority);
            y += 34;

            // Reason
            var lblReason = new Label { Text = "Reason", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblReason);
            txtReason = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(250, 28), Location = new Point(130, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtReason);
            y += 34;

            // Buttons
            var btnSubmit = ThemeHelper.CreatePrimaryButton("Submit Request", 140, 38);
            btnSubmit.Location = new Point(20, y);
            btnSubmit.Click += BtnSubmit_Click;
            cardForm.Controls.Add(btnSubmit);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(170, y);
            btnClear.Click += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            this.Controls.Add(cardForm);

            // ── Grid Card ────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(420, 195);
            cardGrid.Size = new Size(this.Width - 460, 280);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvRequests = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvRequests);
            cardGrid.Controls.Add(dgvRequests);

            var lblGrid = ThemeHelper.CreateSectionHeader("Request History");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            this.Controls.Add(cardGrid);
        }

        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = "SELECT Id, SupplyName, RequestedQuantity, Department, Priority, Status, RequestDate, ApprovedBy, ApprovedDate FROM SupplyRequests";
                    if (_currentUser != null && _currentUser.Role != UserRole.SuperAdmin && _currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.Manager)
                    {
                         sql += " WHERE RequestedBy=@u";
                    }
                    sql += " ORDER BY RequestDate DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (_currentUser != null && _currentUser.Role != UserRole.SuperAdmin && _currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.Manager)
                            cmd.Parameters.AddWithValue("@u", _currentUser.FullName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rows.Add(new
                                {
                                    Id           = reader.GetInt32(0),
                                    Supply       = reader.GetString(1),
                                    Quantity     = reader.GetInt32(2),
                                    Department   = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Priority     = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Status       = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    RequestDate  = reader.GetDateTime(6).ToString("MMM dd, yyyy"),
                                    ApprovedBy   = reader.IsDBNull(7) ? "N/A" : reader.GetString(7),
                                    ApprovedDate = reader.IsDBNull(8) ? "N/A" : reader.GetDateTime(8).ToString("MMM dd, yyyy")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvRequests.DataSource = null;
            dgvRequests.DataSource = rows;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (cmbSupply.SelectedItem == null)
            {
                MessageBox.Show("Please select a supply item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentUser == null)
            {
                MessageBox.Show("User information not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    int supplyId = 0;
                    using (var cmd = new SqlCommand("SELECT Id FROM Supplies WHERE Name=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", cmbSupply.SelectedItem.ToString());
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) supplyId = (int)obj;
                    }

                    using (var cmd = new SqlCommand(@"INSERT INTO SupplyRequests 
                        (SupplyId, SupplyName, RequestedQuantity, RequestedBy, Department, Priority, Reason, Status)
                        VALUES (@sid, @sn, @rq, @rb, @dep, @pri, @rea, 'Pending')", conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", supplyId);
                        cmd.Parameters.AddWithValue("@sn", cmbSupply.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@rq", (int)nudQuantity.Value);
                        cmd.Parameters.AddWithValue("@rb", _currentUser.FullName);
                        cmd.Parameters.AddWithValue("@dep", cmbDepartment.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@pri", cmbPriority.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@rea", txtReason.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }
            ClearForm();
            RefreshGrid();
            MessageBox.Show("Supply request submitted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearForm()
        {
            if (cmbSupply.Items.Count > 0) cmbSupply.SelectedIndex = 0;
            nudQuantity.Value = 10;
            if (cmbDepartment.Items.Count > 0) cmbDepartment.SelectedIndex = 0;
            if (cmbPriority.Items.Count > 0) cmbPriority.SelectedIndex = 1;
            txtReason.Text = "";
        }
    }
}
