using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using InnEase.Infrastructure.Data;
using INNEASE.CRM.Helpers;
using InnEase.Domain.Models;

namespace INNEASE.CRM.ViewForms
{
    /// <summary>
    /// Staff/Admin: Manage Reservations — inline booking form with room types,
    /// auto-pricing, status management, and a full payment processing dialog.
    /// </summary>
    public class ViewReservations : UserControl
    {
        private DataGridView dgvReservations;
        private int? _editingId = null;

        // ── Room type catalogue with nightly rates ───────────────────
        private static readonly Dictionary<string, (string[] Rooms, decimal Rate)> RoomCatalogue
            = new Dictionary<string, (string[] Rooms, decimal Rate)>
        {
            { "Standard",     (Enumerable.Range(1, 120).Select(i => i.ToString("D3")).ToArray(), 3000m)  },
            { "Deluxe",       (Enumerable.Range(121, 30).Select(i => i.ToString("D3")).ToArray(), 5000m)  },
            { "Suite",        (Enumerable.Range(151, 30).Select(i => i.ToString("D3")).ToArray(), 9000m)  },
            { "Presidential", (Enumerable.Range(181, 20).Select(i => i.ToString("D3")).ToArray(), 15000m) },
        };

        public ViewReservations(User currentUser) { Init(); }
        public ViewReservations() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.AutoScroll = true;
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Reservations",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Track bookings, stays and room assignments",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 35)
            };
            this.Controls.Add(lblSubtitle);

            // ── KPI Cards ────────────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(900, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            int confirmed = 0, checkedIn = 0, pending = 0, paidCount = 0;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE Status='Confirmed'", conn))
                        confirmed = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE Status='CheckedIn'", conn))
                        checkedIn = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE Status='Pending'", conn))
                        pending = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Reservations WHERE PaymentStatus='Paid'", conn))
                        paidCount = (int)cmd.ExecuteScalar();
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Confirmed",    confirmed.ToString(),  "↑8.4%",        ThemeHelper.Success, 215, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Checked In",   checkedIn.ToString(),  "In-house",     ThemeHelper.Info,    215, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Pending",      pending.ToString(),    "Needs review", ThemeHelper.Warning, 215, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateModernKpiCard("Paid",         paidCount.ToString(),  "Payment done", ThemeHelper.Accent,  215, 100));

            this.Controls.Add(kpiPanel);

            // ── Search Bar ───────────────────────────────────────────
            var txtSearch = ThemeHelper.CreateSearchBar(400, "Search reservation, guest or room");
            txtSearch.Location = new Point(420, 195);
            this.Controls.Add(txtSearch);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 195);
            cardForm.Size = new Size(400, 670);
            cardForm.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("New Reservation");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 58;
            int lblX = 20, ctrlX = 160, ctrlW = 220, rowH = 40;

            // Guest Name
            cardForm.Controls.Add(new Label { Text = "Guest Name", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbGuest = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT ISNULL(FullName, ISNULL(FirstName,'')+' '+ISNULL(LastName,'')) FROM Guests", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        cmbGuest.Items.Add(reader.GetString(0));
                }
            }
            catch { }
            if (cmbGuest.Items.Count > 0) cmbGuest.SelectedIndex = 0;
            cardForm.Controls.Add(cmbGuest);
            y += rowH;

            // Room Type
            cardForm.Controls.Add(new Label { Text = "Room Type", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbRoomType = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoomType.Items.AddRange(RoomCatalogue.Keys.ToArray());
            cmbRoomType.SelectedIndex = 0;
            cardForm.Controls.Add(cmbRoomType);
            y += rowH;

            // Room Number
            cardForm.Controls.Add(new Label { Text = "Room Number", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbRoom = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cardForm.Controls.Add(cmbRoom);
            y += rowH;

            // Nightly Rate (read-only display)
            cardForm.Controls.Add(new Label { Text = "Nightly Rate", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var lblRate = new Label { Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.Success, AutoSize = true, Location = new Point(ctrlX, y + 4) };
            cardForm.Controls.Add(lblRate);
            y += rowH;

            // Check-In Date
            cardForm.Controls.Add(new Label { Text = "Check-In Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var dtpCheckIn = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            cardForm.Controls.Add(dtpCheckIn);
            y += rowH;

            // Check-Out Date
            cardForm.Controls.Add(new Label { Text = "Check-Out Date", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var dtpCheckOut = new DateTimePicker { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(3) };
            cardForm.Controls.Add(dtpCheckOut);
            y += rowH;

            // Promo Code
            cardForm.Controls.Add(new Label { Text = "Promo Code", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbPromoCode = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPromoCode.Items.Add("None");
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Code FROM Promotions WHERE IsActive=1 AND GETDATE() >= StartDate AND GETDATE() <= EndDate", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        cmbPromoCode.Items.Add(reader.GetString(0));
                }
            }
            catch { }
            cmbPromoCode.SelectedIndex = 0;
            cardForm.Controls.Add(cmbPromoCode);
            y += rowH;

            // Total Amount (auto-calculated, read-only)
            cardForm.Controls.Add(new Label { Text = "Total Amount (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var nudAmount = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), Maximum = 9999999, Minimum = 0, DecimalPlaces = 0, Value = 9000, ReadOnly = true, BackColor = Color.FromArgb(245, 245, 245) };
            cardForm.Controls.Add(nudAmount);
            y += rowH;

            // Booking Source
            cardForm.Controls.Add(new Label { Text = "Booking Source", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbSource = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSource.Items.AddRange(new[] { "Direct", "OTA", "Walk-in" });
            cmbSource.SelectedIndex = 0;
            cardForm.Controls.Add(cmbSource);
            y += rowH;

            // Status
            cardForm.Controls.Add(new Label { Text = "Status", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(lblX, y + 4) });
            var cmbStatus = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(ctrlW, 28), Location = new Point(ctrlX, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new[] { "Pending", "Confirmed", "CheckedIn", "CheckedOut", "Cancelled" });
            cmbStatus.SelectedIndex = 0;
            cardForm.Controls.Add(cmbStatus);
            y += rowH + 5;

            // Buttons row
            var btnSave = ThemeHelper.CreatePrimaryButton("Save Booking", 118, 36);
            btnSave.Location = new Point(lblX, y);
            cardForm.Controls.Add(btnSave);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 80, 36);
            btnClear.Location = new Point(148, y);
            cardForm.Controls.Add(btnClear);

            var btnDelete = ThemeHelper.CreateSecondaryButton("Delete", 80, 36);
            btnDelete.Location = new Point(238, y);
            btnDelete.ForeColor = Color.Crimson;
            btnDelete.Enabled = false;
            cardForm.Controls.Add(btnDelete);
            y += 46;

            // Process Payment button
            var btnPayment = ThemeHelper.CreatePrimaryButton("💳  Process Payment", 200, 36);
            btnPayment.Location = new Point(lblX, y);
            cardForm.Controls.Add(btnPayment);

            this.Controls.Add(cardForm);

            // ── Table Card ───────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(420, 245);
            cardGrid.Size = new Size(this.Width - 460, 540);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            dgvReservations = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvReservations);
            cardGrid.Controls.Add(dgvReservations);
            this.Controls.Add(cardGrid);

            // ── Helper: Recalculate amount ────────────────────────────
            Action recalcAmount = () =>
            {
                string type = cmbRoomType.SelectedItem?.ToString() ?? "Standard";
                if (RoomCatalogue.TryGetValue(type, out var cat))
                {
                    int nights = Math.Max(1, (dtpCheckOut.Value - dtpCheckIn.Value).Days);
                    decimal total = cat.Rate * nights;

                    string promoCode = cmbPromoCode.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(promoCode) && promoCode != "None")
                    {
                        try
                        {
                            using (var conn = DatabaseService.GetConnection())
                            using (var cmd = new SqlCommand("SELECT DiscountPercent FROM Promotions WHERE Code=@c", conn))
                            {
                                cmd.Parameters.AddWithValue("@c", promoCode);
                                object res = cmd.ExecuteScalar();
                                if (res != null)
                                {
                                    decimal discount = Convert.ToDecimal(res);
                                    total -= (total * (discount / 100m));
                                }
                            }
                        }
                        catch { }
                    }

                    nudAmount.Value = total;
                    lblRate.Text = "₱" + cat.Rate.ToString("N0") + " / night  ×  " + nights + " night" + (nights > 1 ? "s" : "");
                }
            };

            // ── Room type changed → update rooms + rate ───────────────
            Action updateRoomList = () =>
            {
                cmbRoom.Items.Clear();
                string type = cmbRoomType.SelectedItem?.ToString() ?? "Standard";
                if (RoomCatalogue.TryGetValue(type, out var cat))
                {
                    var allRooms = cat.Rooms;
                    var activeStatuses = new[] { "CheckedIn", "Confirmed", "Pending" };
                    var takenRooms = new List<string>();
                    try
                    {
                        using (var conn = DatabaseService.GetConnection())
                        using (var cmd = new SqlCommand("SELECT RoomNumber FROM Reservations WHERE Status IN ('CheckedIn', 'Confirmed', 'Pending')", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) takenRooms.Add(reader.GetString(0));
                        }
                    }
                    catch { }

                    string currentRoomToKeep = null;
                    if (_editingId.HasValue)
                    {
                        try
                        {
                            using (var conn = DatabaseService.GetConnection())
                            using (var cmd = new SqlCommand("SELECT RoomNumber, RoomType FROM Reservations WHERE Id=@id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", _editingId.Value);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read() && reader.GetString(1) == type)
                                    {
                                        currentRoomToKeep = reader.GetString(0);
                                        takenRooms.Remove(currentRoomToKeep);
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    var availableRooms = allRooms.Where(r => !takenRooms.Contains(r) || r == currentRoomToKeep).ToArray();
                    cmbRoom.Items.AddRange(availableRooms);
                }
                if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
                recalcAmount();
            };

            cmbRoomType.SelectedIndexChanged += (s, e) => updateRoomList();
            cmbPromoCode.SelectedIndexChanged += (s, e) => recalcAmount();
            // Trigger initial population
            cmbRoomType.SelectedIndex = 0;

            // ── Dates changed → recalculate ───────────────────────────
            dtpCheckIn.ValueChanged  += (s, e) => recalcAmount();
            dtpCheckOut.ValueChanged += (s, e) => recalcAmount();

            // ── Row click → populate form ─────────────────────────────
            dgvReservations.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var cellVal = dgvReservations.Rows[e.RowIndex].Cells["Reservation"].Value;
                if (cellVal == null) return;

                int id;
                if (!int.TryParse(cellVal.ToString().Replace("#", ""), out id)) return;

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("SELECT Id, GuestName, RoomType, RoomNumber, CheckInDate, CheckOutDate, BookingSource, PromoCode, Status, TotalAmount, NightlyRate, PaymentStatus FROM Reservations WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            _editingId = reader.GetInt32(0);
                            lblFormTitle.Text = "Edit  #" + _editingId;
                            btnSave.Text = "Update Booking";
                            btnDelete.Enabled = true;
                            
                            string paymentStatus = reader.IsDBNull(11) ? "" : reader.GetString(11);
                            btnPayment.Enabled = (paymentStatus != "Paid");

                            cmbGuest.Text = reader.GetString(1);
                            
                            string rType = reader.GetString(2);
                            if (cmbRoomType.Items.Contains(rType))
                                cmbRoomType.SelectedItem = rType;
                            else updateRoomList();

                            cmbRoom.Text = reader.GetString(3);
                            dtpCheckIn.Value = reader.GetDateTime(4);
                            dtpCheckOut.Value = reader.GetDateTime(5);

                            string src = reader.IsDBNull(6) ? "Direct" : reader.GetString(6);
                            if (cmbSource.Items.Contains(src)) cmbSource.SelectedItem = src;

                            string pCode = reader.IsDBNull(7) ? "" : reader.GetString(7);
                            if (string.IsNullOrEmpty(pCode)) cmbPromoCode.SelectedIndex = 0;
                            else if (cmbPromoCode.Items.Contains(pCode)) cmbPromoCode.SelectedItem = pCode;

                            string status = reader.GetString(8);
                            if (cmbStatus.Items.Contains(status)) cmbStatus.SelectedItem = status;

                            nudAmount.Value = reader.GetDecimal(9);
                            int nights = Math.Max(1, (dtpCheckOut.Value - dtpCheckIn.Value).Days);
                            lblRate.Text = "₱" + reader.GetDecimal(10).ToString("N0") + " / night  ×  " + nights + " night" + (nights > 1 ? "s" : "");
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error loading reservation: " + ex.Message); }
            };

            // ── Save / Update ─────────────────────────────────────────
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(cmbGuest.Text))
                {
                    MessageBox.Show("Guest Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string guestName  = cmbGuest.Text;
                string newStatus  = cmbStatus.SelectedItem?.ToString() ?? "Pending";
                string roomType   = cmbRoomType.SelectedItem?.ToString() ?? "Standard";
                decimal nightRate = RoomCatalogue.ContainsKey(roomType) ? RoomCatalogue[roomType].Rate : 3000m;
                
                int guestId = 0;
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("SELECT Id FROM Guests WHERE ISNULL(FullName, ISNULL(FirstName,'')+' '+ISNULL(LastName,''))=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", guestName);
                        var res = cmd.ExecuteScalar();
                        if (res != null) guestId = Convert.ToInt32(res);
                    }
                }
                catch { }

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    {
                        if (_editingId.HasValue)
                        {
                            using (var cmd = new SqlCommand("UPDATE Reservations SET GuestId=@gid, GuestName=@gn, RoomType=@rt, NightlyRate=@nr, RoomNumber=@rn, CheckInDate=@ci, CheckOutDate=@co, TotalAmount=@ta, BookingSource=@bs, PromoCode=@pc, Status=@st WHERE Id=@id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", _editingId.Value);
                                cmd.Parameters.AddWithValue("@gid", guestId);
                                cmd.Parameters.AddWithValue("@gn", guestName);
                                cmd.Parameters.AddWithValue("@rt", roomType);
                                cmd.Parameters.AddWithValue("@nr", nightRate);
                                cmd.Parameters.AddWithValue("@rn", cmbRoom.Text);
                                cmd.Parameters.AddWithValue("@ci", dtpCheckIn.Value);
                                cmd.Parameters.AddWithValue("@co", dtpCheckOut.Value);
                                cmd.Parameters.AddWithValue("@ta", nudAmount.Value);
                                cmd.Parameters.AddWithValue("@bs", cmbSource.SelectedItem?.ToString());
                                cmd.Parameters.AddWithValue("@pc", cmbPromoCode.SelectedItem?.ToString() == "None" ? (object)DBNull.Value : cmbPromoCode.SelectedItem.ToString());
                                cmd.Parameters.AddWithValue("@st", newStatus);
                                cmd.ExecuteNonQuery();
                            }
                            INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Updated reservation {_editingId.Value}", "Reservations");
                            MessageBox.Show("Reservation updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            using (var cmd = new SqlCommand("INSERT INTO Reservations (GuestId, GuestName, RoomType, NightlyRate, RoomNumber, CheckInDate, CheckOutDate, TotalAmount, BookingSource, PromoCode, Status, PaymentStatus, CreatedDate) VALUES (@gid, @gn, @rt, @nr, @rn, @ci, @co, @ta, @bs, @pc, @st, 'Pending Payment', GETDATE())", conn))
                            {
                                cmd.Parameters.AddWithValue("@gid", guestId);
                                cmd.Parameters.AddWithValue("@gn", guestName);
                                cmd.Parameters.AddWithValue("@rt", roomType);
                                cmd.Parameters.AddWithValue("@nr", nightRate);
                                cmd.Parameters.AddWithValue("@rn", cmbRoom.Text);
                                cmd.Parameters.AddWithValue("@ci", dtpCheckIn.Value);
                                cmd.Parameters.AddWithValue("@co", dtpCheckOut.Value);
                                cmd.Parameters.AddWithValue("@ta", nudAmount.Value);
                                cmd.Parameters.AddWithValue("@bs", cmbSource.SelectedItem?.ToString());
                                cmd.Parameters.AddWithValue("@pc", cmbPromoCode.SelectedItem?.ToString() == "None" ? (object)DBNull.Value : cmbPromoCode.SelectedItem.ToString());
                                cmd.Parameters.AddWithValue("@st", newStatus);
                                cmd.ExecuteNonQuery();
                            }
                            INNEASE.CRM.Helpers.AuditLogger.LogAction("Create", $"Created reservation for {guestName}", "Reservations");
                            MessageBox.Show("Reservation created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

                RefreshGrid();
                btnClear.PerformClick();
            };

            // ── Delete ────────────────────────────────────────────────
            btnDelete.Click += (s, e) =>
            {
                if (!_editingId.HasValue) return;
                if (MessageBox.Show("Delete this reservation?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("DELETE FROM Reservations WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingId.Value);
                        cmd.ExecuteNonQuery();
                    }
                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Delete", $"Deleted reservation {_editingId.Value}", "Reservations");
                    RefreshGrid();
                    btnClear.PerformClick();
                }
                catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
            };

            // ── Clear form ────────────────────────────────────────────
            btnClear.Click += (s, e) =>
            {
                _editingId = null;
                lblFormTitle.Text = "New Reservation";
                btnSave.Text = "Save Booking";
                btnDelete.Enabled = false;
                btnPayment.Enabled = true;

                cmbGuest.SelectedIndex  = cmbGuest.Items.Count > 0 ? 0 : -1;
                cmbRoomType.SelectedIndex = 0;
                cmbPromoCode.SelectedIndex = 0;
                dtpCheckIn.Value  = DateTime.Today;
                dtpCheckOut.Value = DateTime.Today.AddDays(3);
                cmbSource.SelectedIndex = 0;
                cmbStatus.SelectedIndex = 0;
                recalcAmount();
            };

            // ── Process Payment button ────────────────────────────────
            btnPayment.Click += (s, e) =>
            {
                Reservation target = null;
                if (_editingId.HasValue)
                {
                    try
                    {
                        using (var conn = DatabaseService.GetConnection())
                        using (var cmd = new SqlCommand("SELECT Id, GuestName, RoomType, RoomNumber, CheckInDate, CheckOutDate, TotalAmount, PaymentStatus, Status, PromoCode, NightlyRate FROM Reservations WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    target = new Reservation
                                    {
                                        Id = reader.GetInt32(0),
                                        GuestName = reader.GetString(1),
                                        RoomType = reader.GetString(2),
                                        RoomNumber = reader.GetString(3),
                                        CheckInDate = reader.GetDateTime(4),
                                        CheckOutDate = reader.GetDateTime(5),
                                        TotalAmount = reader.GetDecimal(6),
                                        PaymentStatus = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                        Status = reader.GetString(8),
                                        PromoCode = reader.IsDBNull(9) ? null : reader.GetString(9),
                                        NightlyRate = reader.GetDecimal(10)
                                    };
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (target == null)
                {
                    MessageBox.Show("Please select a reservation from the table first, or save a new one before processing payment.", "No Reservation Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (target.PaymentStatus == "Paid")
                {
                    MessageBox.Show("This reservation has already been paid.", "Already Paid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowPaymentDialog(target);
                RefreshGrid();
                
                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    using (var cmd = new SqlCommand("SELECT PaymentStatus FROM Reservations WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", target.Id);
                        var ps = cmd.ExecuteScalar()?.ToString();
                        btnPayment.Enabled = (ps != "Paid");
                    }
                }
                catch { }
            };

            // ── Resize ───────────────────────────────────────────────
            this.Resize += (s, e) =>
            {
                cardGrid.Width = this.Width - 460;
                txtSearch.Width = this.Width - 460;
            };
        }

        // ── Payment Dialog ────────────────────────────────────────────
        private void ShowPaymentDialog(Reservation reservation)
        {
            var parentForm = this.FindForm();
            var scrim = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.4,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = parentForm.PointToScreen(Point.Empty),
                Size = parentForm.ClientSize
            };
            scrim.Show(parentForm);

            var dlg = new Form
            {
                Text = "Process Payment — Reservation #" + reservation.Id,
                Size = new Size(540, 680),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeHelper.BackgroundLight,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int nights = reservation.Nights;
            decimal subtotal = reservation.TotalAmount;
            decimal taxes = subtotal * 0.12m; // 12% tax assumption
            decimal total = subtotal + taxes;
            decimal balance = total; // Assuming no prior payments in this simple model

            // Header banner
            var banner = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeHelper.SidebarDark };
            var lblHeader = new Label
            {
                Text = "💳  Payment Processing",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            banner.Controls.Add(lblHeader);
            dlg.Controls.Add(banner);

            // Summary Card
            var pnlSummary = new Panel { Location = new Point(20, 76), Size = new Size(485, 175), BackColor = Color.White };
            pnlSummary.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(ThemeHelper.CardBorder), 0, 0, pnlSummary.Width - 1, pnlSummary.Height - 1); };
            
            var lblSumTitle = new Label { Text = "Payment Summary", Font = ThemeHelper.FontHeading, ForeColor = ThemeHelper.TextDark, AutoSize = true, Location = new Point(15, 10) };
            pnlSummary.Controls.Add(lblSumTitle);

            void AddRow(string label, string val, int yPos, bool isBold = false)
            {
                pnlSummary.Controls.Add(new Label { Text = label, Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(15, yPos) });
                var vLbl = new Label { Text = val, Font = isBold ? ThemeHelper.FontBodyBold : ThemeHelper.FontBody, ForeColor = isBold ? ThemeHelper.PrimaryDark : ThemeHelper.TextDark, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Size = new Size(200, 20), Location = new Point(270, yPos) };
                pnlSummary.Controls.Add(vLbl);
            }

            AddRow("Guest Name", reservation.GuestName, 40);
            AddRow("Room", $"{reservation.RoomType} ({reservation.RoomNumber})", 65);
            AddRow("Stay Duration", $"{nights} night{(nights > 1 ? "s" : "")}", 90);
            AddRow("Subtotal", $"₱{subtotal:N2}", 115);
            AddRow("Amount Due", $"₱{total:N2}", 140, true);
            
            dlg.Controls.Add(pnlSummary);

            int dy = 270;

            // Payment Method
            var lblType = new Label { Text = "Payment Method", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, dy + 4) };
            dlg.Controls.Add(lblType);
            var cmbMethod = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(300, 28), Location = new Point(170, dy), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(new[] { "Cash", "Credit/Debit Card", "E-Wallet (GCash/Maya)" });
            cmbMethod.SelectedIndex = 0;
            dlg.Controls.Add(cmbMethod);
            dy += 44;

            // Amount to Pay Input
            var lblAmt = new Label { Text = "Amount (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, dy + 4) };
            dlg.Controls.Add(lblAmt);
            var txtPay = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(180, 28), Location = new Point(170, dy), BorderStyle = BorderStyle.FixedSingle };
            txtPay.Text = total.ToString("N2");
            dlg.Controls.Add(txtPay);
            
            var btnFull = ThemeHelper.CreateSecondaryButton("Full", 55, 26);
            btnFull.Location = new Point(360, dy);
            btnFull.Click += (s, e) => { txtPay.Text = total.ToString("N2"); };
            dlg.Controls.Add(btnFull);
            
            var btnHalf = ThemeHelper.CreateSecondaryButton("50%", 55, 26);
            btnHalf.Location = new Point(420, dy);
            btnHalf.Click += (s, e) => { txtPay.Text = (total / 2).ToString("N2"); };
            dlg.Controls.Add(btnHalf);
            dy += 44;

            // Dynamic Fields
            var pnlDynamic = new Panel { Location = new Point(20, dy), Size = new Size(485, 100), BackColor = ThemeHelper.BackgroundLight };
            dlg.Controls.Add(pnlDynamic);

            var lblDynLabel = new Label { Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(0, 4) };
            var txtDynInput = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(300, 28), Location = new Point(150, 0), BorderStyle = BorderStyle.FixedSingle };
            var lblChange = new Label { Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.Success, AutoSize = true, Location = new Point(150, 36) };
            
            pnlDynamic.Controls.Add(lblDynLabel);
            pnlDynamic.Controls.Add(txtDynInput);
            pnlDynamic.Controls.Add(lblChange);
            
            Action updateDynamicFields = () =>
            {
                string method = cmbMethod.SelectedItem?.ToString();
                if (method == "Cash")
                {
                    lblDynLabel.Text = "Cash Received";
                    lblDynLabel.Visible = txtDynInput.Visible = true;
                    lblChange.Visible = true;
                }
                else
                {
                    lblDynLabel.Text = "Reference ID";
                    lblDynLabel.Visible = txtDynInput.Visible = true;
                    lblChange.Visible = false;
                }
            };
            
            Action calculateChange = () =>
            {
                if (cmbMethod.SelectedItem?.ToString() == "Cash")
                {
                    if (decimal.TryParse(txtDynInput.Text.Replace(",", ""), out decimal cashRec) && decimal.TryParse(txtPay.Text.Replace(",", ""), out decimal payAmt))
                    {
                        decimal change = cashRec - payAmt;
                        lblChange.Text = change >= 0 ? "Change Due: ₱" + change.ToString("N2") : "⚠ Short by ₱" + Math.Abs(change).ToString("N2");
                        lblChange.ForeColor = change >= 0 ? ThemeHelper.Success : Color.Crimson;
                    }
                    else lblChange.Text = "";
                }
            };

            cmbMethod.SelectedIndexChanged += (s, e) => { updateDynamicFields(); calculateChange(); };
            txtDynInput.TextChanged += (s, e) => { calculateChange(); };
            txtPay.TextChanged += (s, e) => { calculateChange(); };
            
            updateDynamicFields();
            dy += 110;

            // Receipt Checkbox
            var chkReceipt = new CheckBox { Text = "Send / Print Digital Receipt upon confirmation", Font = ThemeHelper.FontBody, AutoSize = true, Location = new Point(20, dy), ForeColor = ThemeHelper.TextDark, Checked = true };
            dlg.Controls.Add(chkReceipt);
            dy += 50;

            // Formatting TextBox for Currency on Leave
            txtPay.Leave += (s, e) =>
            {
                if (decimal.TryParse(txtPay.Text.Replace(",", ""), out decimal val))
                    txtPay.Text = val.ToString("N2");
            };

            // Buttons
            var btnConfirmPay = ThemeHelper.CreatePrimaryButton("Confirm Payment", 200, 42);
            btnConfirmPay.Location = new Point(20, dy);
            dlg.Controls.Add(btnConfirmPay);

            // Custom neutral grey cancel button
            var btnCancelDlg = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = ThemeHelper.TextDark,
                Font = ThemeHelper.FontButton,
                Cursor = Cursors.Hand,
                Location = new Point(230, dy)
            };
            btnCancelDlg.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancelDlg.FlatAppearance.BorderSize = 1;
            btnCancelDlg.Region = Region.FromHrgn(ThemeHelper.CreateRoundRectRgn(0, 0, btnCancelDlg.Width, btnCancelDlg.Height, 8, 8));
            dlg.Controls.Add(btnCancelDlg);

            btnConfirmPay.Click += (s2, e2) =>
            {
                if (!decimal.TryParse(txtPay.Text.Replace(",", ""), out decimal amtPaid) || amtPaid <= 0)
                {
                    MessageBox.Show("Please enter a valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string method = cmbMethod.SelectedItem?.ToString();
                
                if (method == "Cash")
                {
                    if (decimal.TryParse(txtDynInput.Text.Replace(",", ""), out decimal cashRec))
                    {
                        if (cashRec < amtPaid)
                        {
                            MessageBox.Show("Cash received is less than the amount to pay.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtDynInput.Text))
                    {
                        MessageBox.Show("Please enter the Reference or Transaction ID.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                try
                {
                    using (var conn = DatabaseService.GetConnection())
                    {
                        using (var cmd = new SqlCommand("UPDATE Reservations SET PaymentStatus='Paid', PaymentMethod=@pm, TotalAmount=@ta, PaymentDate=GETDATE(), Status = CASE WHEN Status='Pending' THEN 'Confirmed' ELSE Status END WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", reservation.Id);
                            cmd.Parameters.AddWithValue("@pm", method);
                            cmd.Parameters.AddWithValue("@ta", amtPaid);
                            cmd.ExecuteNonQuery();
                        }
                        
                        if (!string.IsNullOrEmpty(reservation.PromoCode))
                        {
                            using (var cmd = new SqlCommand("UPDATE Promotions SET UsageCount = UsageCount + 1 WHERE Code=@pc", conn))
                            {
                                cmd.Parameters.AddWithValue("@pc", reservation.PromoCode);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Processed payment for reservation {reservation.Id} ({reservation.GuestName})", "Reservations");
                }
                catch (Exception ex) { MessageBox.Show("Error updating payment: " + ex.Message); }

                MessageBox.Show(
                    $"✅ Payment of ₱{amtPaid:N2} received via {method}.\nReservation #{reservation.Id} is now CONFIRMED.",
                    "Payment Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            btnCancelDlg.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.FormClosed += (s, e) => scrim.Close();

            dlg.ShowDialog(parentForm);
        }

        // ── Grid Refresh ──────────────────────────────────────────────
        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, GuestName, CheckInDate, CheckOutDate, RoomNumber, RoomType, TotalAmount, BookingSource, Status, PaymentStatus FROM Reservations ORDER BY CheckInDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new
                        {
                            Reservation = "#" + reader.GetInt32(0),
                            Guest       = reader.GetString(1),
                            Stay        = reader.GetDateTime(2).ToString("MMM dd") + " – " + reader.GetDateTime(3).ToString("MMM dd"),
                            Room        = reader.GetString(4),
                            Type        = reader.GetString(5),
                            Amount      = "₱" + reader.GetDecimal(6).ToString("N0"),
                            Source      = reader.IsDBNull(7) ? "Direct" : reader.GetString(7),
                            Status      = reader.GetString(8),
                            Payment     = reader.IsDBNull(9) ? "Pending Payment" : reader.GetString(9)
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading reservations: " + ex.Message); }

            dgvReservations.DataSource = null;
            dgvReservations.DataSource = rows;

            // Color-code payment column and align
            dgvReservations.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var colName = dgvReservations.Columns[e.ColumnIndex].Name;
                
                if (colName == "Amount")
                {
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (colName == "Payment")
                {
                    e.CellStyle.ForeColor = e.Value?.ToString() == "Paid"
                        ? ThemeHelper.Success : Color.FromArgb(200, 100, 0);
                    e.CellStyle.Font = ThemeHelper.FontBodyBold;
                }
                if (colName == "Status")
                {
                    string sv = e.Value?.ToString() ?? "";
                    if (sv == "Confirmed")       e.CellStyle.ForeColor = ThemeHelper.Success;
                    else if (sv == "CheckedIn")  e.CellStyle.ForeColor = ThemeHelper.Info;
                    else if (sv == "CheckedOut") e.CellStyle.ForeColor = ThemeHelper.TextMuted;
                    else if (sv == "Cancelled")  e.CellStyle.ForeColor = Color.Crimson;
                    else                         e.CellStyle.ForeColor = ThemeHelper.Warning;
                }
            };
            
            if (dgvReservations.Columns["Amount"] != null)
                dgvReservations.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (dgvReservations.Columns["Status"] != null)
                dgvReservations.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dgvReservations.Columns["Payment"] != null)
                dgvReservations.Columns["Payment"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
    }
}
