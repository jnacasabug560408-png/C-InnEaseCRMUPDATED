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
    /// Super Admin: Manage Users Account — user management and activity tracking.
    /// </summary>
    public class ViewSystemAdmin : UserControl
    {
        private DataGridView dgvUsers;
        private DataGridView dgvUserActivities;
        private TextBox txtUsername, txtFullName, txtEmail;
        private ComboBox cmbRole;
        private CheckBox chkIsActive;
        private TabControl tabControl;
        private Panel scrollableContent;
        private Panel cardForm;
        private Panel cardGrid;
        private Panel usersFormPanel;
        private int? _editingUserId = null;

        public ViewSystemAdmin(User currentUser) { Init(); }
        public ViewSystemAdmin() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.Resize += (s, e) => UpdateLayout();
            BuildUI();
            RefreshUsersGrid();
            RefreshActivitiesGrid();
        }

        private void UpdateLayout()
        {
            if (scrollableContent != null)
            {
                scrollableContent.Width = this.Width;
                scrollableContent.Height = this.Height;
            }
            if (tabControl != null)
            {
                tabControl.Width = scrollableContent.Width - 40;
                tabControl.Height = scrollableContent.Height - 120;
                if (usersFormPanel != null) usersFormPanel.Width = tabControl.Width - 20;
                if (cardForm != null) cardForm.Width = tabControl.Width - 20;
                if (cardGrid != null) cardGrid.Width = tabControl.Width - 20;
            }
        }

        private void BuildUI()
        {
            // ── Scrollable Content Panel ───────────────────────────────
            scrollableContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ThemeHelper.BackgroundLight,
                Padding = new Padding(20, 20, 20, 20)
            };
            this.Controls.Add(scrollableContent);

            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Manage Users Account",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            scrollableContent.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Manage user accounts and monitor user activity",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 35)
            };
            scrollableContent.Controls.Add(lblSubtitle);

            // ── Tab Control ─────────────────────────────────────────
            tabControl = new TabControl
            {
                Location = new Point(0, 70),
                Size = new Size(scrollableContent.Width - 40, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // ── Users Tab ───────────────────────────────────────────
            var tabUsers = new TabPage("User Accounts");
            tabUsers.BackColor = ThemeHelper.BackgroundLight;

            usersFormPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(tabControl.Width - 20, 150),
                BackColor = ThemeHelper.BackgroundLight,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 0);
            cardForm.Size = new Size(tabControl.Width - 20, 140);
            cardForm.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("User Details");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;

            // Username
            var lblUsername = new Label { Text = "Username", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblUsername);
            txtUsername = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(150, 28), Location = new Point(100, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtUsername);

            // Full Name
            var lblFullName = new Label { Text = "Full Name", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(270, y + 4) };
            cardForm.Controls.Add(lblFullName);
            txtFullName = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(200, 28), Location = new Point(350, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtFullName);

            // Email
            var lblEmail = new Label { Text = "Email", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(570, y + 4) };
            cardForm.Controls.Add(lblEmail);
            txtEmail = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(200, 28), Location = new Point(620, y), BorderStyle = BorderStyle.FixedSingle };
            cardForm.Controls.Add(txtEmail);
            y += 40;

            // Role
            var lblRole = new Label { Text = "Role", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblRole);
            cmbRole = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(150, 28), Location = new Point(100, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new[] { "SuperAdmin", "Admin", "Manager", "Staff" });
            if (cmbRole.Items.Count > 0) cmbRole.SelectedIndex = 0;
            cardForm.Controls.Add(cmbRole);

            // Active
            chkIsActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, AutoSize = true, Location = new Point(270, y), Checked = true };
            cardForm.Controls.Add(chkIsActive);

            // Buttons
            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 100, 38);
            btnSave.Location = new Point(570, y - 2);
            btnSave.Click += BtnSaveUser_Click;
            cardForm.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(680, y - 2);
            btnClear.Click += (s, e) => ClearUserForm();
            cardForm.Controls.Add(btnClear);

            usersFormPanel.Controls.Add(cardForm);
            tabUsers.Controls.Add(usersFormPanel);

            // Users Grid
            cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(0, 155);
            cardGrid.Size = new Size(usersFormPanel.Width, 300);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvUsers = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvUsers);
            dgvUsers.CellClick += DgvUsers_CellClick;
            dgvUsers.CellContentClick += DgvUsers_CellContentClick;
            cardGrid.Controls.Add(dgvUsers);

            var lblGrid = ThemeHelper.CreateSectionHeader("User List");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            tabUsers.Controls.Add(cardGrid);
            tabControl.TabPages.Add(tabUsers);

            // ── Activity Log Tab ─────────────────────────────────────
            var tabActivity = new TabPage("Activity Log");
            tabActivity.BackColor = ThemeHelper.BackgroundLight;

            var cardActivity = ThemeHelper.CreateCardPanel();
            cardActivity.Location = new Point(0, 0);
            cardActivity.Size = new Size(tabControl.Width - 20, tabControl.Height - 40);
            cardActivity.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvUserActivities = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvUserActivities);
            cardActivity.Controls.Add(dgvUserActivities);

            var lblActivity = ThemeHelper.CreateSectionHeader("User Activity Log");
            lblActivity.Dock = DockStyle.Top;
            cardActivity.Controls.Add(lblActivity);

            tabActivity.Controls.Add(cardActivity);
            tabActivity.Enter += (s, e) => RefreshActivitiesGrid();
            tabControl.TabPages.Add(tabActivity);

            scrollableContent.Controls.Add(tabControl);
        }

        private void RefreshUsersGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Username, FullName, Email, Role, IsActive, CreatedDate FROM Users ORDER BY FullName", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Id          = reader.GetInt32(0),
                            Username    = reader.GetString(1),
                            FullName    = reader.GetString(2),
                            Email       = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Role        = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Status      = reader.GetBoolean(5) ? "Active" : "Inactive",
                            CreatedDate = reader.GetDateTime(6).ToString("MMM dd, yyyy")
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvUsers.DataSource = null;
            dgvUsers.DataSource = rows;

            if (!dgvUsers.Columns.Contains("Edit"))
                dgvUsers.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "Edit", UseColumnTextForButtonValue = true, Width = 60 });
            if (!dgvUsers.Columns.Contains("Delete"))
                dgvUsers.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 70 });
        }

        private void RefreshActivitiesGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT TOP 200 Id, UserName, Action, Details, Module, ActivityDate FROM UserActivities ORDER BY ActivityDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Id      = reader.GetInt32(0),
                            User    = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Action  = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Details = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Module  = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Date    = reader.GetDateTime(5).ToString("MMM dd, yyyy HH:mm")
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvUserActivities.DataSource = null;
            dgvUserActivities.DataSource = rows;
        }

        private void BtnSaveUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Username and Full Name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserRole role;
            if (!Enum.TryParse(cmbRole.SelectedItem.ToString(), out role))
            {
                role = UserRole.Staff;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    if (_editingUserId.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE Users SET Username=@un, FullName=@fn, Email=@em, Role=@r, IsActive=@ia WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@un", txtUsername.Text.Trim());
                            cmd.Parameters.AddWithValue("@fn", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                            cmd.Parameters.AddWithValue("@r",  role.ToString());
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            cmd.Parameters.AddWithValue("@id", _editingUserId.Value);
                            cmd.ExecuteNonQuery();
                        }
                        LogActivity(_editingUserId.Value, txtFullName.Text.Trim(), "Update", $"Updated user: {txtUsername.Text.Trim()}", "User Management", conn);
                    }
                    else
                    {
                        int newId = 0;
                        using (var cmd = new SqlCommand("INSERT INTO Users (Username, FullName, Email, Password, Role, IsActive) OUTPUT INSERTED.Id VALUES (@un, @fn, @em, 'Temp123', @r, @ia)", conn))
                        {
                            cmd.Parameters.AddWithValue("@un", txtUsername.Text.Trim());
                            cmd.Parameters.AddWithValue("@fn", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                            cmd.Parameters.AddWithValue("@r",  role.ToString());
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            newId = (int)cmd.ExecuteScalar();
                        }
                        LogActivity(newId, txtFullName.Text.Trim(), "Create", $"Created user: {txtUsername.Text.Trim()}", "User Management", conn);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearUserForm();
            RefreshUsersGrid();
            RefreshActivitiesGrid();
            MessageBox.Show("User saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DgvUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvUsers.Rows[e.RowIndex].Cells["Id"].Value;
            LoadUserIntoForm(id);
        }

        private void LoadUserIntoForm(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Username, FullName, Email, Role, IsActive FROM Users WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _editingUserId       = reader.GetInt32(0);
                            txtUsername.Text     = reader.GetString(1);
                            txtFullName.Text     = reader.GetString(2);
                            txtEmail.Text        = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string roleStr       = reader.IsDBNull(4) ? "Staff" : reader.GetString(4);
                            if (cmbRole.Items.Contains(roleStr)) cmbRole.SelectedItem = roleStr;
                            chkIsActive.Checked  = reader.GetBoolean(5);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void DgvUsers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvUsers.Columns[e.ColumnIndex].Name == "Edit")
            {
                int id = (int)dgvUsers.Rows[e.RowIndex].Cells["Id"].Value;
                LoadUserIntoForm(id);
            }
            else if (dgvUsers.Columns[e.ColumnIndex].Name == "Delete")
            {
                int id = (int)dgvUsers.Rows[e.RowIndex].Cells["Id"].Value;
                if (MessageBox.Show("Are you sure you want to delete this user?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    {
                        string uname = "";
                        string fname = "";
                        using (var cmd = new SqlCommand("SELECT Username, FullName FROM Users WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read()) { uname = r.GetString(0); fname = r.GetString(1); }
                            }
                        }
                        using (var cmd = new SqlCommand("DELETE FROM Users WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                        LogActivity(id, fname, "Delete", $"Deleted user: {uname}", "User Management", conn);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

                RefreshUsersGrid();
                ClearUserForm();
                RefreshActivitiesGrid();
                MessageBox.Show("User deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearUserForm()
        {
            _editingUserId = null;
            txtUsername.Text = txtFullName.Text = txtEmail.Text = "";
            if (cmbRole.Items.Count > 0) cmbRole.SelectedIndex = 0;
            chkIsActive.Checked = true;
        }

        private void LogActivity(int userId, string userName, string action, string details, string module, SqlConnection conn)
        {
            try
            {
                using (var cmd = new SqlCommand("INSERT INTO UserActivities (UserId, UserName, Action, Details, Module) VALUES (@uid, @un, @a, @d, @m)", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@un",  userName);
                    cmd.Parameters.AddWithValue("@a",   action);
                    cmd.Parameters.AddWithValue("@d",   details);
                    cmd.Parameters.AddWithValue("@m",   module);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}
