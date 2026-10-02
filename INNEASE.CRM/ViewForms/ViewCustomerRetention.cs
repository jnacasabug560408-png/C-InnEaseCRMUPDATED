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
    /// Manager: Customer Retention — retention rate KPIs and summary cards.
    /// </summary>
    public class ViewCustomerRetention : UserControl
    {
        public ViewCustomerRetention(User currentUser) { Init(); }
        public ViewCustomerRetention() { Init(); }

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
                Text = "Customer Retention",
                Font = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            this.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Retention rate KPIs and guest loyalty analysis",
                Font = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(14, 48)
            };
            this.Controls.Add(lblSub);

            // ── KPI Cards ────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(8, 85),
                Size = new Size(960, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            int repeatGuests = 0, totalGuests = 0, checkedOut = 0, avgLoyalty = 0;
            double retentionRate = 0;
            var tierData = new List<dynamic>();
            var topGuestData = new List<dynamic>();

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests WHERE LoyaltyPoints > 1000", conn))
                        repeatGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Guests", conn))
                        totalGuests = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE Status='CheckedOut'", conn))
                        checkedOut = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT ISNULL(AVG(CAST(LoyaltyPoints AS FLOAT)), 0) FROM Guests", conn))
                        avgLoyalty = (int)(double)cmd.ExecuteScalar();

                    retentionRate = totalGuests > 0 ? (double)repeatGuests / totalGuests * 100 : 0;

                    var tierSql = @"
                        SELECT 'First-Timer (0-999)',
                               SUM(CASE WHEN LoyaltyPoints < 1000 THEN 1 ELSE 0 END),
                               ISNULL(AVG(CASE WHEN LoyaltyPoints < 1000 THEN CAST(LoyaltyPoints AS FLOAT) END), 0)
                        FROM Guests
                        UNION ALL
                        SELECT 'Regular (1000-4999)',
                               SUM(CASE WHEN LoyaltyPoints >= 1000 AND LoyaltyPoints < 5000 THEN 1 ELSE 0 END),
                               ISNULL(AVG(CASE WHEN LoyaltyPoints >= 1000 AND LoyaltyPoints < 5000 THEN CAST(LoyaltyPoints AS FLOAT) END), 0)
                        FROM Guests
                        UNION ALL
                        SELECT 'VIP (5000+)',
                               SUM(CASE WHEN LoyaltyPoints >= 5000 THEN 1 ELSE 0 END),
                               ISNULL(AVG(CASE WHEN LoyaltyPoints >= 5000 THEN CAST(LoyaltyPoints AS FLOAT) END), 0)
                        FROM Guests";

                    using (var cmd = new SqlCommand(tierSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tierData.Add(new
                            {
                                Tier      = reader.GetString(0),
                                Guests    = reader.GetInt32(1),
                                AvgPoints = (int)(double)reader.GetDouble(2)
                            });
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT TOP 8 FullName, LoyaltyPoints, IsVIP, DateRegistered FROM Guests ORDER BY LoyaltyPoints DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topGuestData.Add(new
                            {
                                Name       = reader.GetString(0),
                                LoyaltyPts = reader.GetInt32(1).ToString("N0"),
                                VIP        = reader.GetBoolean(2) ? "⭐" : "",
                                Since      = reader.GetDateTime(3).ToString("MMM yyyy")
                            });
                        }
                    }
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Retention Rate", retentionRate.ToString("F1") + "%", ThemeHelper.Success));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Repeat Guests", repeatGuests.ToString(), ThemeHelper.Primary));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Completed Stays", checkedOut.ToString(), ThemeHelper.Info));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Avg. Loyalty Pts", avgLoyalty.ToString("N0"), ThemeHelper.Accent));

            this.Controls.Add(kpiPanel);

            // ── Loyalty Tier Table ───────────────────────────────────
            var cardTiers = ThemeHelper.CreateCardPanel();
            cardTiers.Location = new Point(8, 235);
            cardTiers.Size = new Size(470, 260);
            cardTiers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            var lblTiers = ThemeHelper.CreateSectionHeader("Guest Loyalty Tiers");
            lblTiers.Dock = DockStyle.Top;
            cardTiers.Controls.Add(lblTiers);

            var dgvTiers = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvTiers);
            dgvTiers.DataSource = tierData;
            cardTiers.Controls.Add(dgvTiers);
            this.Controls.Add(cardTiers);

            // ── Guest Retention Detail ───────────────────────────────
            var cardDetail = ThemeHelper.CreateCardPanel();
            cardDetail.Location = new Point(490, 235);
            cardDetail.Size = new Size(470, 260);
            cardDetail.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblDetail = ThemeHelper.CreateSectionHeader("Top Loyalty Guests");
            lblDetail.Dock = DockStyle.Top;
            cardDetail.Controls.Add(lblDetail);

            var dgvDetail = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvDetail);
            dgvDetail.DataSource = topGuestData;
            cardDetail.Controls.Add(dgvDetail);
            this.Controls.Add(cardDetail);
        }
    }
}
