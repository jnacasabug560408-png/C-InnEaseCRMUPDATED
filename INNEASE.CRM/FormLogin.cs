using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM
{
    /// <summary>
    /// Login form with a themed split layout: branded left panel and login form right panel.
    /// Supports manual credential entry (autofills superadmin for testing).
    /// </summary>
    public class FormLogin : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Label lblError;

        public FormLogin()
        {
            InitializeLoginForm();
        }

        private void InitializeLoginForm()
        {
            // ── Form Properties ──────────────────────────────────────
            this.Text = "InnEase CRM — Login";
            this.Size = new Size(900, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = ThemeHelper.BackgroundWhite;

            // ── Left Brand Panel ─────────────────────────────────────
            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = ThemeHelper.SidebarDark
            };
            panelLeft.Paint += (s, e) =>
            {
                // Gradient overlay
                using (var brush = new LinearGradientBrush(
                    panelLeft.ClientRectangle,
                    ThemeHelper.SidebarDark,
                    Color.FromArgb(136, 14, 79),
                    LinearGradientMode.ForwardDiagonal))
                {
                    e.Graphics.FillRectangle(brush, panelLeft.ClientRectangle);
                }

                // Brand text
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                using (var titleFont = new Font("Segoe UI", 32F, FontStyle.Bold))
                using (var subFont = new Font("Segoe UI", 12F, FontStyle.Regular))
                using (var descFont = new Font("Segoe UI", 9.5F, FontStyle.Regular))
                {
                    // Decorative icon circle
                    using (var circleBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        e.Graphics.FillEllipse(circleBrush, 130, 100, 120, 120);
                    }
                    // Hotel icon text
                    using (var iconFont = new Font("Segoe UI", 42F, FontStyle.Bold))
                    {
                        var iconSize = e.Graphics.MeasureString("IE", iconFont);
                        e.Graphics.DrawString("IE", iconFont, Brushes.White,
                            190 - iconSize.Width / 2, 128);
                    }

                    e.Graphics.DrawString("InnEase", titleFont, Brushes.White, 90, 240);

                    using (var softBrush = new SolidBrush(ThemeHelper.PrimaryLight))
                    {
                        e.Graphics.DrawString("CRM & Reservation System", subFont, softBrush, 85, 290);
                    }

                    // Divider line
                    using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                    {
                        e.Graphics.DrawLine(pen, 80, 330, 300, 330);
                    }

                    using (var mutedBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
                    {
                        e.Graphics.DrawString("Streamline your hotel management\nwith intelligent CRM solutions.", descFont, mutedBrush, 80, 345);
                    }

                    // Version tag
                    using (var versionFont = new Font("Segoe UI", 8F, FontStyle.Italic))
                    using (var vBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                    {
                        e.Graphics.DrawString("v1.0.0 — © 2026 InnEase", versionFont, vBrush, 110, 470);
                    }
                }
            };
            this.Controls.Add(panelLeft);

            // ── Right Login Panel ────────────────────────────────────
            var panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.BackgroundWhite,
                Padding = new Padding(50, 40, 50, 40)
            };
            this.Controls.Add(panelRight);
            panelRight.BringToFront(); // Ensures it fills the REMAINING space, not the whole form

            // Welcome label
            var lblWelcome = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(60, 50)
            };
            panelRight.Controls.Add(lblWelcome);

            var lblSub = new Label
            {
                Text = "Sign in to access your dashboard",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(60, 90)
            };
            panelRight.Controls.Add(lblSub);

            // Username
            var lblUser = new Label
            {
                Text = "Username",
                Font = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize = true,
                Location = new Point(60, 150)
            };
            panelRight.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Font = ThemeHelper.FontBody,
                Size = new Size(380, 34),
                Location = new Point(60, 175),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            panelRight.Controls.Add(txtUsername);

            // Password
            var lblPass = new Label
            {
                Text = "Password",
                Font = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize = true,
                Location = new Point(60, 220)
            };
            panelRight.Controls.Add(lblPass);

            txtPassword = new TextBox
            {
                Font = ThemeHelper.FontBody,
                Size = new Size(380, 34),
                Location = new Point(60, 245),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                UseSystemPasswordChar = true
            };
            panelRight.Controls.Add(txtPassword);

            // Error label
            lblError = new Label
            {
                Text = "",
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.Danger,
                AutoSize = true,
                Location = new Point(60, 285),
                Visible = false
            };
            panelRight.Controls.Add(lblError);

            // Login button
            var btnLogin = ThemeHelper.CreatePrimaryButton("Sign In", 380, 44);
            btnLogin.Location = new Point(60, 310);
            btnLogin.Click += BtnLogin_Click;
            panelRight.Controls.Add(btnLogin);

            // Accept Enter key
            this.AcceptButton = new InvisibleButton(BtnLogin_Click);
            
            // Autofill for testing
            txtUsername.Text = "superadmin";
            txtPassword.Text = "admin123";
            
            txtUsername.Focus();
        }

        // ── Event Handlers ───────────────────────────────────────────

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            User user = null;
            try
            {
                using (var conn = InnEase.Infrastructure.Data.DatabaseService.GetConnection())
                {
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT Id, Username, FullName, Email, Password, Role, IsActive, CreatedDate FROM Users WHERE Username = @u AND Password = @p AND IsActive = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", password);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    FullName = reader.GetString(2),
                                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Password = reader.GetString(4),
                                    Role = (UserRole)Enum.Parse(typeof(UserRole), reader.GetString(5)),
                                    IsActive = reader.GetBoolean(6),
                                    CreatedDate = reader.GetDateTime(7)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Database error: " + ex.Message);
                return;
            }

            if (user == null)
            {
                ShowError("Invalid credentials or account is inactive.");
                return;
            }

            OpenDashboard(user);
        }



        private void OpenDashboard(User user)
        {
            INNEASE.CRM.Helpers.AuditLogger.CurrentUser = user.FullName;
            INNEASE.CRM.Helpers.AuditLogger.LogAction("Login", "User logged in successfully", "Authentication");
            
            this.Hide();
            var main = new FormMain(user);
            main.FormClosed += (s, args) =>
            {
                // Only show login form again if it was a logout (DialogResult.OK)
                // If DialogResult is not OK, user chose to exit the application
                if (main.DialogResult == DialogResult.OK)
                {
                    INNEASE.CRM.Helpers.AuditLogger.LogAction(user.FullName, "Logout", "User logged out", "Authentication");
                    this.Show();
                    txtUsername.Text = "";
                    txtPassword.Text = "";
                    txtUsername.Focus();
                }
                else
                {
                    // User chose to exit, close login form too
                    this.Close();
                }
            };
            main.Show();
        }

        private void ShowError(string message)
        {
            lblError.Text = "⚠  " + message;
            lblError.Visible = true;
        }

        /// <summary>
        /// Invisible button used to wire AcceptButton for Enter key support.
        /// </summary>
        private class InvisibleButton : Button
        {
            public InvisibleButton(EventHandler clickHandler)
            {
                this.Size = new Size(0, 0);
                this.TabStop = false;
                this.Click += clickHandler;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FormLogin
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "FormLogin";
            this.Load += new System.EventHandler(this.FormLogin_Load);
            this.ResumeLayout(false);

        }

        private void FormLogin_Load(object sender, EventArgs e)
        {

        }
    }
}
