using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using INNEASE.CRM.Controllers;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Manage Branches — add, edit, delete branch locations with full CRUD operations.
    /// </summary>
    public class ViewManageBranches : UserControl
    {
        private DataGridView dgvBranches;
        private TextBox txtBranchName, txtBranchCode, txtAddress, txtCity, txtState, txtCountry, txtPhone, txtEmail, txtSearch;
        private CheckBox chkIsActive;
        private Button btnSave, btnClear, btnEdit, btnDelete;
        private int? _editingId = null;
        private readonly BranchController _branchController = new BranchController();

        public ViewManageBranches(User currentUser) { Init(); }
        public ViewManageBranches() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.AutoScroll = false;
            this.Resize += (s, e) => UpdateLayout();
            BuildUI();
            RefreshGrid();
        }

        private void UpdateLayout()
        {
            // Layout is now handled by Anchor properties and DockStyle
        }

        private void BuildUI()
        {
            // ── Main Content Panel (no scroll, uses full window) ───────
            var mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.BackgroundLight,
                Padding = new Padding(20, 30, 20, 20) // Increased top padding
            };
            this.Controls.Add(mainContent);

            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Manage Branches",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            mainContent.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Add, edit, and manage hotel branch locations",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 38)
            };
            mainContent.Controls.Add(lblSubtitle);

            // ── TableLayoutPanel for Main Layout (2 columns, 1 row) ───────
            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ThemeHelper.BackgroundLight,
                Location = new Point(0, 70), // Position below header
                Size = new Size(mainContent.Width - 40, mainContent.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380)); // Column 0: 380px fixed
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Column 1: 100% fill
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Row 0: 100% fill
            mainContent.Controls.Add(tableLayoutPanel);

            // ── Column 0: Input Form Panel ─────────────────────────────
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.BackgroundLight,
                Padding = new Padding(0, 0, 10, 0)
            };
            tableLayoutPanel.Controls.Add(formPanel, 0, 0);

            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Dock = DockStyle.Fill;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Branch Details");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;

            // Branch Name
            var lblBranchName = new Label { Text = "Branch Name", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblBranchName);
            txtBranchName = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(220, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtBranchName);
            y += 34;

            // Branch Code
            var lblBranchCode = new Label { Text = "Branch Code", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblBranchCode);
            txtBranchCode = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle, MaxLength = 20 };
            cardForm.Controls.Add(txtBranchCode);
            y += 34;

            // Address
            var lblAddress = new Label { Text = "Address", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblAddress);
            txtAddress = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(220, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtAddress);
            y += 34;

            // City
            var lblCity = new Label { Text = "City", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblCity);
            txtCity = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtCity);
            y += 34;

            // State
            var lblState = new Label { Text = "State/Province", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblState);
            txtState = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtState);
            y += 34;

            // Country
            var lblCountry = new Label { Text = "Country", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblCountry);
            txtCountry = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle, Text = "Philippines" };
            cardForm.Controls.Add(txtCountry);
            y += 34;

            // Phone
            var lblPhone = new Label { Text = "Phone", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblPhone);
            txtPhone = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtPhone);
            y += 34;

            // Email
            var lblEmail = new Label { Text = "Email", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblEmail);
            txtEmail = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(220, 28), Location = new Point(120, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtEmail);
            y += 34;

            // Active
            chkIsActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, AutoSize = true, Location = new Point(20, y), Checked = true };
            cardForm.Controls.Add(chkIsActive);
            y += 30;

            // Buttons - Row 1: Add and Clear
            btnSave = ThemeHelper.CreatePrimaryButton("Add", 100, 38);
            btnSave.Location = new Point(20, y);
            btnSave.Click += BtnSave_Click;
            cardForm.Controls.Add(btnSave);

            btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(130, y);
            btnClear.Click += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            // Buttons - Row 2: Edit and Delete (below)
            y += 45;
            btnEdit = ThemeHelper.CreateSecondaryButton("Edit", 100, 38);
            btnEdit.Location = new Point(20, y);
            btnEdit.Click += (s, e) => EditCurrentBranch();
            btnEdit.Enabled = false;
            cardForm.Controls.Add(btnEdit);

            btnDelete = ThemeHelper.CreateDangerButton("Delete", 100, 38);
            btnDelete.Location = new Point(130, y);
            btnDelete.Click += (s, e) => DeleteCurrentBranch();
            btnDelete.Enabled = false;
            cardForm.Controls.Add(btnDelete);

            formPanel.Controls.Add(cardForm);

            // ── Column 1: DataGridView Panel ─────────────────────────────
            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.BackgroundLight,
                Padding = new Padding(0, 0, 0, 0)
            };
            tableLayoutPanel.Controls.Add(gridPanel, 1, 0);

            // ── Search Bar ───────────────────────────────────────────
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ThemeHelper.BackgroundLight
            };
            gridPanel.Controls.Add(searchPanel);

            var lblSearch = new Label { Text = "Search:", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(0, 12) };
            searchPanel.Controls.Add(lblSearch);

            txtSearch = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(300, 28), Location = new Point(60, 10), BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => RefreshGrid();
            searchPanel.Controls.Add(txtSearch);

            // ── Branches Grid ─────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(20, 50, 20, 20); // Add top padding for title

            var lblGrid = ThemeHelper.CreateSectionHeader("Branch List");
            lblGrid.Location = new Point(0, 15);
            cardGrid.Controls.Add(lblGrid);

            dgvBranches = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvBranches);
            dgvBranches.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBranches.ReadOnly = false;
            dgvBranches.CellClick += DgvBranches_CellClick;
            dgvBranches.CellValueChanged += DgvBranches_CellValueChanged;
            dgvBranches.CellFormatting += DgvBranches_CellFormatting;
            cardGrid.Controls.Add(dgvBranches);

            gridPanel.Controls.Add(cardGrid);
        }

        private void RefreshGrid()
        {
            try
            {
                var rows = _branchController.GetAllBranches(txtSearch.Text);
                dgvBranches.DataSource = null;
                dgvBranches.DataSource = rows;

                // Hide Id column
                if (dgvBranches.Columns.Contains("Id"))
                    dgvBranches.Columns["Id"].Visible = false;

                // Replace Status column with ComboBox column
                if (dgvBranches.Columns.Contains("Status"))
                {
                    var statusColumn = new DataGridViewComboBoxColumn
                    {
                        Name = "Status",
                        HeaderText = "Status",
                        DataPropertyName = "Status",
                        Items = { "Active", "Inactive" },
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                        FillWeight = 80
                    };
                    var originalIndex = dgvBranches.Columns["Status"].Index;
                    dgvBranches.Columns.Remove(dgvBranches.Columns["Status"]);
                    dgvBranches.Columns.Insert(originalIndex, statusColumn);
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBranchName.Text) || string.IsNullOrWhiteSpace(txtBranchCode.Text))
            {
                MessageBox.Show("Branch Name and Branch Code are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var branch = new Branch
                {
                    BranchName = txtBranchName.Text.Trim(),
                    BranchCode = txtBranchCode.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    City = txtCity.Text.Trim(),
                    State = txtState.Text.Trim(),
                    Country = txtCountry.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    IsActive = chkIsActive.Checked
                };

                if (_editingId.HasValue)
                {
                    branch.Id = _editingId.Value;
                    _branchController.UpdateBranch(branch);
                }
                else
                {
                    _branchController.CreateBranch(branch);
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Branch saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditCurrentBranch()
        {
            if (_editingId.HasValue)
            {
                btnSave.Text = "Update";
            }
        }

        private void DeleteCurrentBranch()
        {
            if (_editingId.HasValue)
            {
                if (MessageBox.Show("Are you sure you want to delete this branch?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _branchController.DeleteBranch(_editingId.Value);
                        ClearForm();
                        RefreshGrid();
                        MessageBox.Show("Branch deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
                }
            }
        }

        private void DgvBranches_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvBranches.Rows[e.RowIndex].Cells["Id"].Value;
            LoadBranchIntoForm(id);
        }

        private void DgvBranches_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvBranches.Columns[e.ColumnIndex].Name == "Status")
            {
                int id = (int)dgvBranches.Rows[e.RowIndex].Cells["Id"].Value;
                string newStatus = dgvBranches.Rows[e.RowIndex].Cells["Status"].Value?.ToString();
                bool isActive = newStatus == "Active";

                try
                {
                    _branchController.UpdateBranchStatus(id, isActive);
                }
                catch (Exception ex) { MessageBox.Show("Error updating status: " + ex.Message); }
            }
        }

        private void DgvBranches_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvBranches.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                if (e.Value.ToString() == "Active")
                {
                    e.CellStyle.BackColor = Color.FromArgb(227, 252, 227);
                    e.CellStyle.ForeColor = Color.FromArgb(34, 139, 34);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                else if (e.Value.ToString() == "Inactive")
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 235, 238);
                    e.CellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }
        }

        private void LoadBranchIntoForm(int id)
        {
            try
            {
                var branch = _branchController.GetBranchById(id);
                if (branch != null)
                {
                    _editingId = branch.Id;
                    txtBranchName.Text = branch.BranchName;
                    txtBranchCode.Text = branch.BranchCode;
                    txtAddress.Text = branch.Address;
                    txtCity.Text = branch.City;
                    txtState.Text = branch.State;
                    txtCountry.Text = branch.Country;
                    txtPhone.Text = branch.Phone;
                    txtEmail.Text = branch.Email;
                    chkIsActive.Checked = branch.IsActive;

                    // Enable Edit and Delete buttons
                    btnEdit.Enabled = true;
                    btnDelete.Enabled = true;
                    btnSave.Text = "Update";
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ClearForm()
        {
            _editingId = null;
            txtBranchName.Text = txtBranchCode.Text = txtAddress.Text = txtCity.Text = txtState.Text = txtCountry.Text = txtPhone.Text = txtEmail.Text = "";
            txtCountry.Text = "Philippines";
            chkIsActive.Checked = true;

            // Disable Edit and Delete buttons
            btnEdit.Enabled = false;
            btnDelete.Enabled = false;
            btnSave.Text = "Add";
        }
    }
}
