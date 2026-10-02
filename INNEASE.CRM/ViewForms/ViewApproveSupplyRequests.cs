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
    /// Simple input dialog for getting user input.
    /// </summary>
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 340 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 250, Width = 80, Top = 90, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 160, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }

    /// <summary>
    /// Admin: Approve/Reject Supply Requests - manage pending supply requests.
    /// </summary>
    public class ViewApproveSupplyRequests : UserControl
    {
        private DataGridView dgvRequests;
        private User _currentUser;

        public ViewApproveSupplyRequests(User currentUser)
        {
            _currentUser = currentUser;
            Init();
        }

        public ViewApproveSupplyRequests() { Init(); }

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
                if (ctrl is Panel cardGrid && cardGrid.Location.Y == 195)
                {
                    cardGrid.Width = this.Width - 40;
                    cardGrid.Height = this.Height - 215;
                }
            }
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Approve Supply Requests",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Review and approve or reject supply requests from staff",
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

            int totalRequests = 0, pendingRequests = 0, approvedRequests = 0, rejectedRequests = 0;
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
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM SupplyRequests WHERE Status='Rejected'", conn))
                        rejectedRequests = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Requests", totalRequests.ToString(), ThemeHelper.Primary, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Pending", pendingRequests.ToString(), ThemeHelper.Warning, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Approved", approvedRequests.ToString(), ThemeHelper.Success, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Rejected", rejectedRequests.ToString(), ThemeHelper.Danger, 220, 100));

            this.Controls.Add(kpiPanel);

            // ── Grid Card ────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(0, 195);
            cardGrid.Size = new Size(this.Width - 40, 400);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvRequests = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvRequests);
            dgvRequests.CellClick += DgvRequests_CellClick;
            cardGrid.Controls.Add(dgvRequests);

            var lblGrid = ThemeHelper.CreateSectionHeader("Supply Requests");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            // Action buttons panel
            var actionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ThemeHelper.BackgroundLight
            };

            var btnApprove = ThemeHelper.CreateSuccessButton("Approve", 120, 38);
            btnApprove.Location = new Point(20, 10);
            btnApprove.Click += BtnApprove_Click;
            actionPanel.Controls.Add(btnApprove);

            var btnReject = ThemeHelper.CreateDangerButton("Reject", 120, 38);
            btnReject.Location = new Point(150, 10);
            btnReject.Click += BtnReject_Click;
            actionPanel.Controls.Add(btnReject);

            var btnRefresh = ThemeHelper.CreateSecondaryButton("Refresh", 100, 38);
            btnRefresh.Location = new Point(280, 10);
            btnRefresh.Click += (s, e) => RefreshGrid();
            actionPanel.Controls.Add(btnRefresh);

            cardGrid.Controls.Add(actionPanel);

            this.Controls.Add(cardGrid);
        }

        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, SupplyName, RequestedQuantity, RequestedBy, Department, Priority, Reason, Status, RequestDate, ApprovedBy, ApprovedDate FROM SupplyRequests ORDER BY RequestDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Id           = reader.GetInt32(0),
                            Supply       = reader.GetString(1),
                            Quantity     = reader.GetInt32(2),
                            RequestedBy  = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Department   = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Priority     = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Reason       = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Status       = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            RequestDate  = reader.GetDateTime(8).ToString("MMM dd, yyyy"),
                            ApprovedBy   = reader.IsDBNull(9) ? "N/A" : reader.GetString(9),
                            ApprovedDate = reader.IsDBNull(10) ? "N/A" : reader.GetDateTime(10).ToString("MMM dd, yyyy")
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvRequests.DataSource = null;
            dgvRequests.DataSource = rows;
        }

        private void BtnApprove_Click(object sender, EventArgs e)
        {
            if (dgvRequests.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a request to approve.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvRequests.SelectedRows[0].Cells["Id"].Value;
            
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string status = "";
                    int supplyId = 0, reqQty = 0;
                    using (var cmd = new SqlCommand("SELECT Status, SupplyId, RequestedQuantity FROM SupplyRequests WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                status = r.IsDBNull(0) ? "" : r.GetString(0);
                                supplyId = r.GetInt32(1);
                                reqQty = r.GetInt32(2);
                            }
                        }
                    }

                    if (status != "Pending")
                    {
                        MessageBox.Show("Only pending requests can be approved.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    if (_currentUser == null)
                    {
                        MessageBox.Show("User information not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (var cmd = new SqlCommand("UPDATE SupplyRequests SET Status='Approved', ApprovedDate=GETDATE(), ApprovedBy=@u, FulfilledDate=GETDATE() WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", _currentUser.FullName);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqlCommand("UPDATE Supplies SET Quantity = Quantity + @qty, LastRestockedDate=GETDATE() WHERE Id=@sid", conn))
                    {
                        cmd.Parameters.AddWithValue("@qty", reqQty);
                        cmd.Parameters.AddWithValue("@sid", supplyId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            RefreshGrid();
            MessageBox.Show("Request approved and supply restocked!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReject_Click(object sender, EventArgs e)
        {
            if (dgvRequests.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a request to reject.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvRequests.SelectedRows[0].Cells["Id"].Value;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string status = "";
                    using (var cmd = new SqlCommand("SELECT Status FROM SupplyRequests WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var res = cmd.ExecuteScalar();
                        if (res != null) status = res.ToString();
                    }

                    if (status != "Pending")
                    {
                        MessageBox.Show("Only pending requests can be rejected.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (_currentUser == null)
                    {
                        MessageBox.Show("User information not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var reason = Prompt.ShowDialog("Enter rejection reason:", "Reject Request", "");
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        MessageBox.Show("Rejection cancelled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    using (var cmd = new SqlCommand("UPDATE SupplyRequests SET Status='Rejected', ApprovedDate=GETDATE(), ApprovedBy=@u, Notes=@n WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", _currentUser.FullName);
                        cmd.Parameters.AddWithValue("@n", reason);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            RefreshGrid();
            MessageBox.Show("Request rejected.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DgvRequests_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Selection is handled by the DataGridView
        }
    }
}
