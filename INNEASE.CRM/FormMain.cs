using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;
using INNEASE.CRM.ViewForms;

namespace INNEASE.CRM
{
    /// <summary>
    /// Main application shell with a collapsible pink-themed sidebar and a content panel
    /// that swaps UserControls based on the selected navigation item.
    /// Navigation items are dynamically built according to the logged-in user's role.
    /// </summary>
    public class FormMain : Form
    {
        // Singleton-style reference so child controls can trigger navigation
        public static FormMain Instance { get; private set; }

        private readonly User _currentUser;

        // Layout panels
        private Panel panelSidebar;
        private Panel panelSidebarMenu;
        private Panel panelTopBar;
        private Panel panelContent;
        private Panel panelSidebarHeader;

        // Sidebar state
        private bool _sidebarCollapsed = false;
        private const int SidebarExpandedWidth = 250;
        private const int SidebarCollapsedWidth = 0;  // Fully hides

        // Track active button
        private Button _activeButton;

        public FormMain(User user)
        {
            Instance = this;
            _currentUser = user;
            InitializeMainForm();
            BuildSidebarMenu();
            this.FormClosing += FormMain_FormClosing;
        }

        /// <summary>
        /// Navigates the content panel to the specified UserControl type
        /// and highlights the matching sidebar button.
        /// </summary>
        public void NavigateTo(Type controlType)
        {
            LoadContent(controlType);

            // Sync sidebar highlight to the matching button
            foreach (Control ctrl in panelSidebarMenu.Controls)
            {
                if (ctrl is Button btn && btn.Tag is MenuItem mi && mi.ControlType == controlType)
                {
                    ThemeHelper.SetActiveSidebarButton(btn, panelSidebarMenu);
                    _activeButton = btn;
                    break;
                }
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If closing due to logout button, just close (will return to login)
            if (this.DialogResult == DialogResult.OK)
            {
                return;
            }

            // If user clicked X button, ask what they want to do
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "Do you want to logout or exit the application?",
                    "Closing Dashboard",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button3);

                if (result == DialogResult.Yes)
                {
                    // Logout - will return to login screen
                    this.DialogResult = DialogResult.OK;
                }
                else if (result == DialogResult.No)
                {
                    // Exit application - don't return to login
                    this.DialogResult = DialogResult.Cancel;
                }
                else // Cancel
                {
                    e.Cancel = true;
                }
            }
        }

        private void InitializeMainForm()
        {
            // ── Form Properties ──────────────────────────────────────
            this.Text = $"InnEase HOTEL CRM — {_currentUser.Role} Dashboard";
            this.Size = new Size(1280, 780);
            this.MinimumSize = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ThemeHelper.BackgroundLight;
            this.WindowState = FormWindowState.Maximized;

            // ── Sidebar ──────────────────────────────────────────────
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = SidebarExpandedWidth,
                BackColor = ThemeHelper.SidebarDark,
            };

