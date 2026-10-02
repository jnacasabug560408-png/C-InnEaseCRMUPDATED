using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Admin: CRUD form for managing staff accounts, credentials, and roles.
    /// </summary>
    public class ViewManageStaff : UserControl
    {
        private DataGridView dgvStaff;
        private TextBox txtFullName, txtUsername, txtPassword, txtEmail;
        private ComboBox cmbRole;
        private CheckBox chkActive;
        private Button btnAdd, btnEdit, btnClear, btnDelete;
        private int? _editingId = null;

        public ViewManageStaff(User currentUser) { Init(); }
        public ViewManageStaff() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            BuildUI();
            RefreshGrid();
            SetButtonStates();
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Header
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Content
            this.Controls.Add(mainPanel);

            // ── Header ───────────────────────────────────────────────
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(headerPanel, 0, 0);

            var lblTitle = new Label
            {
                Text = "Manage Staff Accounts",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(4, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            // ── Content Area (Split: Form on left, Grid on right) ───────
            var contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450));  // Form width
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Grid fills rest
            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Dock = DockStyle.Fill;
            cardForm.Padding = new Padding(12);

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Staff Details");
            lblFormTitle.Dock = DockStyle.Top;
            cardForm.Controls.Add(lblFormTitle);

            var formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));  // Labels
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Fields
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cardForm.Controls.Add(formPanel);

            // Full Name
            var lblName = new Label { Text = "Full Name", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            txtFullName = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblName, 0, 0);
            formPanel.Controls.Add(txtFullName, 1, 0);

            // Username
            var lblUser = new Label { Text = "Username", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            txtUsername = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblUser, 0, 1);
            formPanel.Controls.Add(txtUsername, 1, 1);

            // Password
            var lblPass = new Label { Text = "Password", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            txtPassword = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblPass, 0, 2);
            formPanel.Controls.Add(txtPassword, 1, 2);

            // Email
            var lblEmail = new Label { Text = "Email", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            txtEmail = new TextBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            formPanel.Controls.Add(lblEmail, 0, 3);
            formPanel.Controls.Add(txtEmail, 1, 3);

            // Role
            var lblRole = new Label { Text = "Role", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            cmbRole = new ComboBox { Font = ThemeHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new object[] { "Admin", "Manager", "Staff" }); // Removed SuperAdmin
            cmbRole.SelectedIndex = 2;
            formPanel.Controls.Add(lblRole, 0, 4);
            formPanel.Controls.Add(cmbRole, 1, 4);

            // Active checkbox
            chkActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, Checked = true, AutoSize = false, Dock = DockStyle.Fill };
            formPanel.Controls.Add(new Label(), 0, 5);  // Empty label cell
            formPanel.Controls.Add(chkActive, 1, 5);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };
            formPanel.Controls.Add(buttonPanel, 0, 6);
            formPanel.SetColumnSpan(buttonPanel, 2);

            // [ Add ] [ Edit ] [ Clear ] [ Delete ]
            btnAdd = ThemeHelper.CreatePrimaryButton("Add", 75, 38);
            btnAdd.Margin = new Padding(0, 0, 6, 0);
            btnAdd.Click += BtnAdd_Click;
            buttonPanel.Controls.Add(btnAdd);

            btnEdit = ThemeHelper.CreateSecondaryButton("Edit", 75, 38);
            btnEdit.Margin = new Padding(0, 0, 6, 0);
            btnEdit.Click += BtnEdit_Click;
            buttonPanel.Controls.Add(btnEdit);

            btnClear = ThemeHelper.CreateSecondaryButton("Clear", 75, 38);
            btnClear.Margin = new Padding(0, 0, 6, 0);
            btnClear.Click += (s, e) => ClearForm();
            buttonPanel.Controls.Add(btnClear);

            btnDelete = ThemeHelper.CreateDangerButton("Delete", 85, 38);
            btnDelete.Click += BtnDelete_Click;
            buttonPanel.Controls.Add(btnDelete);

            contentPanel.Controls.Add(cardForm, 0, 0);

            // ── Grid Card ────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(12);

            dgvStaff = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvStaff);
            dgvStaff.CellClick += DgvStaff_CellClick;
            dgvStaff.DataBindingComplete += DgvStaff_DataBindingComplete;
            cardGrid.Controls.Add(dgvStaff);

            var lblGridTitle = ThemeHelper.CreateSectionHeader("Staff List");
            lblGridTitle.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGridTitle);

            contentPanel.Controls.Add(cardGrid, 1, 0);
        }

        private void SetButtonStates()
        {
            if (_editingId.HasValue)
            {
                btnAdd.Enabled = false;
                btnAdd.BackColor = Color.LightGray;

                btnEdit.Enabled = true;
                btnEdit.BackColor = ThemeHelper.Primary; // Or an accent color if desired

                btnDelete.Enabled = true;
                btnDelete.BackColor = ThemeHelper.Danger;
            }
            else
            {
                btnAdd.Enabled = true;
                btnAdd.BackColor = ThemeHelper.Primary; // Vibrant Pink/Magenta

                btnEdit.Enabled = false;
                btnEdit.BackColor = Color.LightGray;

                btnDelete.Enabled = false;
                btnDelete.BackColor = Color.LightGray;
            }
        }

        private void RefreshGrid()
        {
            dgvStaff.DataSource = null;
            var usersList = new List<dynamic>();
            try
            {
                using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                {
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT Id, FullName, Username, Email, Role, IsActive FROM Users WHERE Role != 'SuperAdmin'", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usersList.Add(new
                            {
                                Id = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Username = reader.GetString(2),
                                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Role = reader.GetString(4),
                                Active = reader.GetBoolean(5) ? "Yes" : "No"
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading grid: " + ex.Message); }
            dgvStaff.DataSource = usersList;
        }

        private void DgvStaff_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Grid alignment polish
            foreach (DataGridViewColumn col in dgvStaff.Columns)
            {
                if (col.Name == "Id" || col.Name == "Active")
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                // Fix "Username" header color (use uniform Deep Plum)
                if (col.Name == "Username")
                {
                    col.HeaderCell.Style.BackColor = ThemeHelper.PrimaryDark;
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Full Name and Username are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserRole role = (UserRole)Enum.Parse(typeof(UserRole), cmbRole.SelectedItem.ToString());

            try
            {
                using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                {
                    using (var cmd = new System.Data.SqlClient.SqlCommand("INSERT INTO Users (Username, FullName, Email, Password, Role, IsActive, CreatedDate) VALUES (@u, @f, @e, @p, @r, @a, GETDATE())", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@f", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@e", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(txtPassword.Text) ? "password123" : txtPassword.Text);
                        cmd.Parameters.AddWithValue("@r", role.ToString());
                        cmd.Parameters.AddWithValue("@a", chkActive.Checked);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            INNEASE.CRM.Helpers.AuditLogger.LogAction("Create", $"Created staff account: {txtUsername.Text.Trim()}", "Manage Staff");
            MessageBox.Show("Staff account created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue) return;

            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Full Name and Username are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserRole role = (UserRole)Enum.Parse(typeof(UserRole), cmbRole.SelectedItem.ToString());

            try
            {
                using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                {
                    string query = "UPDATE Users SET FullName=@f, Username=@u, Email=@e, Role=@r, IsActive=@a" +
                        (!string.IsNullOrEmpty(txtPassword.Text) ? ", Password=@p" : "") +
                        " WHERE Id=@id";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingId.Value);
                        cmd.Parameters.AddWithValue("@u", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@f", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@e", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@r", role.ToString());
                        cmd.Parameters.AddWithValue("@a", chkActive.Checked);
                        if (!string.IsNullOrEmpty(txtPassword.Text))
                            cmd.Parameters.AddWithValue("@p", txtPassword.Text);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Updated staff account: {txtUsername.Text.Trim()}", "Manage Staff");
            MessageBox.Show("Staff account updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue) return;
            if (MessageBox.Show("Delete this staff account?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string delName = "Unknown";
                try
                {
                    using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                    {
                        using (var selCmd = new System.Data.SqlClient.SqlCommand("SELECT Username FROM Users WHERE Id=@id", conn))
                        {
                            selCmd.Parameters.AddWithValue("@id", _editingId.Value);
                            var res = selCmd.ExecuteScalar();
                            if (res != null) delName = res.ToString();
                        }
                        using (var cmd = new System.Data.SqlClient.SqlCommand("DELETE FROM Users WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Delete", $"Deleted staff account: {delName}", "Manage Staff");
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
                
                ClearForm();
                RefreshGrid();
            }
        }

        private void DgvStaff_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvStaff.Rows[e.RowIndex].Cells["Id"].Value;
            
            try
            {
                using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                {
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT FullName, Username, Email, Role, IsActive FROM Users WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _editingId = id;
                                txtFullName.Text = reader.GetString(0);
                                txtUsername.Text = reader.GetString(1);
                                txtPassword.Text = "";
                                txtEmail.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                
                                string roleStr = reader.GetString(3);
                                if (roleStr == "SuperAdmin")
                                    cmbRole.SelectedIndex = 0;
                                else
                                    cmbRole.SelectedItem = roleStr;
                                    
                                chkActive.Checked = reader.GetBoolean(4);
                                SetButtonStates();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading user: " + ex.Message); }
        }

        private void ClearForm()
        {
            _editingId = null;
            txtFullName.Text = txtUsername.Text = txtPassword.Text = txtEmail.Text = "";
            cmbRole.SelectedIndex = 2; // Staff default
            chkActive.Checked = true;
            
            SetButtonStates();
            txtFullName.Focus();
        }
    }
}
