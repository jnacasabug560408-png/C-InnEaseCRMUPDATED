using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace INNEASE.CRM.Helpers
{
    /// <summary>
    /// Centralized design system for the InnEase CRM application.
    /// Provides consistent colors, fonts, and styled control factory methods.
    /// </summary>
    public static class ThemeHelper
    {
        // ── Color Palette ────────────────────────────────────────────
        public static readonly Color Primary         = ColorTranslator.FromHtml("#E91E63");
        public static readonly Color PrimaryDark      = ColorTranslator.FromHtml("#C2185B");
        public static readonly Color PrimaryLight     = ColorTranslator.FromHtml("#F8BBD0");
        public static readonly Color Accent           = ColorTranslator.FromHtml("#FF4081");
        public static readonly Color AccentSoft       = ColorTranslator.FromHtml("#FF80AB");
        public static readonly Color BrightPink      = ColorTranslator.FromHtml("#FF69B4");

        public static readonly Color SidebarDark      = ColorTranslator.FromHtml("#2C001E");
        public static readonly Color SidebarHover     = ColorTranslator.FromHtml("#4A0033");
        public static readonly Color SidebarSelected  = ColorTranslator.FromHtml("#E91E63");

        public static readonly Color BackgroundLight  = ColorTranslator.FromHtml("#F5F5F5");
        public static readonly Color BackgroundWhite  = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color CardBackground   = Color.White;
        public static readonly Color CardBorder       = ColorTranslator.FromHtml("#E0E0E0");

        public static readonly Color TextDark         = ColorTranslator.FromHtml("#212121");
        public static readonly Color TextMedium       = ColorTranslator.FromHtml("#616161");
        public static readonly Color TextLight        = Color.White;
        public static readonly Color TextMuted        = ColorTranslator.FromHtml("#9E9E9E");

        public static readonly Color Success          = ColorTranslator.FromHtml("#4CAF50");
        public static readonly Color Warning          = ColorTranslator.FromHtml("#FF9800");
        public static readonly Color Danger           = ColorTranslator.FromHtml("#F44336");
        public static readonly Color Info             = ColorTranslator.FromHtml("#2196F3");

        public static readonly Color GridHeader       = ColorTranslator.FromHtml("#2C001E");
        public static readonly Color GridAltRow       = ColorTranslator.FromHtml("#FAFAFA");

        // ── Fonts ────────────────────────────────────────────────────
        public static readonly Font FontTitle         = new Font("Segoe UI", 20F, FontStyle.Bold);
        public static readonly Font FontSubtitle      = new Font("Segoe UI", 14F, FontStyle.Regular);
        public static readonly Font FontHeading        = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        public static readonly Font FontBody           = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontBodyBold       = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font FontSmall          = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font FontSidebar        = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        public static readonly Font FontSidebarBold    = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        public static readonly Font FontButton         = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        public static readonly Font FontKPI            = new Font("Segoe UI", 28F, FontStyle.Bold);

        // ── Sidebar Navigation Button ────────────────────────────────
        public static Button CreateSidebarButton(string text, string iconChar = "")
        {
            var btn = new Button
            {
                Text = string.IsNullOrEmpty(iconChar) ? "   " + text : "   " + iconChar + "  " + text,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarDark,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = FontSidebar,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 10, 0),
                Dock = DockStyle.Top,
                Height = 48,
                Cursor = Cursors.Hand,
                Tag = text
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = SidebarHover;
            btn.FlatAppearance.MouseDownBackColor = SidebarSelected;

            return btn;
        }

        /// <summary>
        /// Applies the "selected" highlight to a sidebar button and resets siblings.
        /// </summary>
        public static void SetActiveSidebarButton(Button activeBtn, Panel sidebarPanel)
        {
            foreach (Control ctrl in sidebarPanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = SidebarDark;
                    btn.ForeColor = Color.FromArgb(200, 200, 200);
                    btn.Font = FontSidebar;
                    btn.Padding = new Padding(20, 0, 10, 0);
                }
            }
            activeBtn.BackColor = Primary;
            activeBtn.ForeColor = TextLight;
            activeBtn.Font = FontSidebarBold;
            activeBtn.Padding = new Padding(24, 0, 10, 0); // Add left padding for active state
        }

        // ── Styled Primary Button ────────────────────────────────────
        public static Button CreatePrimaryButton(string text, int width = 140, int height = 40)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Primary,
                ForeColor = TextLight,
                Font = FontButton,
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = PrimaryDark;
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            btn.Resize += (s, e) =>
            {
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            };
            return btn;
        }

        // ── Styled Secondary / Outline Button ────────────────────────
        public static Button CreateSecondaryButton(string text, int width = 140, int height = 40)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBackground,
                ForeColor = Primary,
                Font = FontButton,
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = Primary;
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(252, 228, 236);
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            btn.Resize += (s, e) =>
            {
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            };
            return btn;
        }

        // ── Styled Danger Button ─────────────────────────────────────
        public static Button CreateDangerButton(string text, int width = 140, int height = 40)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Danger,
                ForeColor = TextLight,
                Font = FontButton,
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 47, 47);
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            btn.Resize += (s, e) =>
            {
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            };
            return btn;
        }

        // ── Styled Success Button ─────────────────────────────────────
        public static Button CreateSuccessButton(string text, int width = 140, int height = 40)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Success,
                ForeColor = TextLight,
                Font = FontButton,
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(67, 160, 71);
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            btn.Resize += (s, e) =>
            {
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            };
            return btn;
        }

        // ── KPI / Summary Card Panel ─────────────────────────────────
        public static Panel CreateKpiCard(string title, string value, Color accentColor, int width = 220, int height = 120)
        {
            var card = new Panel
            {
                Size = new Size(width, height),
                BackColor = CardBackground,
                Padding = new Padding(16),
                Margin = new Padding(8),
            };

            // Accent stripe on the left
            var stripe = new Panel
            {
                Size = new Size(4, height),
                BackColor = accentColor,
                Dock = DockStyle.Left
            };
            card.Controls.Add(stripe);

            var lblTitle = new Label
            {
                Text = title,
                Font = FontSmall,
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(24, 16)
            };
            card.Controls.Add(lblTitle);

            var lblValue = new Label
            {
                Text = value,
                Font = FontKPI,
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(24, 38)
            };
            card.Controls.Add(lblValue);

            // Paint shadow + border
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            return card;
        }

        // ── Modern KPI Card with Background Color ────────────────────
        public static Panel CreateModernKpiCard(string title, string value, string subtitle, Color bgColor, int width = 240, int height = 100)
        {
            var card = new Panel
            {
                Size = new Size(width, height),
                BackColor = bgColor,
                Padding = new Padding(16),
                Margin = new Padding(8),
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = FontSmall,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 12)
            };
            card.Controls.Add(lblTitle);

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 32)
            };
            card.Controls.Add(lblValue);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var lblSubtitle = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = Color.FromArgb(200, 255, 255, 255),
                    AutoSize = true,
                    Location = new Point(16, 62)
                };
                card.Controls.Add(lblSubtitle);
            }

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 0, 0, 0), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            return card;
        }

        // ── Card Container Panel (white rounded card with border) ────
        public static Panel CreateCardPanel()
        {
            var panel = new Panel
            {
                BackColor = CardBackground,
                Padding = new Padding(20),
                Margin = new Padding(8),
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            return panel;
        }

        // ── Styled TextBox ───────────────────────────────────────────
        public static TextBox CreateStyledTextBox(int width = 250)
        {
            var txt = new TextBox
            {
                Font = FontBody,
                Size = new Size(width, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
            };
            return txt;
        }

        // ── Styled DataGridView ──────────────────────────────────────
        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = CardBackground;
            dgv.GridColor = CardBorder;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBodyBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.DefaultCellStyle.Font = FontBody;
            dgv.DefaultCellStyle.ForeColor = TextDark;
            dgv.DefaultCellStyle.BackColor = CardBackground;
            dgv.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            dgv.DefaultCellStyle.SelectionForeColor = TextDark;
            dgv.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;

            dgv.RowTemplate.Height = 38;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgv.ScrollBars = ScrollBars.Both;
        }

        // ── Section Header Label ─────────────────────────────────────
        public static Label CreateSectionHeader(string text)
        {
            return new Label
            {
                Text = text,
                Font = FontHeading,
                ForeColor = PrimaryDark,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 8)
            };
        }

        // ── Search Bar TextBox ────────────────────────────────────────
        public static TextBox CreateSearchBar(int width = 300, string placeholder = "Search...")
        {
            var txt = new TextBox
            {
                Font = FontBody,
                Size = new Size(width, 36),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = placeholder,
                ForeColor = TextMuted
            };
            txt.GotFocus += (s, e) =>
            {
                if (txt.Text == placeholder)
                {
                    txt.Text = "";
                    txt.ForeColor = TextDark;
                }
            };
            txt.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    txt.Text = placeholder;
                    txt.ForeColor = TextMuted;
                }
            };
            return txt;
        }

        // ── Filter ComboBox ───────────────────────────────────────────
        public static ComboBox CreateFilterComboBox(string[] items, int width = 150)
        {
            var cmb = new ComboBox
            {
                Font = FontBody,
                Size = new Size(width, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextDark
            };
            cmb.Items.AddRange(items);
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            return cmb;
        }

        // ── Status Badge Label ───────────────────────────────────────
        public static Label CreateStatusBadge(string text, Color bgColor)
        {
            return new Label
            {
                Text = text,
                Font = FontSmall,
                ForeColor = Color.White,
                BackColor = bgColor,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        // ── Win32 for Rounded Corners ────────────────────────────────
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        // ── Format Abbreviated Value (e.g., $212K, $1.5M) ─────────────
        public static string FormatAbbreviated(decimal value)
        {
            if (value >= 1000000m)
                return (value / 1000000m).ToString("F1") + "M";
            if (value >= 1000m)
                return (value / 1000m).ToString("F0") + "K";
            return value.ToString("F0");
        }

        // ── Style DataGridView with Pink Accent Hover ───────────────
        public static void StyleDataGridViewWithAccent(DataGridView dgv)
        {
            StyleDataGridView(dgv);

            // Add pink accent bar on hover
            dgv.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count)
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 252);
                }
            };

            dgv.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count)
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = dgv.Rows[e.RowIndex].Index % 2 == 0
                        ? CardBackground
                        : GridAltRow;
                }
            };

            // Paint event for left accent bar on selected row
            dgv.RowPostPaint += (s, e) =>
            {
                if (dgv.Rows[e.RowIndex].Selected)
                {
                    var rect = new Rectangle(0, e.RowBounds.Top, 4, e.RowBounds.Height);
                    using (var brush = new SolidBrush(BrightPink))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
            };
        }
    }
}
