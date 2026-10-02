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
    /// Manager: Customer Analytics — data visualization mockup with summary grids.
    /// </summary>
    public class ViewCustomerAnalytics : UserControl
    {
        public ViewCustomerAnalytics(User currentUser) { Init(); }
        public ViewCustomerAnalytics() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            BuildUI();
        }

        private void BuildUI()
        {
            var lblTitle = new Label
            {
                Text = "Customer Analytics",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            this.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Data visualization and trends analysis",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(14, 48)
            };
            this.Controls.Add(lblSub);

            // ── KPI Row ──────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(8, 85),
                Size = new Size(960, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            double avgSatisfaction = 4.2;
            int totalFeedback = 0, compliments = 0, resolvedCount = 0;
            var catData = new List<dynamic>();

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets", conn))
                        totalFeedback = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Category='Compliment'", conn))
                        compliments = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM FeedbackTickets WHERE Status IN ('Resolved','Closed')", conn))
                        resolvedCount = (int)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand(@"
                        SELECT Category,
                               COUNT(*) AS Total,
                               SUM(CASE WHEN Status='Open' THEN 1 ELSE 0 END) AS Pending,
                               SUM(CASE WHEN Status IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS Resolved,
                               SUM(CASE WHEN Priority IN ('High','Critical') THEN 1 ELSE 0 END) AS HighPriority
                        FROM FeedbackTickets
                        GROUP BY Category
                        ORDER BY Total DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            catData.Add(new
                            {
                                Category    = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                Count       = reader.GetInt32(1),
                                Pending     = reader.GetInt32(2),
                                Resolved    = reader.GetInt32(3),
                                HighPriority= reader.GetInt32(4)
                            });
                        }
                    }
                }
            }
            catch { }

            double resolutionRate = totalFeedback > 0 ? (double)resolvedCount / totalFeedback * 100 : 0;

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Satisfaction", avgSatisfaction.ToString("F1") + " / 5", ThemeHelper.Success, 200, 90));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Feedback", totalFeedback.ToString(), ThemeHelper.Primary, 200, 90));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Compliments", compliments.ToString(), ThemeHelper.Accent, 200, 90));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Resolution Rate", resolutionRate.ToString("F0") + "%", ThemeHelper.Info, 200, 90));

            this.Controls.Add(kpiPanel);

            // ── Monthly Booking Trends (table mock) ──────────────────
            var cardTrends = ThemeHelper.CreateCardPanel();
            cardTrends.Location = new Point(8, 200);
            cardTrends.Size = new Size(470, 290);
            cardTrends.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            var lblTrends = ThemeHelper.CreateSectionHeader("📊 Monthly Booking Trends");
            lblTrends.Dock = DockStyle.Top;
            cardTrends.Controls.Add(lblTrends);

            var dgvTrends = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvTrends);

            var trendData = new[]
            {
                new { Month = "July 2026",      Bookings = 28, Revenue = "₱168,000", AvgStay = "3.2 nights", Occupancy = "72%" },
                new { Month = "August 2026",     Bookings = 35, Revenue = "₱245,000", AvgStay = "3.8 nights", Occupancy = "85%" },
                new { Month = "September 2026",  Bookings = 42, Revenue = "₱315,500", AvgStay = "3.5 nights", Occupancy = "91%" },
                new { Month = "October 2026",    Bookings = 18, Revenue = "₱126,000", AvgStay = "3.4 nights", Occupancy = "65%" },
            };

            dgvTrends.DataSource = trendData;
            cardTrends.Controls.Add(dgvTrends);
            this.Controls.Add(cardTrends);

            // ── Feedback Category Breakdown ──────────────────────────
            var cardCat = ThemeHelper.CreateCardPanel();
            cardCat.Location = new Point(490, 200);
            cardCat.Size = new Size(470, 290);
            cardCat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblCat = ThemeHelper.CreateSectionHeader("📋 Feedback Breakdown");
            lblCat.Dock = DockStyle.Top;
            cardCat.Controls.Add(lblCat);

            var dgvCat = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvCat);

            dgvCat.DataSource = catData;
            cardCat.Controls.Add(dgvCat);
            this.Controls.Add(cardCat);

            // ── Bar Chart Mockup ─────────────────────────────────────
            var cardChart = ThemeHelper.CreateCardPanel();
            cardChart.Location = new Point(8, 500);
            cardChart.Size = new Size(952, 180);
            cardChart.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblChart = ThemeHelper.CreateSectionHeader("📈 Occupancy Visualization");
            lblChart.Dock = DockStyle.Top;
            cardChart.Controls.Add(lblChart);

            // Simple painted bar chart
            var chartPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 10) };
            chartPanel.Paint += (s, e) =>
            {
                var months = new[] { "Jul", "Aug", "Sep", "Oct" };
                var values = new[] { 72, 85, 91, 65 };
                var colors = new[] { ThemeHelper.PrimaryLight, ThemeHelper.Primary, ThemeHelper.PrimaryDark, ThemeHelper.AccentSoft };

                int barWidth = 80;
                int gap = 30;
                int maxHeight = 100;
                int startX = 40;

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                for (int i = 0; i < months.Length; i++)
                {
                    int x = startX + i * (barWidth + gap);
                    int barH = (int)(values[i] / 100.0 * maxHeight);
                    int barY = maxHeight - barH + 5;

                    using (var brush = new SolidBrush(colors[i]))
                    {
                        e.Graphics.FillRectangle(brush, x, barY, barWidth, barH);
                    }

                    // Value on top
                    using (var font = ThemeHelper.FontBodyBold)
                    {
                        var valStr = values[i] + "%";
                        var sz = e.Graphics.MeasureString(valStr, font);
                        e.Graphics.DrawString(valStr, font, new SolidBrush(ThemeHelper.TextDark),
                            x + barWidth / 2 - sz.Width / 2, barY - 20);
                    }

                    // Month label
                    using (var font = ThemeHelper.FontSmall)
                    {
                        var sz = e.Graphics.MeasureString(months[i], font);
                        e.Graphics.DrawString(months[i], font, new SolidBrush(ThemeHelper.TextMedium),
                            x + barWidth / 2 - sz.Width / 2, maxHeight + 10);
                    }
                }
            };
            cardChart.Controls.Add(chartPanel);
            this.Controls.Add(cardChart);
        }
    }
}
