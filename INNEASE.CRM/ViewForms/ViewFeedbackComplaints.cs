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
    /// Manager: Feedback &amp; Complaints — ticket log with status dropdown management,
    /// search/filter bar, color-coded status pills, KPI card filters, and multi-line description.
    /// </summary>
    public class ViewFeedbackComplaints : UserControl
    {
        private DataGridView dgvTickets;
        private TextBox txtSubject, txtDescription, txtSearch;
        private ComboBox cmbGuest, cmbCategory, cmbStatus, cmbPriority, cmbAssigned, cmbFilterStatus;
        private int? _editingId = null;
        private string _activeStatusFilter = null; // null = show all

        public ViewFeedbackComplaints(User currentUser) { Init(); }
        public ViewFeedbackComplaints() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.AutoScroll = true;
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            var lblTitle = new Label
            {
                Text = "Feedback & Complaints",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            this.Controls.Add(lblTitle);

            // ── KPI Row ──────────────────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(8, 48),
                Size = new Size(960, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            int pending = 0, inProgress = 0, resolved = 0, closed = 0, total = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets", conn))
                        total = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Status='Open'", conn))
                        pending = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Status='InProgress'", conn))
                        inProgress = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Status='Resolved'", conn))
                        resolved = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Status='Closed'", conn))
                        closed = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            var kpiPending    = ThemeHelper.CreateKpiCard("Open",        pending.ToString(),    ThemeHelper.Warning, 180, 80);
            var kpiInProgress = ThemeHelper.CreateKpiCard("In Progress", inProgress.ToString(), ThemeHelper.Info,    180, 80);
            var kpiResolved   = ThemeHelper.CreateKpiCard("Resolved",    resolved.ToString(),   ThemeHelper.Success, 180, 80);
            var kpiTotal      = ThemeHelper.CreateKpiCard("Total",       total.ToString(),      ThemeHelper.Primary, 180, 80);

            // KPI card click → filter the grid
            kpiPending.Cursor    = Cursors.Hand;
            kpiInProgress.Cursor = Cursors.Hand;
            kpiResolved.Cursor   = Cursors.Hand;
            kpiTotal.Cursor      = Cursors.Hand;

            kpiPending.Click    += (s, e) => ApplyStatusFilter("Pending");
            kpiInProgress.Click += (s, e) => ApplyStatusFilter("InProgress");
            kpiResolved.Click   += (s, e) => ApplyStatusFilter("Resolved");
            kpiTotal.Click      += (s, e) => ApplyStatusFilter(null);

            // Make child controls inside KPI cards also respond to clicks
            foreach (Control child in kpiPending.Controls)    child.Click += (s, e) => ApplyStatusFilter("Pending");
            foreach (Control child in kpiInProgress.Controls) child.Click += (s, e) => ApplyStatusFilter("InProgress");
            foreach (Control child in kpiResolved.Controls)   child.Click += (s, e) => ApplyStatusFilter("Resolved");
            foreach (Control child in kpiTotal.Controls)      child.Click += (s, e) => ApplyStatusFilter(null);

            kpiPanel.Controls.Add(kpiPending);
            kpiPanel.Controls.Add(kpiInProgress);
            kpiPanel.Controls.Add(kpiResolved);
            kpiPanel.Controls.Add(kpiTotal);
            this.Controls.Add(kpiPanel);

            // ── Form Card ────────────────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(8, 152);
            cardForm.Size     = new Size(390, 470);

            var lblForm = ThemeHelper.CreateSectionHeader("Ticket Details");
            lblForm.Location = new Point(10, 5);
            cardForm.Controls.Add(lblForm);

            int y = 38;

            var guestNames = new List<string>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT FullName FROM Guests ORDER BY FullName", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) guestNames.Add(r.GetString(0));
                }
            }
            catch { }

            (y, cmbGuest) = AddCombo(cardForm, "Guest", y, guestNames.ToArray());

            (y, txtSubject) = AddFieldWithPlaceholder(cardForm, "Subject *", y, "Enter subject...");

            (y, cmbCategory) = AddCombo(cardForm, "Category", y,
                new[] { "Complaint", "Suggestion", "Compliment" });

            (y, cmbStatus) = AddCombo(cardForm, "Status", y,
                new[] { "Open", "InProgress", "Resolved", "Closed" });

            (y, cmbPriority) = AddCombo(cardForm, "Priority", y,
                new[] { "Low", "Medium", "High", "Critical" });

            var assignedUsers = new List<string>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT FullName FROM Users WHERE Role IN ('Manager', 'Staff') ORDER BY FullName", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) assignedUsers.Add(r.GetString(0));
                }
            }
            catch { }

            (y, cmbAssigned) = AddCombo(cardForm, "Assigned To", y, assignedUsers.ToArray());

            // Multi-line Description
            (y, txtDescription) = AddMultilineFieldWithPlaceholder(cardForm, "Description", y, "Enter details...");

            // Buttons
            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 110, 36);
            btnSave.Location = new Point(10, y + 4);
            btnSave.Click   += BtnSave_Click;
            cardForm.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 110, 36);
            btnClear.Location = new Point(130, y + 4);
            btnClear.Click   += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            var btnDelete = ThemeHelper.CreateSecondaryButton("Delete", 110, 36);
            btnDelete.Location = new Point(250, y + 4);
            btnDelete.BackColor = Color.FromArgb(220, 53, 69);
            btnDelete.ForeColor = Color.White;
            btnDelete.Click += BtnDelete_Click;
            cardForm.Controls.Add(btnDelete);

            this.Controls.Add(cardForm);

            // ── Grid Card ────────────────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(408, 152);
            cardGrid.Size     = new Size(570, 470);
            cardGrid.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblGrid = ThemeHelper.CreateSectionHeader("Ticket Log");
            lblGrid.Location = new Point(10, 5);
            cardGrid.Controls.Add(lblGrid);

            // Search + Filter bar
            var pnlSearch = new Panel
            {
                Location  = new Point(8, 30),
                Size      = new Size(550, 36),
                BackColor = Color.Transparent
            };

            string hint = "Search guest, subject, ID...";
            txtSearch = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(240, 30),
                Location    = new Point(0, 3),
                BorderStyle = BorderStyle.FixedSingle,
                Text        = hint,
                ForeColor   = Color.Gray
            };
            txtSearch.Enter += (s, e) =>
            {
                if (txtSearch.Text == hint) { txtSearch.Text = ""; txtSearch.ForeColor = ThemeHelper.TextDark; }
            };
            txtSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = hint; txtSearch.ForeColor = Color.Gray; }
            };
            txtSearch.TextChanged += (s, e) =>
            {
                if (txtSearch.Text != hint) RefreshGrid();
            };
            pnlSearch.Controls.Add(txtSearch);

            var lblFilter = new Label
            {
                Text      = "Status:",
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(252, 8)
            };
            pnlSearch.Controls.Add(lblFilter);

            cmbFilterStatus = new ComboBox
            {
                Font          = ThemeHelper.FontBody,
                Size          = new Size(148, 30),
                Location      = new Point(302, 3),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilterStatus.Items.AddRange(new[] { "All", "Pending", "InProgress", "Resolved", "Closed" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += (s, e) =>
            {
                _activeStatusFilter = cmbFilterStatus.SelectedItem?.ToString() == "All"
                    ? null
                    : cmbFilterStatus.SelectedItem?.ToString();
                RefreshGrid();
            };
            pnlSearch.Controls.Add(cmbFilterStatus);

            cardGrid.Controls.Add(pnlSearch);

            // DataGridView
            dgvTickets = new DataGridView
            {
                Location = new Point(8, 72),
                Size     = new Size(550, 370),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            ThemeHelper.StyleDataGridView(dgvTickets);
            dgvTickets.CellFormatting += DgvTickets_CellFormatting;
            dgvTickets.CellClick      += DgvTickets_CellClick;
            cardGrid.Controls.Add(dgvTickets);

            this.Controls.Add(cardGrid);
        }

        // Helper: single-line text field
        private (int, TextBox) AddField(Panel p, string label, int y)
        {
            var lbl = new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 5)
            };
            p.Controls.Add(lbl);
            var txt = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(210, 30),
                Location    = new Point(130, y),
                BorderStyle = BorderStyle.FixedSingle
            };
            p.Controls.Add(txt);
            return (y + 38, txt);
        }

        // Helper: single-line text field with placeholder
        private (int, TextBox) AddFieldWithPlaceholder(Panel p, string label, int y, string placeholder)
        {
            var lbl = new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 5)
            };
            p.Controls.Add(lbl);
            var txt = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(210, 30),
                Location    = new Point(130, y),
                BorderStyle = BorderStyle.FixedSingle,
                Text        = placeholder,
                ForeColor   = Color.Gray
            };
            txt.Enter += (s, e) =>
            {
                if (txt.Text == placeholder) { txt.Text = ""; txt.ForeColor = ThemeHelper.TextDark; }
            };
            txt.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; }
            };
            p.Controls.Add(txt);
            return (y + 38, txt);
        }

        // Helper: multi-line text field
        private (int, TextBox) AddMultilineField(Panel p, string label, int y)
        {
            var lbl = new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 5)
            };
            p.Controls.Add(lbl);
            var txt = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(210, 70),
                Location    = new Point(130, y),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                AcceptsReturn = true
            };
            p.Controls.Add(txt);
            return (y + 78, txt);
        }

        // Helper: multi-line text field with placeholder
        private (int, TextBox) AddMultilineFieldWithPlaceholder(Panel p, string label, int y, string placeholder)
        {
            var lbl = new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 5)
            };
            p.Controls.Add(lbl);
            var txt = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(210, 70),
                Location    = new Point(130, y),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                AcceptsReturn = true,
                Text        = placeholder,
                ForeColor   = Color.Gray
            };
            txt.Enter += (s, e) =>
            {
                if (txt.Text == placeholder) { txt.Text = ""; txt.ForeColor = ThemeHelper.TextDark; }
            };
            txt.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; }
            };
            p.Controls.Add(txt);
            return (y + 78, txt);
        }

        // Helper: combo/dropdown field
        private (int, ComboBox) AddCombo(Panel p, string label, int y, string[] items)
        {
            var lbl = new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 5)
            };
            p.Controls.Add(lbl);
            var cmb = new ComboBox
            {
                Font          = ThemeHelper.FontBody,
                Size          = new Size(210, 30),
                Location      = new Point(130, y),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmb.Items.AddRange(items);
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            p.Controls.Add(cmb);
            return (y + 38, cmb);
        }

        // ── Filter helper called by KPI cards ─────────────────────────────────
        private void ApplyStatusFilter(string status)
        {
            _activeStatusFilter = status;
            // Also sync the dropdown
            if (cmbFilterStatus != null)
            {
                string display = status == null ? "All" : status;
                for (int i = 0; i < cmbFilterStatus.Items.Count; i++)
                {
                    if (cmbFilterStatus.Items[i].ToString() == display)
                    {
                        cmbFilterStatus.SelectedIndex = i;
                        break;
                    }
                }
            }
            RefreshGrid();
        }

        // ── Grid refresh with search + status filter ───────────────────────────
        private void RefreshGrid()
        {
            string searchHint = "Search guest, subject, ID...";
            string search = (txtSearch?.Text?.ToLower() ?? "") == searchHint.ToLower() ? "" : txtSearch?.Text?.ToLower() ?? "";
            string filter = _activeStatusFilter;

            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = "SELECT Id, GuestName, Subject, Category, Status, Priority, AssignedTo, SubmittedDate FROM FeedbackTickets WHERE 1=1";
                    
                    if (!string.IsNullOrEmpty(filter)) sql += " AND Status=@st";
                    if (!string.IsNullOrEmpty(search)) sql += " AND (LOWER(GuestName) LIKE @q OR LOWER(Subject) LIKE @q OR CAST(Id AS VARCHAR) LIKE @q)";
                    
                    sql += " ORDER BY SubmittedDate DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@st", filter);
                        if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@q", "%" + search + "%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rows.Add(new
                                {
                                    Id             = reader.GetInt32(0),
                                    Guest          = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    Subject        = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Category       = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Status         = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Priority       = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Assigned_To    = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    Date_Submitted = reader.GetDateTime(7).ToString("MMM dd, yyyy")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvTickets.DataSource = null;
            dgvTickets.DataSource = rows;
        }

        // ── Color-code the Status column ──────────────────────────────────────
        private void DgvTickets_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvTickets.Columns.Count == 0 || e.RowIndex < 0) return;

            // Find Status column
            if (dgvTickets.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                switch (status)
                {
                    case "Open":
                        e.CellStyle.ForeColor = Color.FromArgb(210, 120, 0);   // Orange
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold);
                        break;
                    case "InProgress":
                        e.CellStyle.ForeColor = Color.FromArgb(30, 120, 220);  // Blue
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold);
                        break;
                    case "Resolved":
                        e.CellStyle.ForeColor = Color.FromArgb(34, 160, 80);   // Green
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold);
                        break;
                    case "Closed":
                        e.CellStyle.ForeColor = Color.FromArgb(140, 140, 140); // Gray
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Regular);
                        break;
                }
            }

            // Color-code Priority column
            if (dgvTickets.Columns[e.ColumnIndex].Name == "Priority" && e.Value != null)
            {
                string priority = e.Value.ToString();
                switch (priority)
                {
                    case "Critical": e.CellStyle.ForeColor = Color.FromArgb(200, 30,  30);  break;
                    case "High":     e.CellStyle.ForeColor = Color.FromArgb(210, 100, 0);   break;
                    case "Medium":   e.CellStyle.ForeColor = Color.FromArgb(30,  120, 220); break;
                    case "Low":      e.CellStyle.ForeColor = Color.FromArgb(34,  160, 80);  break;
                }
            }
        }

        private void DgvTickets_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvTickets.Rows[e.RowIndex].Cells["Id"].Value;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT GuestName, Subject, Category, Status, Priority, AssignedTo, Description FROM FeedbackTickets WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _editingId = id;
                            string guestName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            if (cmbGuest.Items.Contains(guestName)) cmbGuest.SelectedItem = guestName;

                            txtSubject.Text          = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            txtSubject.ForeColor     = ThemeHelper.TextDark;
                            
                            string category = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            if (cmbCategory.Items.Contains(category)) cmbCategory.SelectedItem = category;
                            
                            string status = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            if (cmbStatus.Items.Contains(status)) cmbStatus.SelectedItem = status;
                            
                            string priority = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            if (cmbPriority.Items.Contains(priority)) cmbPriority.SelectedItem = priority;
                            
                            string assignedTo = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            if (cmbAssigned.Items.Contains(assignedTo)) cmbAssigned.SelectedItem = assignedTo;

                            txtDescription.Text      = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            txtDescription.ForeColor  = ThemeHelper.TextDark;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        // ── Save / Update ──────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (cmbGuest.SelectedItem == null || string.IsNullOrWhiteSpace(cmbGuest.SelectedItem.ToString()))
            {
                MessageBox.Show("Please select a Guest.", "Validation Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbGuest.Focus();
                return;
            }

            string subjectText = txtSubject.Text == "Enter subject..." ? "" : txtSubject.Text;
            if (string.IsNullOrWhiteSpace(subjectText))
            {
                MessageBox.Show("Subject is required. Please enter a subject for this ticket.", "Validation Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSubject.Focus();
                return;
            }

            string descriptionText = txtDescription.Text == "Enter details..." ? "" : txtDescription.Text;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    if (_editingId.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE FeedbackTickets SET GuestName=@gn, Subject=@sub, Category=@cat, Status=@st, Priority=@pr, AssignedTo=@as, Description=@desc WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@gn", cmbGuest.SelectedItem?.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@sub", subjectText.Trim());
                            cmd.Parameters.AddWithValue("@cat", cmbCategory.SelectedItem?.ToString() ?? "Complaint");
                            cmd.Parameters.AddWithValue("@st", cmbStatus.SelectedItem?.ToString() ?? "Open");
                            cmd.Parameters.AddWithValue("@pr", cmbPriority.SelectedItem?.ToString() ?? "Medium");
                            cmd.Parameters.AddWithValue("@as", cmbAssigned.SelectedItem?.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@desc", descriptionText.Trim());
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            cmd.ExecuteNonQuery();
                        }
                        
                        if (cmbStatus.SelectedItem?.ToString() == "Resolved")
                        {
                            using (var cmd = new SqlCommand("UPDATE FeedbackTickets SET ResolvedDate=GETDATE() WHERE Id=@id AND ResolvedDate IS NULL", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", _editingId.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        int guestId = 0;
                        using (var cmd = new SqlCommand("SELECT Id FROM Guests WHERE FullName=@gn", conn))
                        {
                            cmd.Parameters.AddWithValue("@gn", cmbGuest.SelectedItem?.ToString() ?? "");
                            var res = cmd.ExecuteScalar();
                            if (res != null) guestId = (int)res;
                        }

                        using (var cmd = new SqlCommand(@"INSERT INTO FeedbackTickets 
                            (GuestId, GuestName, Subject, Category, Status, Priority, AssignedTo, Description)
                            VALUES (@gid, @gn, @sub, @cat, @st, @pr, @as, @desc)", conn))
                        {
                            cmd.Parameters.AddWithValue("@gid", guestId);
                            cmd.Parameters.AddWithValue("@gn", cmbGuest.SelectedItem?.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@sub", subjectText.Trim());
                            cmd.Parameters.AddWithValue("@cat", cmbCategory.SelectedItem?.ToString() ?? "Complaint");
                            cmd.Parameters.AddWithValue("@st", cmbStatus.SelectedItem?.ToString() ?? "Open");
                            cmd.Parameters.AddWithValue("@pr", cmbPriority.SelectedItem?.ToString() ?? "Medium");
                            cmd.Parameters.AddWithValue("@as", cmbAssigned.SelectedItem?.ToString() ?? "");
                            cmd.Parameters.AddWithValue("@desc", descriptionText.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Ticket saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue)
            {
                MessageBox.Show("Please select a ticket to delete.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this ticket?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("DELETE FROM FeedbackTickets WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

                ClearForm();
                RefreshGrid();
                MessageBox.Show("Ticket deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ── Clear form ────────────────────────────────────────────────────────
        private void ClearForm()
        {
            _editingId          = null;
            txtSubject.Text     = "Enter subject...";
            txtSubject.ForeColor = Color.Gray;
            txtDescription.Text = "Enter details...";
            txtDescription.ForeColor = Color.Gray;
            if (cmbGuest.Items.Count    > 0) cmbGuest.SelectedIndex    = 0;
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            cmbStatus.SelectedIndex   = 0;
            cmbPriority.SelectedIndex = 1;
            if (cmbAssigned.Items.Count > 0) cmbAssigned.SelectedIndex = 0;
        }
    }
}
