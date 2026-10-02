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
    /// Admin: Promotions & Loyalty management — create discount vouchers and manage loyalty points.
    /// </summary>
    public class ViewPromotions : UserControl
    {
        private DataGridView dgvPromos;
        private TextBox txtCode, txtDescription, txtReason;
        private NumericUpDown nudDiscount, nudMaxUsage;
        private DateTimePicker dtpStart, dtpEnd;
        private CheckBox chkActive;
        private int? _editingId = null;

        public ViewPromotions(User currentUser) { Init(); }
        public ViewPromotions() { Init(); }

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
            var lblTitle = new Label
            {
                Text = "Promotions & Loyalty",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            this.Controls.Add(lblTitle);

            // ── KPI Row ──────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(8, 50),
                Size = new Size(950, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            int activeCount = 0, totalUsage = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Promotions WHERE IsActive=1", conn))
                        activeCount = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(UsageCount), 0) FROM Promotions", conn))
                        totalUsage = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Active Promos", activeCount.ToString(), ThemeHelper.Success, 200, 85));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Redemptions", totalUsage.ToString(), ThemeHelper.Info, 200, 85));

            this.Controls.Add(kpiPanel);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(8, 155);
            cardForm.Size = new Size(380, 366);

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Voucher Details");
            lblFormTitle.Location = new Point(10, 5);
            cardForm.Controls.Add(lblFormTitle);

            int y = 35;
            AddField(cardForm, "Code", ref y, out txtCode, 160);
            AddField(cardForm, "Description", ref y, out txtDescription, 220);
            AddField(cardForm, "Reason", ref y, out txtReason, 220);

            // Discount
            var lblDisc = new Label { Text = "Discount %", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(10, y + 4) };
            cardForm.Controls.Add(lblDisc);
            nudDiscount = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(80, 28), Location = new Point(110, y), Maximum = 100, Minimum = 1, Value = 10 };
            cardForm.Controls.Add(nudDiscount);
            y += 36;

            // Max Usage
            var lblMax = new Label { Text = "Max Usage", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(10, y + 4) };
            cardForm.Controls.Add(lblMax);
            nudMaxUsage = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(80, 28), Location = new Point(110, y), Maximum = 9999, Minimum = 1, Value = 100 };
            cardForm.Controls.Add(nudMaxUsage);
            y += 36;

            // Dates
            var lblStart = new Label { Text = "Start Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(10, y + 4) };
            cardForm.Controls.Add(lblStart);
            dtpStart = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(180, 28), Location = new Point(110, y), Format = DateTimePickerFormat.Short };
            cardForm.Controls.Add(dtpStart);
            y += 34;

            var lblEnd = new Label { Text = "End Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(10, y + 4) };
            cardForm.Controls.Add(lblEnd);
            dtpEnd = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(180, 28), Location = new Point(110, y), Format = DateTimePickerFormat.Short };
            cardForm.Controls.Add(dtpEnd);
            y += 34;

            chkActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, Checked = true, AutoSize = true, Location = new Point(110, y) };
            cardForm.Controls.Add(chkActive);
            y += 34;

            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 100, 38);
            btnSave.Location = new Point(10, y);
            btnSave.Click += BtnSave_Click;
            cardForm.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(120, y);
            btnClear.Click += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            this.Controls.Add(cardForm);

            // ── Grid ─────────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(400, 155);
            cardGrid.Size = new Size(560, 330);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            dgvPromos = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvPromos);
            dgvPromos.CellClick += DgvPromos_CellClick;
            dgvPromos.CellContentClick += DgvPromos_CellContentClick;
            cardGrid.Controls.Add(dgvPromos);

            var lblGrid = ThemeHelper.CreateSectionHeader("Promotions List");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            this.Controls.Add(cardGrid);
        }

        private void AddField(Panel parent, string label, ref int y, out TextBox txt, int width = 200)
        {
            var lbl = new Label { Text = label, Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(10, y + 4) };
            parent.Controls.Add(lbl);
            txt = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(width, 28), Location = new Point(110, y), BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            y += 36;
        }

        private void RefreshGrid()
        {
            dgvPromos.DataSource = null;
            dgvPromos.Columns.Clear();

            var data = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Code, Description, Reason, DiscountPercent, StartDate, EndDate, IsActive, UsageCount, MaxUsage FROM Promotions", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Code = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Reason = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Discount = reader.GetDecimal(4).ToString("0.##") + "%",
                            Start = reader.GetDateTime(5).ToString("MMM dd, yyyy"),
                            End = reader.GetDateTime(6).ToString("MMM dd, yyyy"),
                            Active = reader.GetBoolean(7) ? "✅" : "❌",
                            Usage = $"{reader.GetInt32(8)}/{reader.GetInt32(9)}"
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading promos: " + ex.Message); }

            dgvPromos.DataSource = data;

            // Add Edit button column
            var editCol = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "Edit",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                Width = 60
            };
            dgvPromos.Columns.Add(editCol);

            // Add Delete button column
            var deleteCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "Delete",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            dgvPromos.Columns.Add(deleteCol);

            // Hide ID column
            dgvPromos.Columns["Id"].Visible = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("Promo Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    if (_editingId.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE Promotions SET Code=@c, Description=@d, Reason=@r, DiscountPercent=@dp, MaxUsage=@mu, StartDate=@sd, EndDate=@ed, IsActive=@ia WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            cmd.Parameters.AddWithValue("@c", txtCode.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@d", txtDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@r", txtReason.Text.Trim());
                            cmd.Parameters.AddWithValue("@dp", nudDiscount.Value);
                            cmd.Parameters.AddWithValue("@mu", (int)nudMaxUsage.Value);
                            cmd.Parameters.AddWithValue("@sd", dtpStart.Value);
                            cmd.Parameters.AddWithValue("@ed", dtpEnd.Value);
                            cmd.Parameters.AddWithValue("@ia", chkActive.Checked);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand("INSERT INTO Promotions (Code, Description, Reason, DiscountPercent, MaxUsage, StartDate, EndDate, IsActive, UsageCount) VALUES (@c, @d, @r, @dp, @mu, @sd, @ed, @ia, 0)", conn))
                        {
                            cmd.Parameters.AddWithValue("@c", txtCode.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@d", txtDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@r", txtReason.Text.Trim());
                            cmd.Parameters.AddWithValue("@dp", nudDiscount.Value);
                            cmd.Parameters.AddWithValue("@mu", (int)nudMaxUsage.Value);
                            cmd.Parameters.AddWithValue("@sd", dtpStart.Value);
                            cmd.Parameters.AddWithValue("@ed", dtpEnd.Value);
                            cmd.Parameters.AddWithValue("@ia", chkActive.Checked);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Promotion saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DgvPromos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvPromos.Rows[e.RowIndex].Cells["Id"].Value;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Code, Description, Reason, DiscountPercent, StartDate, EndDate, IsActive, MaxUsage FROM Promotions WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _editingId = reader.GetInt32(0);
                            txtCode.Text = reader.GetString(1);
                            txtDescription.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            txtReason.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            nudDiscount.Value = reader.GetDecimal(4);
                            dtpStart.Value = reader.GetDateTime(5);
                            dtpEnd.Value = reader.GetDateTime(6);
                            chkActive.Checked = reader.GetBoolean(7);
                            nudMaxUsage.Value = reader.GetInt32(8);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
        }

        private void DgvPromos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Handle Edit button click
            if (dgvPromos.Columns[e.ColumnIndex].Name == "Edit")
            {
                int id = (int)dgvPromos.Rows[e.RowIndex].Cells["Id"].Value;
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("SELECT Id, Code, Description, Reason, DiscountPercent, StartDate, EndDate, IsActive, MaxUsage FROM Promotions WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _editingId = reader.GetInt32(0);
                                txtCode.Text = reader.GetString(1);
                                txtDescription.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                txtReason.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                nudDiscount.Value = reader.GetDecimal(4);
                                dtpStart.Value = reader.GetDateTime(5);
                                dtpEnd.Value = reader.GetDateTime(6);
                                chkActive.Checked = reader.GetBoolean(7);
                                nudMaxUsage.Value = reader.GetInt32(8);
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
            }

            // Handle Delete button click
            if (dgvPromos.Columns[e.ColumnIndex].Name == "Delete")
            {
                int id = (int)dgvPromos.Rows[e.RowIndex].Cells["Id"].Value;
                var result = MessageBox.Show("Are you sure you want to delete this promotion?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        using (var conn = DatabaseService.GetConnection())
                        using (var cmd = new SqlCommand("DELETE FROM Promotions WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                        RefreshGrid();
                        ClearForm();
                        MessageBox.Show("Promotion deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
                }
            }
        }

        private void ClearForm()
        {
            _editingId = null;
            txtCode.Text = txtDescription.Text = txtReason.Text = "";
            nudDiscount.Value = 10;
            nudMaxUsage.Value = 100;
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Today.AddMonths(3);
            chkActive.Checked = true;
        }
    }
}
