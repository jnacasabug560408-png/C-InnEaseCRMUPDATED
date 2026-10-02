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
    /// Super Admin: Manage Subscriptions — create and manage subscription plans.
    /// </summary>
    public class ViewManageSubscriptions : UserControl
    {
        private DataGridView dgvSubscriptions;
        private NumericUpDown nudMonthlyPricePesos, nudYearlyPricePesos;
        private CheckBox chkIsActive;
        private int? _editingId = null;

        public ViewManageSubscriptions(User currentUser) { Init(); }
        public ViewManageSubscriptions() { Init(); }

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
                    cardGrid.Height = this.Height - 250;
                }
            }
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Manage Subscriptions",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Create and manage subscription plans for tenants",
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

            int totalPlans = 0, activePlans = 0, totalSubscribers = 0;
            decimal avgPrice = 0;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Subscriptions", conn))
                        totalPlans = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Subscriptions WHERE IsActive=1", conn))
                        activePlans = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT AVG(MonthlyPricePesos) FROM Subscriptions", conn))
                    {
                        var res = cmd.ExecuteScalar();
                        if (res != DBNull.Value) avgPrice = Convert.ToDecimal(res);
                    }
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM TenantSubscriptions WHERE IsActive=1", conn))
                        totalSubscribers = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Plans", totalPlans.ToString(), ThemeHelper.Primary, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Active Plans", activePlans.ToString(), ThemeHelper.Success, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Avg. Monthly Price", "₱" + avgPrice.ToString("N0"), ThemeHelper.Info, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Active Subscribers", totalSubscribers.ToString(), ThemeHelper.Accent, 220, 100));

            this.Controls.Add(kpiPanel);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 195);
            cardForm.Size = new Size(400, 280);
            cardForm.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Plan Details");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;

            // Monthly Price (Pesos)
            var lblMonthlyPesos = new Label { Text = "Monthly Price (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblMonthlyPesos);
            nudMonthlyPricePesos = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 500000, Minimum = 0, DecimalPlaces = 0, Value = 5500 };
            cardForm.Controls.Add(nudMonthlyPricePesos);
            y += 34;

            // Yearly Price (Pesos)
            var lblYearlyPesos = new Label { Text = "Yearly Price (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblYearlyPesos);
            nudYearlyPricePesos = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 1000000, Minimum = 0, DecimalPlaces = 0, Value = 55000 };
            cardForm.Controls.Add(nudYearlyPricePesos);
            y += 34;

            chkIsActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, AutoSize = true, Location = new Point(20, y), Checked = true };
            cardForm.Controls.Add(chkIsActive);
            y += 30;

            // Buttons - Save and Clear
            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 100, 38);
            btnSave.Location = new Point(20, y);
            btnSave.Click += BtnSave_Click;
            cardForm.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(130, y);
            btnClear.Click += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            // Buttons - Edit and Delete (below Save and Clear)
            y += 45;
            var btnEdit = ThemeHelper.CreatePrimaryButton("Edit", 100, 38);
            btnEdit.Location = new Point(20, y);
            btnEdit.Click += BtnEdit_Click;
            cardForm.Controls.Add(btnEdit);

            var btnDelete = ThemeHelper.CreateDangerButton("Delete", 100, 38);
            btnDelete.Location = new Point(130, y);
            btnDelete.Click += BtnDelete_Click;
            cardForm.Controls.Add(btnDelete);

            this.Controls.Add(cardForm);

            // ── Grid Card ────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(420, 195);
            cardGrid.Size = new Size(this.Width - 460, 280);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvSubscriptions = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvSubscriptions);
            dgvSubscriptions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            cardGrid.Controls.Add(dgvSubscriptions);

            var lblGrid = ThemeHelper.CreateSectionHeader("Subscription Plans");
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
                using (var cmd = new SqlCommand("SELECT Id, MonthlyPricePesos, YearlyPricePesos, CreatedDate, IsActive FROM Subscriptions ORDER BY MonthlyPricePesos ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Id            = reader.GetInt32(0),
                            Monthly       = "₱" + reader.GetDecimal(1).ToString("N0"),
                            Yearly        = "₱" + reader.GetDecimal(2).ToString("N0"),
                            DateOfPayment = reader.GetDateTime(3).ToString("MMM dd, yyyy"),
                            Status        = reader.GetBoolean(4) ? "Active" : "Inactive"
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvSubscriptions.DataSource = null;
            dgvSubscriptions.DataSource = rows;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    if (_editingId.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE Subscriptions SET MonthlyPricePesos=@mp, YearlyPricePesos=@yp, IsActive=@ia WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@mp", nudMonthlyPricePesos.Value);
                            cmd.Parameters.AddWithValue("@yp", nudYearlyPricePesos.Value);
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string planName = "Plan " + DateTime.Now.Ticks.ToString().Substring(0, 5); // Just a generic name
                        using (var cmd = new SqlCommand(@"INSERT INTO Subscriptions (PlanName, MonthlyPricePesos, YearlyPricePesos, MaxRooms, MaxUsers, IncludesAnalytics, IncludesReports, IncludesAPIAccess, IsActive)
                                                         VALUES (@pn, @mp, @yp, 0, 0, 0, 1, 0, @ia)", conn))
                        {
                            cmd.Parameters.AddWithValue("@pn", planName);
                            cmd.Parameters.AddWithValue("@mp", nudMonthlyPricePesos.Value);
                            cmd.Parameters.AddWithValue("@yp", nudYearlyPricePesos.Value);
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Subscription plan saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvSubscriptions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a subscription plan to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvSubscriptions.SelectedRows[0].Cells["Id"].Value;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT MonthlyPricePesos, YearlyPricePesos, IsActive FROM Subscriptions WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            _editingId = id;
                            nudMonthlyPricePesos.Value = r.GetDecimal(0);
                            nudYearlyPricePesos.Value  = r.GetDecimal(1);
                            chkIsActive.Checked        = r.GetBoolean(2);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvSubscriptions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a subscription plan to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvSubscriptions.SelectedRows[0].Cells["Id"].Value;
            if (MessageBox.Show("Are you sure you want to delete this subscription plan?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("DELETE FROM Subscriptions WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

                ClearForm();
                RefreshGrid();
                MessageBox.Show("Subscription plan deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearForm()
        {
            _editingId = null;
            nudMonthlyPricePesos.Value = 5500;
            nudYearlyPricePesos.Value = 55000;
            chkIsActive.Checked = true;
        }
    }
}