            // Sidebar header with property name
            panelSidebarHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(25, 0, 18),
                Padding = new Padding(20, 15, 15, 15)
            };
            panelSidebarHeader.Paint += (s, e) =>
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // Property name
                using (var nameFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold))
                {
                    e.Graphics.DrawString("The Aster Hotel", nameFont, Brushes.White, 20, 18);
                }
            };
            panelSidebar.Controls.Add(panelSidebarHeader);

            // Sidebar scrollable menu area — AutoScroll lets users scroll to see all menu items
            panelSidebarMenu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.SidebarDark,
                AutoScroll = true,
                AutoScrollMinSize = new Size(0, 500),
                Padding = new Padding(0, 8, 17, 0)
            };
            panelSidebar.Controls.Add(panelSidebarMenu);

            this.Controls.Add(panelSidebar);

            // ── Top Bar ──────────────────────────────────────────────
            panelTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            panelTopBar.Paint += (s, e) =>
            {
                // Bottom border
                using (var pen = new Pen(ThemeHelper.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, panelTopBar.Height - 1, panelTopBar.Width, panelTopBar.Height - 1);
                }
            };

            // Hamburger / Toggle button
            var btnToggle = new Button
            {
                Text = "☰",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 64),
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI", 18F),
                ForeColor = ThemeHelper.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.FlatAppearance.MouseOverBackColor = ThemeHelper.BackgroundLight;
            btnToggle.Click += (s, e) => ToggleSidebar();
            panelTopBar.Controls.Add(btnToggle);

            // User info (right side)
            var panelUserInfo = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.Transparent
            };

            // Logout button
            var btnLogout = new Button
            {
                Text = "Logout",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(75, 36),
                Font = ThemeHelper.FontSmall,
                ForeColor = ThemeHelper.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Location = new Point(10, 14),
            };
            btnLogout.FlatAppearance.BorderColor = ThemeHelper.CardBorder;
            btnLogout.FlatAppearance.BorderSize = 1;
            btnLogout.FlatAppearance.MouseOverBackColor = ThemeHelper.BackgroundLight;
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to logout?", "Logout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.OK; // Mark as logout action
                    this.Close();
                }
            };
            panelUserInfo.Controls.Add(btnLogout);

            // Notification bell
            var btnNotification = new Button
            {
                Text = "🔔",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(95, 12),
                Font = new Font("Segoe UI", 16F),
                ForeColor = ThemeHelper.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnNotification.FlatAppearance.BorderSize = 0;
            btnNotification.FlatAppearance.MouseOverBackColor = ThemeHelper.BackgroundLight;
            panelUserInfo.Controls.Add(btnNotification);

            // User avatar and name
            var panelUser = new Panel
            {
                Size = new Size(200, 40),
                Location = new Point(145, 12),
                BackColor = Color.Transparent
            };

            var avatarCircle = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(0, 0),
                BackColor = ThemeHelper.Primary
            };
            avatarCircle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(ThemeHelper.Primary))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, 40, 40);
                }

                string initials = "";
                var parts = _currentUser.FullName.Split(' ');
                if (parts.Length >= 2) initials = parts[0][0].ToString() + parts[1][0].ToString();
                else if (parts.Length == 1) initials = parts[0][0].ToString();

                using (var font = new Font("Segoe UI", 14F, FontStyle.Bold))
                {
                    var size = e.Graphics.MeasureString(initials, font);
                    e.Graphics.DrawString(initials, font, Brushes.White,
                        20 - size.Width / 2, 20 - size.Height / 2);
                }
            };
            panelUser.Controls.Add(avatarCircle);

            var lblUserName = new Label
            {
                Text = _currentUser.FullName + ", " + _currentUser.Role,
                Font = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(50, 12),
            };
            panelUser.Controls.Add(lblUserName);

            panelUserInfo.Controls.Add(panelUser);

            panelTopBar.Controls.Add(panelUserInfo);
            this.Controls.Add(panelTopBar);

            // ── Content Area ─────────────────────────────────────────
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeHelper.BackgroundLight,
                Padding = new Padding(0),
                Margin = new Padding(0),
            };

            // Z-order: Controls added last dock first. Order matters.
            this.Controls.Add(panelContent);    // Fill - must be added FIRST so it takes remaining space
            this.Controls.Add(panelSidebar);    // Left dock - clips Fill from left
            this.Controls.Add(panelTopBar);     // Top dock - clips Fill from top
        }

        // ── Build Sidebar Menu Based on Role ─────────────────────────

        private void BuildSidebarMenu()
        {
            panelSidebarMenu.Controls.Clear();
            var menuItems = GetMenuItemsForRole(_currentUser.Role);

            // Add buttons to sidebar (standard forward loop naturally stacks Top-docked items top-to-bottom)
            foreach (var item in menuItems)
            {
                Control ctrl;

                if (item.IsSeparator)
                {
                    ctrl = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 1,
                        BackColor = Color.FromArgb(60, 255, 255, 255),
                        Margin = new Padding(20, 8, 20, 8)
                    };
                }
                else
                {
                    var btn = ThemeHelper.CreateSidebarButton(item.Text, item.Icon);
                    btn.Tag = item;
                    btn.Click += SidebarButton_Click;
                    ctrl = btn;
                }
                
                panelSidebarMenu.Controls.Add(ctrl);
                ctrl.BringToFront(); // Ensures this latest item is pushed beneath the previously added ones in DockStyle.Top
            }

            // Auto-select first non-separator
            foreach (Control ctrl in panelSidebarMenu.Controls)
            {
                if (ctrl is Button btn && btn.Tag is MenuItem)
                {
                    btn.PerformClick();
                    break;
                }
            }
        }

        private void SidebarButton_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var menuItem = btn.Tag as MenuItem;
            if (menuItem == null) return;

            // Highlight active
            ThemeHelper.SetActiveSidebarButton(btn, panelSidebarMenu);
            _activeButton = btn;

            // Swap content
            LoadContent(menuItem.ControlType);
        }

        private void LoadContent(Type controlType)
        {
            panelContent.Controls.Clear();

            if (controlType != null)
            {
                UserControl uc;
                // Pass current user to controls that need it
                try
                {
                    uc = (UserControl)Activator.CreateInstance(controlType, _currentUser);
                }
                catch
                {
                    uc = (UserControl)Activator.CreateInstance(controlType);
                }

                uc.Dock = DockStyle.Fill;
                panelContent.Controls.Add(uc);
            }
        }

        // ── Toggle Sidebar ───────────────────────────────────────────

        private void ToggleSidebar()
        {
            _sidebarCollapsed = !_sidebarCollapsed;

            if (_sidebarCollapsed)
            {
                panelSidebar.Width = SidebarCollapsedWidth;
                panelSidebar.Visible = false;
            }
            else
            {
                panelSidebar.Visible = true;
                panelSidebar.Width = SidebarExpandedWidth;
            }
        }

        // ── Menu Configuration per Role ──────────────────────────────

        private List<MenuItem> GetMenuItemsForRole(UserRole role)
        {
            var items = new List<MenuItem>();

            switch (role)
            {
                case UserRole.SuperAdmin:
                    // Super Admin only has access to system management
                    items.Add(new MenuItem("Dashboard", "🏠", typeof(ViewDashboard)));
                    items.Add(MenuItem.Separator("System Management"));
                    items.Add(new MenuItem("Manage Tenants", "🏢", typeof(ViewManageTenants)));
                    items.Add(new MenuItem("Manage Billing", "💳", typeof(ViewManageBilling)));
                    items.Add(new MenuItem("System Administration", "⚙️", typeof(ViewSystemAdmin)));
                    break;

                case UserRole.Admin:
                    items.Add(new MenuItem("Dashboard",         "🏠", typeof(ViewDashboard)));
                    items.Add(MenuItem.Separator("Guest Management"));
                    items.Add(new MenuItem("Guests",            "👥", typeof(ViewManageGuests)));
                    items.Add(new MenuItem("Reservations",      "📅", typeof(ViewReservations)));
                    items.Add(MenuItem.Separator("Reports"));
                    items.Add(new MenuItem("Reports",           "📊", typeof(ViewCustomerReports)));
                    items.Add(MenuItem.Separator("Operations"));
                    items.Add(new MenuItem("Manage Staff",      "👤", typeof(ViewManageStaff)));
                    items.Add(new MenuItem("Manage Branches",   "🏢", typeof(ViewManageBranches)));
                    items.Add(new MenuItem("Manage Supplies",   "📦", typeof(ViewManageSupplies)));
                    items.Add(new MenuItem("Approve Requests",  "✅", typeof(ViewApproveSupplyRequests)));
                    items.Add(MenuItem.Separator("Marketing"));
                    items.Add(new MenuItem("Promotions Loyalty","🎁", typeof(ViewPromotions)));
                    items.Add(MenuItem.Separator("Settings"));
                    items.Add(new MenuItem("Settings",          "⚙️", typeof(ViewSystemAdmin)));
                    break;
                case UserRole.Manager:
                    // Manager modules
                    items.Add(new MenuItem("Dashboard", "🏠", typeof(ViewDashboard)));
                    items.Add(new MenuItem("Guests", "👥", typeof(ViewManageGuests)));
                    items.Add(new MenuItem("Reservations", "📅", typeof(ViewReservations)));
                    items.Add(new MenuItem("KPI DASHBOARD", "📊", typeof(ViewCustomerReports)));
                    items.Add(MenuItem.Separator("Operations"));
                    items.Add(new MenuItem("Manage Supplies", "📦", typeof(ViewManageSupplies)));
                    items.Add(new MenuItem("Approve Requests", "✅", typeof(ViewApproveSupplyRequests)));
                    items.Add(new MenuItem("Feedback & Complaints", "📝", typeof(ViewFeedbackComplaints)));
                    break;

                case UserRole.Staff:
                    // Staff has access to guest interactions
                    items.Add(new MenuItem("Dashboard", "🏠", typeof(ViewDashboard)));
                    items.Add(new MenuItem("Guests", "👥", typeof(ViewManageGuests)));
                    items.Add(new MenuItem("Reservations", "📅", typeof(ViewReservations)));
                    items.Add(MenuItem.Separator("Operations"));
                    items.Add(new MenuItem("Guest Interactions", "🤝", typeof(ViewGuestInteractions)));
                    items.Add(new MenuItem("Guest Requests", "🔔", typeof(ViewGuestRequests)));
                    items.Add(new MenuItem("Feedback & Complaints", "📝", typeof(ViewFeedbackComplaints)));
                    items.Add(MenuItem.Separator("Inventory"));
                    items.Add(new MenuItem("Request Supplies", "📦", typeof(ViewRequestSupplies)));
                    break;
            }

            return items;
        }

        /// <summary>
        /// Represents a sidebar navigation menu item.
        /// </summary>
        private class MenuItem
        {
            public string Text { get; set; }
            public string Icon { get; set; }
            public Type ControlType { get; set; }
            public bool IsSeparator { get; set; }

            public MenuItem(string text, string icon, Type controlType)
            {
                Text = text;
                Icon = icon;
                ControlType = controlType;
                IsSeparator = false;
            }

            private MenuItem() { IsSeparator = true; }

            public static MenuItem Separator(string label)
            {
                return new MenuItem { Text = label, IsSeparator = true };
            }
        }
    }
}

