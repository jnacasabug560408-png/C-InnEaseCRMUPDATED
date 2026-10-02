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
    /// Staff/Admin/Manager: Handle Guest Requests — full CRUD with Room Type integration.
    /// </summary>
    public class ViewGuestRequests : UserControl
    {
        private DataGridView dgvRequests;
        private ComboBox cmbGuest, cmbRoomType, cmbRoom, cmbType, cmbStatus, cmbAssigned;
        private TextBox txtDescription;
        private int? _editingId = null;
        private User _currentUser;

        // Room ranges per type
        private static readonly Dictionary<string, (int Min, int Max)> RoomRanges = new Dictionary<string, (int, int)>
        {
            { "Standard",    (1,   120) },
            { "Deluxe",      (121, 150) },
            { "Suite",       (151, 180) },
            { "Presidential",(181, 200) }
        };

        public ViewGuestRequests(User currentUser) { _currentUser = currentUser; Init(); }
        public ViewGuestRequests() { Init(); }

        private void Init()
        {
            this.BackColor  = ThemeHelper.BackgroundLight;
            this.Padding    = new Padding(8);
            this.AutoScroll = true;
            BuildUI();
            RefreshGrid();
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────
            this.Controls.Add(new Label
            {
                Text      = "Guest Requests",
                Font      = ThemeHelper.FontTitle,
                ForeColor = ThemeHelper.TextDark,
                AutoSize  = true,
                Location  = new Point(12, 10)
            });
            this.Controls.Add(new Label
            {
                Text      = "Log and track service requests from guests",
                Font      = ThemeHelper.FontSubtitle,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize  = true,
                Location  = new Point(14, 48)
            });

            // ── Form Card ─────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(8, 85);
            cardForm.Size     = new Size(390, 430);
            cardForm.Anchor   = AnchorStyles.Top | AnchorStyles.Left;

            var lblForm = ThemeHelper.CreateSectionHeader("Service Request");
            lblForm.Location = new Point(10, 5);
            cardForm.Controls.Add(lblForm);

            int y = 35;

            // Guest
            var guestNames = new List<string>();
            var staffNames = new List<string>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT FullName FROM Guests ORDER BY FullName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) guestNames.Add(r.GetString(0));
                    }
                    using (var cmd = new SqlCommand("SELECT FullName FROM Users WHERE Role IN ('Staff','Manager') ORDER BY FullName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) staffNames.Add(r.GetString(0));
                    }
                }
            }
            catch { }

            AddComboRow(cardForm, "Guest", ref y, out cmbGuest, guestNames.ToArray());
            cmbGuest.SelectedIndexChanged += CmbGuest_SelectedIndexChanged;

            // Room Type
            AddComboRow(cardForm, "Room Type", ref y, out cmbRoomType,
                new[] { "Standard", "Deluxe", "Suite", "Presidential" });
            cmbRoomType.SelectedIndexChanged += CmbRoomType_SelectedIndexChanged;

            // Room #
            AddComboRow(cardForm, "Room #", ref y, out cmbRoom, new string[0]);
            PopulateRoomNumbers("Standard"); // default

            // Request Type
            AddComboRow(cardForm, "Request Type", ref y, out cmbType,
                new[] { "Housekeeping", "RoomService", "Maintenance", "Concierge" });

            // Status
            AddComboRow(cardForm, "Status", ref y, out cmbStatus,
                new[] { "Pending", "InProgress", "Completed" });

            // Assign To
            AddComboRow(cardForm, "Assign To", ref y, out cmbAssigned, staffNames.ToArray());

            // Details (multiline)
            cardForm.Controls.Add(new Label
            {
                Text      = "Details",
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 4)
            });
            txtDescription = new TextBox
            {
                Font        = ThemeHelper.FontBody,
                Size        = new Size(240, 65),
                Location    = new Point(130, y),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical
            };
            cardForm.Controls.Add(txtDescription);
            y += 75;

            // ── CRUD Buttons ──────────────────────────────────────────
            var btnCreate = ThemeHelper.CreatePrimaryButton("➕ Create", 110, 36);
            btnCreate.Location = new Point(10, y);
            btnCreate.Click   += BtnCreate_Click;
            cardForm.Controls.Add(btnCreate);

            var btnUpdate = ThemeHelper.CreateSecondaryButton("✏️ Update", 110, 36);
            btnUpdate.Location = new Point(128, y);
            btnUpdate.Click   += BtnUpdate_Click;
            cardForm.Controls.Add(btnUpdate);

            var btnDelete = ThemeHelper.CreateDangerButton("🗑️ Delete", 100, 36);
            btnDelete.Location = new Point(246, y);
            btnDelete.Click   += BtnDelete_Click;
            cardForm.Controls.Add(btnDelete);

            y += 44;

            var btnDone = ThemeHelper.CreateSuccessButton("✅ Mark Done", 150, 36);
            btnDone.Location = new Point(10, y);
            btnDone.Click   += BtnMarkDone_Click;
            cardForm.Controls.Add(btnDone);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 90, 36);
            btnClear.Location = new Point(168, y);
            btnClear.Click   += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            this.Controls.Add(cardForm);

            // ── Grid Card ─────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(410, 85);
            cardGrid.Size     = new Size(600, 430);
            cardGrid.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var lblGrid = ThemeHelper.CreateSectionHeader("Request Log");
            lblGrid.Location = new Point(10, 5);
            cardGrid.Controls.Add(lblGrid);

            dgvRequests = new DataGridView
            {
                Location = new Point(8, 32),
                Size     = new Size(cardGrid.Width - 20, cardGrid.Height - 42),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            ThemeHelper.StyleDataGridView(dgvRequests);
            dgvRequests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRequests.CellClick          += DgvRequests_CellClick;
            dgvRequests.CellFormatting     += DgvRequests_CellFormatting;
            cardGrid.Controls.Add(dgvRequests);

            this.Controls.Add(cardGrid);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void AddComboRow(Panel p, string label, ref int y, out ComboBox cmb, string[] items)
        {
            p.Controls.Add(new Label
            {
                Text      = label,
                Font      = ThemeHelper.FontBodyBold,
                ForeColor = ThemeHelper.TextMedium,
                AutoSize  = true,
                Location  = new Point(10, y + 4)
            });
            cmb = new ComboBox
            {
                Font          = ThemeHelper.FontBody,
                Size          = new Size(240, 28),
                Location      = new Point(130, y),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            if (items.Length > 0)
            {
                cmb.Items.AddRange(items);
                cmb.SelectedIndex = 0;
            }
            p.Controls.Add(cmb);
            y += 34;
        }

        private void PopulateRoomNumbers(string roomType)
        {
            cmbRoom.Items.Clear();
            if (!RoomRanges.ContainsKey(roomType)) return;
            var (min, max) = RoomRanges[roomType];
            for (int i = min; i <= max; i++)
                cmbRoom.Items.Add(i.ToString("D3"));
            if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
        }

        // ── Events ────────────────────────────────────────────────────
        private void CmbRoomType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateRoomNumbers(cmbRoomType.SelectedItem?.ToString() ?? "Standard");
        }

        private void CmbGuest_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-fill Room Type and Room # from guest's active CheckedIn reservation
            string guestName = cmbGuest.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(guestName)) return;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT TOP 1 RoomNumber, RoomType FROM Reservations WHERE GuestName=@gn AND Status='CheckedIn'", conn))
                {
                    cmd.Parameters.AddWithValue("@gn", guestName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string roomNum  = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            string roomType = reader.IsDBNull(1) ? GetRoomTypeFromNumber(roomNum) : reader.GetString(1);
                            if (!string.IsNullOrEmpty(roomType) && cmbRoomType.Items.Contains(roomType))
                            {
                                cmbRoomType.SelectedItem = roomType;
                                if (cmbRoom.Items.Contains(roomNum)) cmbRoom.SelectedItem = roomNum;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private string GetRoomTypeFromNumber(string roomNumStr)
        {
            if (!int.TryParse(roomNumStr, out int num)) return "";
            foreach (var kvp in RoomRanges)
                if (num >= kvp.Value.Min && num <= kvp.Value.Max)
                    return kvp.Key;
            return "";
        }

        // ── Grid ─────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, GuestName, RoomType, RoomNumber, RequestType, Status, RequestDate, AssignedTo FROM ServiceRequests ORDER BY RequestDate DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string rn = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        string rt = reader.IsDBNull(2) ? GetRoomTypeFromNumber(rn) : reader.GetString(2);
                        rows.Add(new
                        {
                            Id          = reader.GetInt32(0),
                            Guest       = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Room_Type   = rt,
                            Room        = rn,
                            Request     = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Status      = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Date        = reader.GetDateTime(6).ToString("MMM dd, HH:mm"),
                            Assigned_To = reader.IsDBNull(7) ? "" : reader.GetString(7)
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }

            dgvRequests.DataSource = null;
            dgvRequests.DataSource = rows;
        }

        private void DgvRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvRequests.Columns.Count == 0) return;
            string colName = dgvRequests.Columns[e.ColumnIndex].Name;
            if (colName == "Status" && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Pending":
                        e.CellStyle.ForeColor = Color.FromArgb(210, 120, 0);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "InProgress":
                        e.CellStyle.ForeColor = Color.FromArgb(30, 100, 200);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                    case "Completed":
                        e.CellStyle.ForeColor = Color.FromArgb(34, 139, 60);
                        e.CellStyle.Font      = new Font(ThemeHelper.FontBody, FontStyle.Bold); break;
                }
            }
        }

        // ── CRUD Handlers ─────────────────────────────────────────────
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            string guestName = cmbGuest.SelectedItem?.ToString() ?? "";
            try
            {
                int guestId = 0;
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT Id FROM Guests WHERE FullName=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", guestName);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) guestId = (int)obj;
                    }

                    using (var cmd = new SqlCommand(@"
                        INSERT INTO ServiceRequests (GuestId, GuestName, RoomType, RoomNumber, RequestType, Description, Status, AssignedTo)
                        VALUES (@gId, @gn, @rt, @rn, @rqt, @desc, @st, @asn)", conn))
                    {
                        cmd.Parameters.AddWithValue("@gId",  guestId);
                        cmd.Parameters.AddWithValue("@gn",   guestName);
                        cmd.Parameters.AddWithValue("@rt",   cmbRoomType.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@rn",   cmbRoom.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@rqt",  cmbType.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@desc", txtDescription.Text.Trim());
                        cmd.Parameters.AddWithValue("@st",   cmbStatus.SelectedItem?.ToString() ?? "Pending");
                        cmd.Parameters.AddWithValue("@asn",  cmbAssigned.SelectedItem?.ToString() ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Request created successfully!", "Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue)
            {
                MessageBox.Show("Please select a request row from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string guestName = cmbGuest.SelectedItem?.ToString() ?? "";
            try
            {
                int guestId = 0;
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT Id FROM Guests WHERE FullName=@n", conn))
                    {
                        cmd.Parameters.AddWithValue("@n", guestName);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) guestId = (int)obj;
                    }

                    using (var cmd = new SqlCommand(@"
                        UPDATE ServiceRequests SET GuestId=@gId, GuestName=@gn, RoomType=@rt,
                        RoomNumber=@rn, RequestType=@rqt, Status=@st, AssignedTo=@asn, Description=@desc
                        WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id",   _editingId.Value);
                        cmd.Parameters.AddWithValue("@gId",  guestId);
                        cmd.Parameters.AddWithValue("@gn",   guestName);
                        cmd.Parameters.AddWithValue("@rt",   cmbRoomType.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@rn",   cmbRoom.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@rqt",  cmbType.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@st",   cmbStatus.SelectedItem?.ToString() ?? "Pending");
                        cmd.Parameters.AddWithValue("@asn",  cmbAssigned.SelectedItem?.ToString() ?? "");
                        cmd.Parameters.AddWithValue("@desc", txtDescription.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Request updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue)
            {
                MessageBox.Show("Please select a request row from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Are you sure you want to delete this request?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM ServiceRequests WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _editingId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Request deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnMarkDone_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue)
            {
                MessageBox.Show("Please select a request row from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("UPDATE ServiceRequests SET Status='Completed', CompletedDate=GETDATE() WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _editingId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); return; }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Request marked as completed!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Grid Row Click → Populate Form ───────────────────────────
        private void DgvRequests_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvRequests.Rows[e.RowIndex].Cells["Id"].Value;

            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT GuestName, RoomType, RoomNumber, RequestType, Status, AssignedTo, Description FROM ServiceRequests WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _editingId = id;
                            string gn  = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            string rt  = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            string rn  = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string rqt = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string st  = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            string asn = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            string dsc = reader.IsDBNull(6) ? "" : reader.GetString(6);

                            if (cmbGuest.Items.Contains(gn))    cmbGuest.SelectedItem    = gn;
                            if (!string.IsNullOrEmpty(rt) && cmbRoomType.Items.Contains(rt)) cmbRoomType.SelectedItem = rt;
                            if (cmbRoom.Items.Contains(rn))     cmbRoom.SelectedItem     = rn;
                            if (cmbType.Items.Contains(rqt))    cmbType.SelectedItem     = rqt;
                            if (cmbStatus.Items.Contains(st))   cmbStatus.SelectedItem   = st;
                            if (cmbAssigned.Items.Contains(asn))cmbAssigned.SelectedItem = asn;
                            txtDescription.Text = dsc;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ClearForm()
        {
            _editingId = null;
            if (cmbGuest.Items.Count > 0)    cmbGuest.SelectedIndex    = 0;
            if (cmbRoomType.Items.Count > 0) { cmbRoomType.SelectedIndex = 0; PopulateRoomNumbers("Standard"); }
            if (cmbRoom.Items.Count > 0)     cmbRoom.SelectedIndex     = 0;
            if (cmbType.Items.Count > 0)     cmbType.SelectedIndex     = 0;
            if (cmbStatus.Items.Count > 0)   cmbStatus.SelectedIndex   = 0;
            if (cmbAssigned.Items.Count > 0) cmbAssigned.SelectedIndex = 0;
            txtDescription.Text = "";
        }
    }
}
