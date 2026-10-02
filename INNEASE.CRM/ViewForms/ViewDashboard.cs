using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Dashboard overview with KPI cards showing key metrics across the system.
    /// Displayed as the landing page for all roles.
    /// </summary>
    public class ViewDashboard : UserControl
    {
        private readonly User _currentUser;
        private Panel scrollableContent;

        public ViewDashboard(User currentUser)
        {
            _currentUser = currentUser;
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.Resize += (s, e) => UpdateLayout();
            BuildDashboard();
        }

        private void UpdateLayout()
        {
            if (scrollableContent != null)
            {
                scrollableContent.Width = this.Width;
                scrollableContent.Height = this.Height;
            }
        }

        private void BuildDashboard()
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
                Text = "Dashboard",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            scrollableContent.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Overview of your hotel's performance",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 35)
            };
            scrollableContent.Controls.Add(lblSubtitle);

            // ── Top KPI Cards Row ────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(scrollableContent.Width - 40, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            kpiPanel.Resize += (s, e) =>
            {
                if (kpiPanel.Controls.Count > 0)
                {
                    var cardWidth = (kpiPanel.Width - 30) / 4;
                    foreach (Control ctrl in kpiPanel.Controls)
                    {
                        ctrl.Width = cardWidth;
                    }
                }
            };

            int totalGuests = 0;
            int activeReservations = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests", conn))
                        totalGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE Status IN ('Confirmed', 'CheckedIn')", conn))
                        activeReservations = (int)cmd.ExecuteScalar();
                }
            }
            catch { }
            
            double occupancyRate = activeReservations > 0 ? 78.2 : 0;
            double avgDailyRate = 246.0;

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Customer retention", "86.4%", ThemeHelper.Primary, 200, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Booking metrics", activeReservations.ToString(), ThemeHelper.Info, 200, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Occupancy rate", occupancyRate.ToString("0.0") + "%", ThemeHelper.Success, 200, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Avg. daily rate", "$" + avgDailyRate.ToString("0"), ThemeHelper.Accent, 200, 100));

            scrollableContent.Controls.Add(kpiPanel);

            // ── Booking Overview Card ────────────────────────────────
            var cardBooking = ThemeHelper.CreateCardPanel();
            cardBooking.Location = new Point(0, 195);
            cardBooking.Size = new Size(scrollableContent.Width - 40, 280);
            cardBooking.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblBooking = ThemeHelper.CreateSectionHeader("Booking overview");
            lblBooking.Location = new Point(20, 15);
            cardBooking.Controls.Add(lblBooking);

            // Simple chart visualization (placeholder)
            var chartPanel = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(cardBooking.Width - 40, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            chartPanel.Paint += (s, e) =>
            {
                // Draw simple bar chart
                var bars = new[] { 65, 80, 75, 90, 85, 95, 70 };
                var barWidth = 60;
                var spacing = 15;
                var startX = 20;
                var maxHeight = 150;
                var baseY = 180;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                for (int i = 0; i < bars.Length; i++)
                {
                    var barHeight = (bars[i] / 100.0f) * maxHeight;
                    var x = startX + i * (barWidth + spacing);
                    var y = baseY - barHeight;

                    // Bar
                    using (var brush = new SolidBrush(ThemeHelper.Primary))
                    {
                        e.Graphics.FillRectangle(brush, x, y, barWidth, barHeight);
                    }

                    // Day label
                    var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                    using (var font = new Font("Segoe UI", 8F))
                    using (var textBrush = new SolidBrush(ThemeHelper.TextMuted))
                    {
                        e.Graphics.DrawString(days[i], font, textBrush, x + barWidth / 2 - 10, baseY + 5);
                    }
                }
            };
            cardBooking.Controls.Add(chartPanel);

            scrollableContent.Controls.Add(cardBooking);

            // ── Recent Activity Card ──────────────────────────────────
            var cardActivity = ThemeHelper.CreateCardPanel();
            cardActivity.Location = new Point(0, 490);
            cardActivity.Size = new Size(scrollableContent.Width - 40, 280);
            cardActivity.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblActivity = ThemeHelper.CreateSectionHeader("Recent activity");
            lblActivity.Location = new Point(20, 15);
            cardActivity.Controls.Add(lblActivity);

            var activityList = new FlowLayoutPanel
            {
                Location = new Point(20, 55),
                Size = new Size(cardActivity.Width - 40, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var activities = new[]
            {
                "New reservation: John Smith",
                "Guest checked in: Room 201",
                "Payment received: $450",
                "Guest checked out: Room 115",
                "New booking: Sarah Johnson"
            };

            foreach (var activity in activities)
            {
                var item = new Label
                {
                    Text = "• " + activity,
                    Font = ThemeHelper.FontSmall,
                    ForeColor = ThemeHelper.TextMedium,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 8)
                };
                activityList.Controls.Add(item);
            }

            cardActivity.Controls.Add(activityList);
            scrollableContent.Controls.Add(cardActivity);

            // ── Bottom Cards Row ───────────────────────────────────────
            var bottomPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 785),
                Size = new Size(scrollableContent.Width - 40, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            bottomPanel.Resize += (s, e) =>
            {
                if (bottomPanel.Controls.Count > 0)
                {
                    var cardWidth = (bottomPanel.Width - 30) / 3;
                    foreach (Control ctrl in bottomPanel.Controls)
                    {
                        ctrl.Width = cardWidth;
                    }
                }
            };

            bottomPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Arrivals", "24", "7 pending", ThemeHelper.Primary, 200, 100));
            bottomPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Departures", "18", "", ThemeHelper.Info, 200, 100));
            bottomPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("In-house guests", "126", "", ThemeHelper.Success, 200, 100));

            scrollableContent.Controls.Add(bottomPanel);

            // ── Quick Actions ─────────────────────────────────────────
            var actionsPanel = new Panel
            {
                Location = new Point(0, 920),
                Size = new Size(scrollableContent.Width - 40, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblActions = new Label
            {
                Text = "Quick actions",
                Font = ThemeHelper.FontHeading,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            actionsPanel.Controls.Add(lblActions);

            var btnNewGuest = ThemeHelper.CreatePrimaryButton("New guest", 130, 36);
            btnNewGuest.Location = new Point(0, 30);
            actionsPanel.Controls.Add(btnNewGuest);

            var btnReserve = ThemeHelper.CreateSecondaryButton("Reserve", 130, 36);
            btnReserve.Location = new Point(140, 30);
            actionsPanel.Controls.Add(btnReserve);

            scrollableContent.Controls.Add(actionsPanel);
        }
    }
}
