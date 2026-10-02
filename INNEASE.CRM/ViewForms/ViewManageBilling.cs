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
    /// Super Admin: Manage Billing — generate invoices, track payment history,
    /// with interactive KPI cards, full invoice grid, and billing history tab.
    /// </summary>
    public class ViewManageBilling : UserControl
    {
        private TabControl tabControl;
        private FlowLayoutPanel kpiPanel;
        private Panel cardForm, cardGrid, cardHistory;

        // Invoice form controls
        private ComboBox cmbTenant, cmbPaymentMethod, cmbBillingTerm;
        private NumericUpDown nudAmount;
        private DateTimePicker dtpIssueDate, dtpDueDate;
        private TextBox txtNotes;

        // Grids
        private DataGridView dgvInvoices, dgvBillingHistory;

        // Filter state
        private int? _editingInvoiceId = null;
        private string _invoiceFilter  = null;

        public ViewManageBilling(User currentUser) { Init(); }
        public ViewManageBilling() { Init(); }

        private void Init()
        {
            this.BackColor  = ThemeHelper.BackgroundLight;
            this.Padding    = new Padding(20);
            this.AutoScroll = true;
            this.Resize    += (s, e) => UpdateLayout();
            BuildUI();
            RefreshKpis();
            RefreshInvoicesGrid();
            RefreshBillingHistoryGrid();
        }

        private void UpdateLayout()
        {
            if (kpiPanel  != null) kpiPanel.Width  = this.Width - 40;
            if (tabControl != null) { tabControl.Width = this.Width - 40; tabControl.Height = this.Height - 215; }
        }

        private void UpdateTabLayout()
        {
            foreach (TabPage tabPage in tabControl.TabPages)
                foreach (Control pageCtrl in tabPage.Controls)
                    if (pageCtrl is Panel card)
                    {
                        card.Width = tabControl.Width - 20;
                        if (card.Location.Y == 155) card.Height = tabPage.Height - 165;
                    }
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────────
            this.Controls.Add(new Label
            {
                Text      = "Manage Billing",
                Font      = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize  = true,
                Location  = new Point(0, 0)
            });
            this.Controls.Add(new Label
            {
                Text      = "Generate invoices and track payment history for tenant subscriptions",
                Font      = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize  = true,
                Location  = new Point(0, 48)
            });

            // ── KPI Cards ─────────────────────────────────────────────────────
            kpiPanel = new FlowLayoutPanel
            {
                Location      = new Point(0, 80),
                Size          = new Size(this.Width - 40, 110),
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents  = false,
                AutoScroll    = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            this.Controls.Add(kpiPanel);

            // ── Tab Control ───────────────────────────────────────────────────
            tabControl = new TabControl
            {
                Location = new Point(0, 195),
                Size     = new Size(this.Width - 40, 480),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tabControl.Resize += (s, e) => UpdateTabLayout();

            BuildInvoicesTab();
            BuildHistoryTab();

            this.Controls.Add(tabControl);
        }

        // ── Invoices Tab ──────────────────────────────────────────────────────
        private void BuildInvoicesTab()
        {
            var tabInvoices = new TabPage("Invoices") { BackColor = ThemeHelper.BackgroundLight };

            // Generate Invoice form card
            cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 0);
            cardForm.Size     = new Size(tabControl.Width - 20, 180);
            cardForm.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Generate Invoice");
            lblFormTitle.Location = new Point(15, 8);
            cardForm.Controls.Add(lblFormTitle);

            // Row 1 — Vertical Stacks
            int row1Y_lbl = 40;
            int row1Y_ctl = 62;
            
            AddLabel(cardForm, "Tenant",      15,  row1Y_lbl);
            cmbTenant = AddCombo(cardForm,    15,  row1Y_ctl, 200, GetTenantNames());
            cmbTenant.SelectedIndexChanged += CmbTenant_SelectedIndexChanged;
            if (cmbTenant.Items.Count > 0) cmbTenant.SelectedIndex = 0;

            AddLabel(cardForm, "Amount (₱)",  230, row1Y_lbl);
            nudAmount = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(140, 28), Location = new Point(230, row1Y_ctl), Maximum = 10000000, Minimum = 0, DecimalPlaces = 2, Value = 5500, ThousandsSeparator = true };
            cardForm.Controls.Add(nudAmount);

            AddLabel(cardForm, "Issue Date",  385, row1Y_lbl);
            dtpIssueDate = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(130, 28), Location = new Point(385, row1Y_ctl), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            cardForm.Controls.Add(dtpIssueDate);

            AddLabel(cardForm, "Due Date",    530, row1Y_lbl);
            dtpDueDate = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(130, 28), Location = new Point(530, row1Y_ctl), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(15) };
            cardForm.Controls.Add(dtpDueDate);

            // Row 2 — Vertical Stacks
            int row2Y_lbl = 100;
            int row2Y_ctl = 122;
            
            AddLabel(cardForm, "Payment",    15, row2Y_lbl);
            cmbPaymentMethod = AddCombo(cardForm, 15, row2Y_ctl, 200, new[] { "Bank Transfer", "Credit Card", "Cash", "Check" });

            AddLabel(cardForm, "Term",       230, row2Y_lbl);
            cmbBillingTerm = AddCombo(cardForm, 230, row2Y_ctl, 140, new[] { "Monthly", "Yearly" });
            cmbBillingTerm.SelectedIndexChanged += CmbBillingTerm_SelectedIndexChanged;

            AddLabel(cardForm, "Notes",      385, row2Y_lbl);
            txtNotes = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(275, 28), Location = new Point(385, row2Y_ctl), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtNotes);

            // Buttons
            var btnGenerate = ThemeHelper.CreatePrimaryButton("Generate Invoice", 150, 34);
            btnGenerate.Location = new Point(680, row2Y_ctl - 2);
            btnGenerate.Click   += BtnGenerateInvoice_Click;
            cardForm.Controls.Add(btnGenerate);

            var btnMarkPaid = ThemeHelper.CreateSuccessButton("Mark Paid", 110, 34);
            btnMarkPaid.Location = new Point(840, row2Y_ctl - 2);
            btnMarkPaid.Click   += BtnMarkPaid_Click;
            cardForm.Controls.Add(btnMarkPaid);

            var btnVoid = ThemeHelper.CreateDangerButton("Void", 80, 34);
            btnVoid.Location = new Point(960, row2Y_ctl - 2);
            btnVoid.Click   += BtnVoidInvoice_Click;
            cardForm.Controls.Add(btnVoid);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 80, 34);
            btnClear.Location = new Point(1050, row2Y_ctl - 2);
            btnClear.Click   += (s, e) => ClearInvoiceForm();
            cardForm.Controls.Add(btnClear);

            tabInvoices.Controls.Add(cardForm);

            // ── Invoices Grid ─────────────────────────────────────────────────
            cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(0, 190);
            cardGrid.Size     = new Size(tabControl.Width - 20, tabControl.Height - 220);
            cardGrid.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblGrid = ThemeHelper.CreateSectionHeader("Invoice List");
            lblGrid.Location = new Point(10, 5);
            cardGrid.Controls.Add(lblGrid);

            dgvInvoices = new DataGridView { Location = new Point(8, 34), Size = new Size(cardGrid.Width - 20, cardGrid.Height - 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            ThemeHelper.StyleDataGridView(dgvInvoices);
            dgvInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvInvoices.CellFormatting     += DgvInvoices_CellFormatting;
            dgvInvoices.CellClick          += DgvInvoices_CellClick;
            cardGrid.Controls.Add(dgvInvoices);

            tabInvoices.Controls.Add(cardGrid);
            tabControl.TabPages.Add(tabInvoices);
        }

        // ── Billing History Tab ───────────────────────────────────────────────
        private void BuildHistoryTab()
        {
            var tabHistory = new TabPage("Billing History") { BackColor = ThemeHelper.BackgroundLight };

            cardHistory = ThemeHelper.CreateCardPanel();
            cardHistory.Location = new Point(0, 0);
            cardHistory.Size     = new Size(tabControl.Width - 20, tabControl.Height - 30);
            cardHistory.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblHistory = ThemeHelper.CreateSectionHeader("Payment Transaction Log");
            lblHistory.Location = new Point(10, 5);
            cardHistory.Controls.Add(lblHistory);

            dgvBillingHistory = new DataGridView { Location = new Point(8, 34), Size = new Size(cardHistory.Width - 20, cardHistory.Height - 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            ThemeHelper.StyleDataGridView(dgvBillingHistory);
            dgvBillingHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            cardHistory.Controls.Add(dgvBillingHistory);

            tabHistory.Controls.Add(cardHistory);
            tabControl.TabPages.Add(tabHistory);
        }

        // ── KPI helper ────────────────────────────────────────────────────────
        private string[] GetTenantNames()
        {
            var names = new List<string>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Name FROM Tenants ORDER BY Name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) names.Add(reader.GetString(0));
                }
            }
            catch { }
            return names.ToArray();
        }

        private void RefreshKpis()
        {
            kpiPanel.Controls.Clear();

            decimal totalMRR = 0;
            decimal amountToCollect = 0;
            decimal totalCollected = 0;
            int activeCount = 0;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(MonthlyPaymentAmount), 0) FROM Tenants WHERE IsActive=1 AND (ExpiryDate IS NULL OR ExpiryDate >= CAST(GETDATE() AS DATE))", conn))
                        totalMRR = (decimal)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM Invoices WHERE Status IN ('Pending', 'Unpaid')", conn))
                        amountToCollect = (decimal)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM BillingHistory WHERE Status='Completed'", conn))
                        totalCollected = (decimal)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Tenants WHERE IsActive=1 AND (ExpiryDate IS NULL OR ExpiryDate >= CAST(GETDATE() AS DATE))", conn))
                        activeCount = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            var kpiMrr     = ThemeHelper.CreateKpiCard("Monthly Revenue (MRR)", "₱" + totalMRR.ToString("N0"),       ThemeHelper.Primary, 220, 100);
            var kpiCollect = ThemeHelper.CreateKpiCard("To Collect",            "₱" + amountToCollect.ToString("N0"), ThemeHelper.Warning, 220, 100);
            var kpiPaid    = ThemeHelper.CreateKpiCard("Total Collected",        "₱" + totalCollected.ToString("N0"), ThemeHelper.Success, 220, 100);
            var kpiActive  = ThemeHelper.CreateKpiCard("Active Tenants",         activeCount.ToString(),              ThemeHelper.Info,    220, 100);

            SetKpiFilter(kpiCollect, "Unpaid");
            SetKpiFilter(kpiPaid,    "Paid");
            SetKpiFilter(kpiMrr,     null);
            SetKpiFilter(kpiActive,  null);

            kpiPanel.Controls.Add(kpiMrr);
            kpiPanel.Controls.Add(kpiCollect);
            kpiPanel.Controls.Add(kpiPaid);
            kpiPanel.Controls.Add(kpiActive);
        }

        private void SetKpiFilter(Panel card, string filter)
        {
            card.Cursor = Cursors.Hand;
            card.Click += (s, e) => ApplyInvoiceFilter(filter);
            foreach (Control c in card.Controls) { c.Cursor = Cursors.Hand; c.Click += (s, e) => ApplyInvoiceFilter(filter); }
        }

        private void ApplyInvoiceFilter(string filter)
        {
            _invoiceFilter = filter;
            RefreshInvoicesGrid();
        }

        // ── Form helpers ────────────────────────────────────────────────────
        private void AddLabel(Panel parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label { Text = text, Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(x, y) });
        }

        private ComboBox AddCombo(Panel parent, int x, int y, int width, string[] items)
        {
            var cmb = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(width, 28), Location = new Point(x, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.AddRange(items);
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            parent.Controls.Add(cmb);
            return cmb;
        }

        // ── Grid refresh ──────────────────────────────────────────────────────
        private void RefreshInvoicesGrid()
        {
            var data = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = @"
                        SELECT i.Id, i.InvoiceNumber, t.Name, i.BillingPeriod, i.Amount, i.IssueDate, i.DueDate, i.PaymentMethod, i.Status
                        FROM Invoices i
                        LEFT JOIN Tenants t ON i.TenantId = t.Id";

                    if (!string.IsNullOrEmpty(_invoiceFilter))
                    {
                        if (_invoiceFilter == "Paid") sql += " WHERE i.Status = 'Paid'";
                        else if (_invoiceFilter == "Unpaid") sql += " WHERE i.Status IN ('Pending', 'Unpaid')";
                        else if (_invoiceFilter == "Overdue") sql += " WHERE i.Status = 'Overdue'";
                    }

                    sql += " ORDER BY i.IssueDate DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Invoice_No = reader.GetString(1),
                                Tenant = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                                Billing_Term = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Amount = "₱" + reader.GetDecimal(4).ToString("N2"),
                                Issue_Date = reader.GetDateTime(5).ToString("MMM dd, yyyy"),
                                Due_Date = reader.GetDateTime(6).ToString("MMM dd, yyyy"),
                                Payment_Method = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Status = reader.GetString(8)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading invoices: " + ex.Message); }

            dgvInvoices.DataSource = null;
            dgvInvoices.DataSource = data;
        }

        private void RefreshBillingHistoryGrid()
        {
            var data = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string sql = @"
                        SELECT bh.Id, i.InvoiceNumber, t.Name, bh.Amount, bh.PaymentDate, bh.PaymentMethod, bh.TransactionReference
                        FROM BillingHistory bh
                        LEFT JOIN Invoices i ON bh.InvoiceId = i.Id
                        LEFT JOIN Tenants t ON bh.TenantId = t.Id
                        ORDER BY bh.PaymentDate DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new
                            {
                                Transaction_ID = reader.GetInt32(0),
                                Invoice_No = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                                Tenant = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                                Amount_Paid = "₱" + reader.GetDecimal(3).ToString("N2"),
                                Payment_Date = reader.GetDateTime(4).ToString("MMM dd, yyyy"),
                                Payment_Method = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Reference_No = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading billing history: " + ex.Message); }

            dgvBillingHistory.DataSource = null;
            dgvBillingHistory.DataSource = data;
        }

        // ── Color-code Status column ──────────────────────────────────────────
        private void DgvInvoices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvInvoices.Columns.Count == 0) return;
            if (dgvInvoices.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Paid":
                        e.CellStyle.ForeColor = Color.FromArgb(34, 139, 60);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Pending":
                    case "Unpaid":
                        e.CellStyle.ForeColor = Color.FromArgb(210, 120, 0);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Overdue":
                        e.CellStyle.ForeColor = Color.FromArgb(200, 30, 30);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Voided":
                        e.CellStyle.ForeColor = Color.FromArgb(140, 140, 140);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Regular); break;
                }
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────
        private void CmbBillingTerm_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInvoiceAmount();
            // Auto-calculate due date: Monthly=+30 days, Yearly=+365 days
            if (dtpIssueDate != null && dtpDueDate != null && cmbBillingTerm?.SelectedItem != null)
            {
                dtpDueDate.Value = cmbBillingTerm.SelectedItem.ToString() == "Yearly"
                    ? dtpIssueDate.Value.AddDays(365)
                    : dtpIssueDate.Value.AddDays(30);
            }
        }
        private void CmbTenant_SelectedIndexChanged(object sender, EventArgs e) => UpdateInvoiceAmount();

        private void UpdateInvoiceAmount()
        {
            if (cmbTenant?.SelectedItem == null || cmbBillingTerm?.SelectedItem == null || nudAmount == null) return;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT MonthlyPaymentAmount FROM Tenants WHERE Name=@n", conn))
                {
                    cmd.Parameters.AddWithValue("@n", cmbTenant.SelectedItem.ToString());
                    var obj = cmd.ExecuteScalar();
                    if (obj != null)
                    {
                        decimal monthly = (decimal)obj;
                        nudAmount.Value = cmbBillingTerm.SelectedItem.ToString() == "Yearly" ? monthly * 12 : monthly;
                    }
                }
            }
            catch { }
        }

        private void DgvInvoices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvInvoices.Rows[e.RowIndex].Cells["Id"].Value;
            
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string tName = null;
                    using (var cmd = new SqlCommand(@"
                        SELECT i.Amount, i.IssueDate, i.DueDate, i.PaymentMethod, i.Notes, t.Name 
                        FROM Invoices i LEFT JOIN Tenants t ON i.TenantId = t.Id WHERE i.Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                nudAmount.Value = reader.GetDecimal(0);
                                dtpIssueDate.Value = reader.GetDateTime(1);
                                dtpDueDate.Value = reader.GetDateTime(2);
                                
                                string pm = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                if (cmbPaymentMethod.Items.Contains(pm)) cmbPaymentMethod.SelectedItem = pm;
                                
                                txtNotes.Text = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                tName = reader.IsDBNull(5) ? null : reader.GetString(5);
                                _editingInvoiceId = id;
                            }
                        }
                    }
                    if (tName != null && cmbTenant.Items.Contains(tName)) cmbTenant.SelectedItem = tName;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading invoice: " + ex.Message); }
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            if (cmbTenant.SelectedItem == null)
            {
                MessageBox.Show("Please select a tenant.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    int tenantId = 0;
                    string tName = cmbTenant.SelectedItem.ToString();
                    int isActiveTenant = 0;

                    using (var cmd = new SqlCommand("SELECT Id, IsActive FROM Tenants WHERE Name=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", tName);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                tenantId     = r.GetInt32(0);
                                isActiveTenant = Convert.ToInt32(r.GetBoolean(1));
                            }
                        }
                    }

                    if (tenantId == 0) return;

                    if (isActiveTenant == 0)
                    {
                        MessageBox.Show("This tenant is inactive and cannot be billed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int count = 0;
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Invoices", conn))
                        count = (int)cmd.ExecuteScalar();

                    string invoiceNumber = $"INV-{DateTime.Now.Year}-{(count + 1):D3}";
                    string term = cmbBillingTerm.SelectedItem?.ToString() ?? "Monthly";
                    string billingPeriod = dtpIssueDate.Value.ToString("MMMM yyyy") + $" ({term})";
                    string paymentMethod = cmbPaymentMethod.SelectedItem?.ToString() ?? "Bank Transfer";

                    using (var insertCmd = new SqlCommand(@"
                        INSERT INTO Invoices (TenantId, InvoiceNumber, IssueDate, DueDate, Amount, Currency, BillingPeriod, Status, PaymentMethod, Notes) 
                        VALUES (@tId, @invNum, @issue, @due, @amt, 'PHP', @bp, 'Unpaid', @pm, @notes)", conn))
                    {
                        insertCmd.Parameters.AddWithValue("@tId",    tenantId);
                        insertCmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        insertCmd.Parameters.AddWithValue("@issue",  dtpIssueDate.Value);
                        insertCmd.Parameters.AddWithValue("@due",    dtpDueDate.Value);
                        insertCmd.Parameters.AddWithValue("@amt",    nudAmount.Value);
                        insertCmd.Parameters.AddWithValue("@bp",     billingPeriod);
                        insertCmd.Parameters.AddWithValue("@pm",     paymentMethod);
                        insertCmd.Parameters.AddWithValue("@notes",  txtNotes.Text.Trim());
                        insertCmd.ExecuteNonQuery();
                    }

                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Create", $"Generated invoice {invoiceNumber} for {tName}", "Manage Billing");
                    MessageBox.Show($"Invoice {invoiceNumber} generated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

            ClearInvoiceForm();
            RefreshInvoicesGrid();
            RefreshKpis();
        }

        private void BtnMarkPaid_Click(object sender, EventArgs e)
        {
            if (!_editingInvoiceId.HasValue)
            {
                MessageBox.Show("Please select an invoice row first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    int tId = 0;
                    decimal amt = 0;
                    string curr = "PHP", pm = "", invNum = "", bp = "";

                    using (var cmd = new SqlCommand("SELECT TenantId, Amount, Currency, PaymentMethod, InvoiceNumber, BillingPeriod FROM Invoices WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingInvoiceId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tId = reader.GetInt32(0);
                                amt = reader.GetDecimal(1);
                                curr = reader.IsDBNull(2) ? "PHP" : reader.GetString(2);
                                pm = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                invNum = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                bp = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            }
                        }
                    }

                    if (tId == 0) return;

                    using (var cmd = new SqlCommand("UPDATE Invoices SET Status='Paid', PaidDate=GETDATE() WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingInvoiceId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    int bhCount = 0;
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM BillingHistory", conn))
                        bhCount = (int)cmd.ExecuteScalar();

                    string txnRef = $"TXN-{DateTime.Now:yyyy-MM-dd}-{(bhCount + 1):D3}";
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO BillingHistory (TenantId, InvoiceId, Amount, Currency, PaymentMethod, TransactionReference, Status) 
                        VALUES (@tId, @invId, @amt, @curr, @pm, @txnRef, 'Completed')", conn))
                    {
                        cmd.Parameters.AddWithValue("@tId", tId);
                        cmd.Parameters.AddWithValue("@invId", _editingInvoiceId.Value);
                        cmd.Parameters.AddWithValue("@amt", amt);
                        cmd.Parameters.AddWithValue("@curr", curr);
                        cmd.Parameters.AddWithValue("@pm", pm);
                        cmd.Parameters.AddWithValue("@txnRef", txnRef);
                        cmd.ExecuteNonQuery();
                    }

                    // Update expiry
                    int monthsToAdd = bp.Contains("Yearly") ? 12 : 1;
                    using (var cmd = new SqlCommand("UPDATE Tenants SET ExpiryDate = DATEADD(month, @m, ISNULL(ExpiryDate, CAST(GETDATE() AS DATE))) WHERE Id=@tId", conn))
                    {
                        cmd.Parameters.AddWithValue("@m", monthsToAdd);
                        cmd.Parameters.AddWithValue("@tId", tId);
                        cmd.ExecuteNonQuery();
                    }

                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Marked invoice {invNum} as paid", "Manage Billing");
                    MessageBox.Show("Invoice marked as paid!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

            _editingInvoiceId = null;
            RefreshInvoicesGrid();
            RefreshBillingHistoryGrid();
            RefreshKpis();
        }

        private void BtnVoidInvoice_Click(object sender, EventArgs e)
        {
            if (!_editingInvoiceId.HasValue)
            {
                MessageBox.Show("Please select an invoice row first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var confirm = MessageBox.Show("Are you sure you want to void/delete this invoice?", "Confirm Void", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string invNum = "";
                    using (var cmd = new SqlCommand("SELECT InvoiceNumber FROM Invoices WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingInvoiceId.Value);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) invNum = obj.ToString();
                    }

                    using (var cmd = new SqlCommand("UPDATE Invoices SET Status='Voided' WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingInvoiceId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    _editingInvoiceId = null;
                    RefreshInvoicesGrid();
                    RefreshKpis();
                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Voided invoice {invNum}", "Manage Billing");
                    MessageBox.Show("Invoice voided.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
        }

        private void ClearInvoiceForm()
        {
            _editingInvoiceId = null;
            if (cmbTenant.Items.Count > 0)         cmbTenant.SelectedIndex = 0;
            if (cmbPaymentMethod.Items.Count > 0)  cmbPaymentMethod.SelectedIndex = 0;
            if (cmbBillingTerm.Items.Count > 0)    cmbBillingTerm.SelectedIndex = 0;
            nudAmount.Value    = 5500;
            dtpIssueDate.Value = DateTime.Today;
            dtpDueDate.Value   = DateTime.Today.AddDays(15);
            txtNotes.Text      = "";
        }
    }
}
