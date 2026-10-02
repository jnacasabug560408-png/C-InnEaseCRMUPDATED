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
    /// Manager: Monitor Customer Relationships — VIP guest status and interaction timeline.
    /// </summary>
    public class ViewCrmMonitor : UserControl
    {
        public ViewCrmMonitor(User currentUser) { Init(); }
        public ViewCrmMonitor() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            BuildUI();
        }

        private void BuildUI()
        {
            // Main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(8),
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));  // Header
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // KPI cards
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Tables
            this.Controls.Add(mainPanel);

            // ── Header ───────────────────────────────────────────────
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(headerPanel, 0, 0);

            var lblTitle = new Label
            {
                Text = "Customer Relationship Monitor",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(4, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "VIP guest status and interaction timeline",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(4, 48)
            };
            headerPanel.Controls.Add(lblSub);

            // ── VIP Guest Cards ──────────────────────────────────────
            int vipCount = 0, totalInteractions = 0;
            double avgLoyalty = 0;
            var vipRows = new List<dynamic>();
            var interactionRows = new List<dynamic>();

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests WHERE IsVIP=1", conn))
                        vipCount = (int)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT ISNULL(AVG(CAST(LoyaltyPoints AS FLOAT)), 0) FROM Guests WHERE IsVIP=1", conn))
                        avgLoyalty = (double)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM GuestInteractions", conn))
                        totalInteractions = (int)cmd.ExecuteScalar();

                    using (var cmd = new SqlCommand("SELECT Id, FullName, LoyaltyPoints, DateRegistered, Notes FROM Guests WHERE IsVIP=1 ORDER BY LoyaltyPoints DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vipRows.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                LoyaltyPts = reader.GetInt32(2).ToString("N0"),
                                Since = reader.GetDateTime(3).ToString("MMM yyyy"),
                                Notes = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT TOP 50 InteractionDate, InteractionType, GuestName, Notes, StaffMember FROM GuestInteractions ORDER BY InteractionDate DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            interactionRows.Add(new
                            {
                                Date = reader.GetDateTime(0).ToString("MMM dd, HH:mm"),
                                Type = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Guest = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Details = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Staff = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            catch { }

            var kpiPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
            };
            mainPanel.Controls.Add(kpiPanel, 0, 1);

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("VIP Guests", vipCount.ToString(), ThemeHelper.Accent, 200, 90));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Avg. Loyalty", ((int)avgLoyalty).ToString("N0"), ThemeHelper.Primary, 200, 90));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Interactions", totalInteractions.ToString(), ThemeHelper.Info, 200, 90));

            // ── Tables Area (Split into two columns) ───────────────────
            var tablesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Padding = new Padding(0, 8, 0, 0),
            };
            tablesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tablesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainPanel.Controls.Add(tablesPanel, 0, 2);

            // ── VIP Guest List ───────────────────────────────────────
            var cardVip = ThemeHelper.CreateCardPanel();
            cardVip.Dock = DockStyle.Fill;
            cardVip.Padding = new Padding(12);

            var lblVip = ThemeHelper.CreateSectionHeader("⭐ VIP Guest Profiles");
            lblVip.Dock = DockStyle.Top;
            cardVip.Controls.Add(lblVip);

            var dgvVip = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvVip);
            dgvVip.DataSource = vipRows;
            cardVip.Controls.Add(dgvVip);
            tablesPanel.Controls.Add(cardVip, 0, 0);

            // ── Interaction Timeline ─────────────────────────────────
            var cardTimeline = ThemeHelper.CreateCardPanel();
            cardTimeline.Dock = DockStyle.Fill;
            cardTimeline.Padding = new Padding(12);

            var lblTimeline = ThemeHelper.CreateSectionHeader("📋 Recent Interactions");
            lblTimeline.Dock = DockStyle.Top;
            cardTimeline.Controls.Add(lblTimeline);

            var dgvTimeline = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvTimeline);
            dgvTimeline.DataSource = interactionRows;
            cardTimeline.Controls.Add(dgvTimeline);
            tablesPanel.Controls.Add(cardTimeline, 1, 0);
        }
    }
}
