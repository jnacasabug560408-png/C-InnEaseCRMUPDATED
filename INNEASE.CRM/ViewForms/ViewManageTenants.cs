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
    /// Super Admin: Manage Tenants — add, edit, search, and view tenant properties
    /// with status badges, interactive KPI cards, and a search/filter bar.
    /// </summary>
    public class ViewManageTenants : UserControl
    {
        private DataGridView dgvTenants;
        private TextBox txtName, txtPropertyName, txtEmail, txtPhone, txtAddress, txtCity, txtSearch;
        private NumericUpDown nudMonthlyPayment;
        private ComboBox cmbBillingCycle, cmbFilterStatus;
        private CheckBox chkIsActive;
        private DateTimePicker dtpStartDate, dtpExpiryDate;
        private int? _editingId = null;
        private string _activeStatusFilter = null;
        private Button btnAdd, btnUpdate, btnClear, btnDelete;

        public ViewManageTenants(User currentUser) { Init(); }
        public ViewManageTenants() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding   = new Padding(20);
            this.AutoScroll = true;
            this.Resize    += (s, e) => UpdateLayout();
            BuildUI();
            RefreshGrid();
        }

        private void UpdateLayout()
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is FlowLayoutPanel kpi && kpi.Location.Y == 70)
                    kpi.Width = this.Width - 40;
                if (ctrl is Panel card && card.Location.X == 420)
                {
                    card.Width  = this.Width - 460;
                    card.Height = this.Height - 215;
                }
            }
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────────
            this.Controls.Add(new Label
            {
                Text      = "Manage Tenants",
                Font      = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize  = true,
                Location  = new Point(0, 0)
            });
            this.Controls.Add(new Label
            {
                Text      = "Add and manage tenant properties and their subscriptions",
                Font      = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize  = true,
                Location  = new Point(0, 35)
            });

            // ── KPI Cards ─────────────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location       = new Point(0, 70),
                Size           = new Size(this.Width - 40, 110),
                Anchor         = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents   = false,
                AutoScroll     = false,
                FlowDirection  = FlowDirection.LeftToRight
            };

            int totalTenants = 0, activeTenants = 0, trialTenants = 0, expiringSoon = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Tenants", conn))
                        totalTenants = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Tenants WHERE IsActive=1 AND (ExpiryDate IS NULL OR ExpiryDate > DATEADD(DAY,30,GETDATE()))", conn))
                        activeTenants = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM TenantSubscriptions WHERE IsTrial=1", conn))
                        trialTenants = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Tenants WHERE IsActive=1 AND ExpiryDate IS NOT NULL AND ExpiryDate > GETDATE() AND ExpiryDate <= DATEADD(DAY,30,GETDATE())", conn))
                        expiringSoon = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            var kpiTotal    = ThemeHelper.CreateKpiCard("Total Tenants",   totalTenants.ToString(),  ThemeHelper.Primary, 210, 100);
            var kpiActive   = ThemeHelper.CreateKpiCard("Active Tenants",  activeTenants.ToString(), ThemeHelper.Success, 210, 100);
            var kpiTrial    = ThemeHelper.CreateKpiCard("Trial Accounts",  trialTenants.ToString(),  ThemeHelper.Warning, 210, 100);
            var kpiExpiring = ThemeHelper.CreateKpiCard("Expiring Soon",   expiringSoon.ToString(),  ThemeHelper.Danger,  210, 100);

            // KPI → filter
            SetKpiFilter(kpiTotal,    null);
            SetKpiFilter(kpiActive,   "Active");
            SetKpiFilter(kpiTrial,    "Trial");
            SetKpiFilter(kpiExpiring, "Expiring");

            kpiPanel.Controls.Add(kpiTotal);
            kpiPanel.Controls.Add(kpiActive);
            kpiPanel.Controls.Add(kpiTrial);
            kpiPanel.Controls.Add(kpiExpiring);
            this.Controls.Add(kpiPanel);

            // ── Form Card ─────────────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 195);
            cardForm.Size     = new Size(410, 520);
            cardForm.Anchor   = AnchorStyles.Top | AnchorStyles.Left;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Tenant Details");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;
            AddField(cardForm, "Name",          ref y, out txtName);
            AddField(cardForm, "Property Name", ref y, out txtPropertyName);
            AddField(cardForm, "Email",         ref y, out txtEmail);
            AddField(cardForm, "Phone",         ref y, out txtPhone);
            AddField(cardForm, "Address",       ref y, out txtAddress);
            AddField(cardForm, "City",          ref y, out txtCity);

            // Monthly Payment
            var lblPayment = new Label { Text = "Monthly Pay (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 5) };
            cardForm.Controls.Add(lblPayment);
            nudMonthlyPayment = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(130, 30), Location = new Point(145, y), Maximum = 1000000, Minimum = 0, DecimalPlaces = 0, Value = 5500 };
            cardForm.Controls.Add(nudMonthlyPayment);
            y += 38;

            // Billing Cycle
            var lblCycle = new Label { Text = "Billing Cycle", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 5) };
            cardForm.Controls.Add(lblCycle);
            cmbBillingCycle = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(130, 30), Location = new Point(145, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbBillingCycle.Items.AddRange(new[] { "Monthly", "Yearly" });
            cmbBillingCycle.SelectedIndex = 0;
            cardForm.Controls.Add(cmbBillingCycle);
            y += 38;

            // Start Date
            var lblStart = new Label { Text = "Start Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 5) };
            cardForm.Controls.Add(lblStart);
            dtpStartDate = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(130, 30), Location = new Point(145, y), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            cardForm.Controls.Add(dtpStartDate);
            y += 38;

            // Expiry Date
            var lblExpiry = new Label { Text = "Expiry Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 5) };
            cardForm.Controls.Add(lblExpiry);
            dtpExpiryDate = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(130, 30), Location = new Point(145, y), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(6) };
            cardForm.Controls.Add(dtpExpiryDate);
            y += 38;

            // Active checkbox
            chkIsActive = new CheckBox { Text = "Active", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextDark, Location = new Point(145, y), AutoSize = true, Checked = true };
            cardForm.Controls.Add(chkIsActive);
            y += 36;

            // Buttons
            var btnPanel = new FlowLayoutPanel
            {
                Location = new Point(20, y + 4),
                Size = new Size(380, 45),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnAdd = ThemeHelper.CreatePrimaryButton("Add", 75, 36);
            btnAdd.Margin = new Padding(0, 0, 10, 0);
            btnAdd.Click += BtnAdd_Click;
            btnPanel.Controls.Add(btnAdd);

            btnUpdate = ThemeHelper.CreatePrimaryButton("Update", 75, 36);
            btnUpdate.Margin = new Padding(0, 0, 10, 0);
            btnUpdate.Click += BtnUpdate_Click;
            btnUpdate.Enabled = false;
            btnPanel.Controls.Add(btnUpdate);

            btnDelete = ThemeHelper.CreateDangerButton("Delete", 75, 36);
            btnDelete.Margin = new Padding(0, 0, 10, 0);
            btnDelete.Click += BtnDelete_Click;
            btnDelete.Enabled = false;
            btnPanel.Controls.Add(btnDelete);

            btnClear = ThemeHelper.CreateSecondaryButton("Clear", 75, 36);
            btnClear.Click += (s, e) => ClearForm();
            btnPanel.Controls.Add(btnClear);

            cardForm.Controls.Add(btnPanel);

            this.Controls.Add(cardForm);

            // ── Grid Card ─────────────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(420, 195);
            cardGrid.Size     = new Size(this.Width - 460, 520);
            cardGrid.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblGrid = ThemeHelper.CreateSectionHeader("Tenant List");
            lblGrid.Location = new Point(10, 5);
            cardGrid.Controls.Add(lblGrid);

            // Search + Filter bar
            var pnlSearch = new Panel { Location = new Point(8, 30), Size = new Size(700, 36), BackColor = Color.Transparent };

            string hint = "Search name, property, email...";
            txtSearch = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(260, 30), Location = new Point(0, 3), BorderStyle = BorderStyle.FixedSingle, Text = hint, ForeColor = Color.Gray };
            txtSearch.Enter       += (s, e) => { if (txtSearch.Text == hint) { txtSearch.Text = ""; txtSearch.ForeColor = ThemeHelper.TextDark; } };
            txtSearch.Leave       += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = hint; txtSearch.ForeColor = Color.Gray; } };
            txtSearch.TextChanged += (s, e) => RefreshGrid();
            pnlSearch.Controls.Add(txtSearch);

            var lblFilter = new Label { Text = "Status:", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(272, 8) };
            pnlSearch.Controls.Add(lblFilter);

            cmbFilterStatus = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(130, 30), Location = new Point(320, 3), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterStatus.Items.AddRange(new[] { "All", "Active", "Trial", "Expiring" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += (s, e) =>
            {
                _activeStatusFilter = cmbFilterStatus.SelectedItem?.ToString() == "All" ? null : cmbFilterStatus.SelectedItem?.ToString();
                RefreshGrid();
            };
            pnlSearch.Controls.Add(cmbFilterStatus);
            cardGrid.Controls.Add(pnlSearch);

            // DataGridView
            dgvTenants = new DataGridView { Location = new Point(8, 72), Size = new Size(cardGrid.Width - 20, cardGrid.Height - 82), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            ThemeHelper.StyleDataGridView(dgvTenants);
            dgvTenants.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTenants.CellFormatting        += DgvTenants_CellFormatting;
            dgvTenants.CellClick             += DgvTenants_CellClick;
            dgvTenants.CellContentClick      += DgvTenants_CellContentClick;
            cardGrid.Controls.Add(dgvTenants);

            this.Controls.Add(cardGrid);
        }

        // Wire KPI card + all its children to a filter
        private void SetKpiFilter(Panel card, string filter)
        {
            card.Cursor = Cursors.Hand;
            card.Click += (s, e) => ApplyStatusFilter(filter);
            foreach (Control c in card.Controls) { c.Cursor = Cursors.Hand; c.Click += (s, e) => ApplyStatusFilter(filter); }
        }

        private void ApplyStatusFilter(string filter)
        {
            _activeStatusFilter = filter;
            if (cmbFilterStatus != null)
            {
                string display = filter ?? "All";
                for (int i = 0; i < cmbFilterStatus.Items.Count; i++)
                    if (cmbFilterStatus.Items[i].ToString() == display) { cmbFilterStatus.SelectedIndex = i; break; }
            }
            RefreshGrid();
        }

        private void AddField(Panel parent, string label, ref int y, out TextBox txt)
        {
            var lbl = new Label { Text = label, Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 5) };
            parent.Controls.Add(lbl);
            txt = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(230, 30), Location = new Point(145, y), BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            y += 38;
        }

        private void RefreshGrid()
        {
            string search = txtSearch?.Text ?? "";
            string hint   = "Search name, property, email...";
            if (search == hint) search = "";

            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = "SELECT Id, Name, PropertyName, ContactEmail, City, MonthlyPaymentAmount, BillingCycle, IsActive, StartDate, ExpiryDate FROM Tenants WHERE 1=1";
                    
                    // Status filter
                    if (_activeStatusFilter == "Active")
                        sql += " AND IsActive=1 AND (ExpiryDate IS NULL OR ExpiryDate > DATEADD(DAY,30,GETDATE()))";
                    else if (_activeStatusFilter == "Expiring")
                        sql += " AND IsActive=1 AND ExpiryDate IS NOT NULL AND ExpiryDate > GETDATE() AND ExpiryDate <= DATEADD(DAY,30,GETDATE())";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool isActive = reader.GetBoolean(7);
                            DateTime? expiry = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
                            string status = !isActive ? "Inactive" :
                                expiry.HasValue && expiry.Value < DateTime.Today ? "Expired" :
                                expiry.HasValue && expiry.Value <= DateTime.Today.AddDays(30) ? "Expiring Soon" : "Active";

                            string name = reader.GetString(1);
                            string prop = reader.GetString(2);
                            string email = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            
                            if (!string.IsNullOrEmpty(search) &&
                                !name.ToLower().Contains(search.ToLower()) &&
                                !prop.ToLower().Contains(search.ToLower()) &&
                                !email.ToLower().Contains(search.ToLower()))
                                continue;

                            rows.Add(new
                            {
                                Id              = reader.GetInt32(0),
                                Name            = name,
                                Property        = prop,
                                Email           = email,
                                City            = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Monthly_Payment = "\u20b1" + reader.GetDecimal(5).ToString("N0"),
                                Billing_Cycle   = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Status          = status,
                                Start_Date      = reader.IsDBNull(8) ? "N/A" : reader.GetDateTime(8).ToString("MMM dd, yyyy"),
                                Expiry          = expiry.HasValue ? expiry.Value.ToString("MMM dd, yyyy") : "N/A"
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading tenants: " + ex.Message); }

            dgvTenants.DataSource = null;
            dgvTenants.DataSource = rows;

            // Action button columns
            if (!dgvTenants.Columns.Contains("Edit"))
            {
                dgvTenants.Columns.Add(new DataGridViewButtonColumn { Name = "Edit",   HeaderText = "", Text = "Edit",   UseColumnTextForButtonValue = true, Width = 55, FillWeight = 8 });
                dgvTenants.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 60, FillWeight = 8 });
            }
        }

        // Smart status: checks expiry date FIRST, then falls back to IsActive flag
        private string GetTenantStatus(Tenant t)
        {
            if (!t.IsActive) return "Inactive";
            if (t.ExpiryDate.HasValue)
            {
                if (t.ExpiryDate.Value < DateTime.Today) return "Expired";
                if (t.ExpiryDate.Value <= DateTime.Today.AddDays(30)) return "Expiring Soon";
            }
            return "Active";
        }

        // Color-code the Status column
        private void DgvTenants_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvTenants.Columns.Count == 0) return;
            if (dgvTenants.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Active":
                        e.CellStyle.ForeColor = Color.FromArgb(34, 139, 60);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Trial":
                        e.CellStyle.ForeColor = Color.FromArgb(30, 100, 200);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Expiring Soon":
                        e.CellStyle.ForeColor = Color.FromArgb(210, 120, 0);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Expired":
                        e.CellStyle.ForeColor = Color.FromArgb(200, 30, 30);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Inactive":
                        e.CellStyle.ForeColor = Color.FromArgb(140, 140, 140);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Regular); break;
                }
            }
        }

        private void DgvTenants_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvTenants.Rows[e.RowIndex].Cells["Id"].Value;
            LoadTenantToForm(id);
        }

        private void DgvTenants_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvTenants.Rows[e.RowIndex].Cells["Id"].Value;
            string col = dgvTenants.Columns[e.ColumnIndex].Name;

            if (col == "Edit")
                LoadTenantToForm(id);
            else if (col == "Delete")
            {
                if (MessageBox.Show("Are you sure you want to delete this tenant?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        using (var conn = DatabaseService.GetConnection())
                        using (var cmd = new SqlCommand("DELETE FROM Tenants WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                        AuditLogger.LogAction("Delete", $"Deleted tenant Id={id}", "Manage Tenants");
                        RefreshGrid(); ClearForm();
                        MessageBox.Show("Tenant deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
            }
        }

        private void LoadTenantToForm(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id,Name,PropertyName,ContactEmail,ContactPhone,Address,City,MonthlyPaymentAmount,BillingCycle,StartDate,ExpiryDate,IsActive FROM Tenants WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return;
                        _editingId              = r.GetInt32(0);
                        txtName.Text            = r.GetString(1);
                        txtPropertyName.Text    = r.GetString(2);
                        txtEmail.Text           = r.IsDBNull(3) ? "" : r.GetString(3);
                        txtPhone.Text           = r.IsDBNull(4) ? "" : r.GetString(4);
                        txtAddress.Text         = r.IsDBNull(5) ? "" : r.GetString(5);
                        txtCity.Text            = r.IsDBNull(6) ? "" : r.GetString(6);
                        nudMonthlyPayment.Value = r.GetDecimal(7);
                        cmbBillingCycle.SelectedItem = r.IsDBNull(8) ? "Monthly" : r.GetString(8);
                        dtpStartDate.Value      = r.IsDBNull(9)  ? DateTime.Today : r.GetDateTime(9);
                        dtpExpiryDate.Value     = r.IsDBNull(10) ? DateTime.Today.AddMonths(6) : r.GetDateTime(10);
                        chkIsActive.Checked     = r.GetBoolean(11);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading tenant: " + ex.Message); return; }

            btnAdd.Enabled    = false;
            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPropertyName.Text))
            {
                MessageBox.Show("Name and Property Name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("INSERT INTO Tenants (Name,PropertyName,ContactEmail,ContactPhone,Address,City,MonthlyPaymentAmount,BillingCycle,StartDate,ExpiryDate,IsActive,CreatedDate) VALUES (@n,@pn,@e,@ph,@ad,@ci,@mp,@bc,@sd,@ed,@ia,GETDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@pn", txtPropertyName.Text.Trim());
                    cmd.Parameters.AddWithValue("@e",  txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@ph", txtPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@ad", txtAddress.Text.Trim());
                    cmd.Parameters.AddWithValue("@ci", txtCity.Text.Trim());
                    cmd.Parameters.AddWithValue("@mp", nudMonthlyPayment.Value);
                    cmd.Parameters.AddWithValue("@bc", cmbBillingCycle.SelectedItem?.ToString() ?? "Monthly");
                    cmd.Parameters.AddWithValue("@sd", dtpStartDate.Value);
                    cmd.Parameters.AddWithValue("@ed", dtpExpiryDate.Value);
                    cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                    cmd.ExecuteNonQuery();
                }
                AuditLogger.LogAction("Create", $"Created tenant: {txtName.Text.Trim()}", "Manage Tenants");
                ClearForm(); RefreshGrid();
                MessageBox.Show("Tenant added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue) return;
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPropertyName.Text))
            {
                MessageBox.Show("Name and Property Name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("UPDATE Tenants SET Name=@n,PropertyName=@pn,ContactEmail=@e,ContactPhone=@ph,Address=@ad,City=@ci,MonthlyPaymentAmount=@mp,BillingCycle=@bc,StartDate=@sd,ExpiryDate=@ed,IsActive=@ia WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _editingId.Value);
                    cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@pn", txtPropertyName.Text.Trim());
                    cmd.Parameters.AddWithValue("@e",  txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@ph", txtPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@ad", txtAddress.Text.Trim());
                    cmd.Parameters.AddWithValue("@ci", txtCity.Text.Trim());
                    cmd.Parameters.AddWithValue("@mp", nudMonthlyPayment.Value);
                    cmd.Parameters.AddWithValue("@bc", cmbBillingCycle.SelectedItem?.ToString() ?? "Monthly");
                    cmd.Parameters.AddWithValue("@sd", dtpStartDate.Value);
                    cmd.Parameters.AddWithValue("@ed", dtpExpiryDate.Value);
                    cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                    cmd.ExecuteNonQuery();
                }
                AuditLogger.LogAction("Update", $"Updated tenant: {txtName.Text.Trim()}", "Manage Tenants");
                ClearForm(); RefreshGrid();
                MessageBox.Show("Tenant updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue) return;
            if (MessageBox.Show("Are you sure you want to delete this tenant?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("DELETE FROM Tenants WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingId.Value);
                        cmd.ExecuteNonQuery();
                    }
                    AuditLogger.LogAction("Delete", $"Deleted tenant Id={_editingId.Value}", "Manage Tenants");
                    RefreshGrid(); ClearForm();
                    MessageBox.Show("Tenant deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
            }
        }

        // ApplyFormToTenant kept for reference but SQL handles this now
        private void ApplyFormToTenant(Tenant t)
        {
            t.Name                  = txtName.Text.Trim();
            t.PropertyName          = txtPropertyName.Text.Trim();
            t.ContactEmail          = txtEmail.Text.Trim();
            t.ContactPhone          = txtPhone.Text.Trim();
            t.Address               = txtAddress.Text.Trim();
            t.City                  = txtCity.Text.Trim();
            t.MonthlyPaymentAmount  = nudMonthlyPayment.Value;
            t.BillingCycle          = cmbBillingCycle.SelectedItem?.ToString() ?? "Monthly";
            t.StartDate             = dtpStartDate.Value;
            t.ExpiryDate            = dtpExpiryDate.Value;
            t.IsActive              = chkIsActive.Checked;
        }

        private void ClearForm()
        {
            _editingId = null;
            txtName.Text = txtPropertyName.Text = txtEmail.Text = txtPhone.Text = "";
            txtAddress.Text = txtCity.Text = "";
            nudMonthlyPayment.Value = 5500;
            if (cmbBillingCycle.Items.Count > 0) cmbBillingCycle.SelectedIndex = 0;
            dtpStartDate.Value  = DateTime.Today;
            dtpExpiryDate.Value = DateTime.Today.AddMonths(6);
            chkIsActive.Checked = true;

            if (btnAdd != null) btnAdd.Enabled = true;
            if (btnUpdate != null) btnUpdate.Enabled = false;
            if (btnDelete != null) btnDelete.Enabled = false;
        }
    }
}
