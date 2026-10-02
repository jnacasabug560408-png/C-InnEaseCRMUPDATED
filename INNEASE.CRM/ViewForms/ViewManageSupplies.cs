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
    /// Admin: Manage Supplies - add, edit, and restock inventory items.
    /// </summary>
    public class ViewManageSupplies : UserControl
    {
        private DataGridView dgvSupplies;
        private TextBox txtName, txtDescription, txtSupplier, txtLocation;
        private ComboBox cmbCategory, cmbUnit;
        private NumericUpDown nudQuantity, nudMinimumStock, nudUnitCost;
        private CheckBox chkIsActive;
        private int? _editingId = null;

        public ViewManageSupplies(User currentUser) { Init(); }
        public ViewManageSupplies() { Init(); }

        private void Init()
        {
            this.BackColor = ThemeHelper.BackgroundLight;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.AutoScroll = true;
            this.Resize += (s, e) => UpdateLayout();
            BuildUI();
            RefreshGrid();
        }

        private void UpdateLayout()
        {
            // Update KPI panel width
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is FlowLayoutPanel kpiPanel && kpiPanel.Location.Y == 70)
                {
                    kpiPanel.Width = this.Width - 40;
                }
                // Update grid card width
                if (ctrl is Panel cardGrid && cardGrid.Location.X == 420)
                {
                    cardGrid.Width = this.Width - 460;
                    cardGrid.Height = this.Height - 215;
                }
            }
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "Manage Supplies",
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                ForeColor = ThemeHelper.TextDark,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Add, edit, and restock inventory supplies",
                Font = ThemeHelper.FontBody,
                ForeColor = ThemeHelper.TextMuted,
                AutoSize = true,
                Location = new Point(0, 35)
            };
            this.Controls.Add(lblSubtitle);

            // ── Summary Cards Row ────────────────────────────────────
            var kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(this.Width - 40, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            int totalSupplies = 0, lowStock = 0, totalQuantity = 0;
            decimal totalValue = 0m;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Supplies", conn))
                        totalSupplies = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Supplies WHERE Quantity <= MinimumStock", conn))
                        lowStock = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Quantity), 0) FROM Supplies", conn))
                        totalQuantity = (int)cmd.ExecuteScalar();
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(CAST(Quantity AS DECIMAL(18,2)) * UnitCost), 0) FROM Supplies", conn))
                        totalValue = (decimal)cmd.ExecuteScalar();
                }
            }
            catch { }

            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Items", totalSupplies.ToString(), ThemeHelper.Primary, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Low Stock Items", lowStock.ToString(), ThemeHelper.Warning, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Quantity", totalQuantity.ToString(), ThemeHelper.Info, 220, 100));
            kpiPanel.Controls.Add(ThemeHelper.CreateKpiCard("Total Value", "₱" + totalValue.ToString("N0"), ThemeHelper.Success, 220, 100));

            this.Controls.Add(kpiPanel);

            // ── Form Card ────────────────────────────────────────────
            var cardForm = ThemeHelper.CreateCardPanel();
            cardForm.Location = new Point(0, 195);
            cardForm.Size = new Size(400, 480);
            cardForm.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var lblFormTitle = ThemeHelper.CreateSectionHeader("Supply Details");
            lblFormTitle.Location = new Point(20, 15);
            cardForm.Controls.Add(lblFormTitle);

            int y = 50;
            AddField(cardForm, "Name", ref y, out txtName);
            AddField(cardForm, "Description", ref y, out txtDescription);

            // Category
            var lblCategory = new Label { Text = "Category", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblCategory);
            cmbCategory = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(250, 28), Location = new Point(130, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategory.Items.AddRange(new[] { "Linens", "Bathroom Supplies", "Amenities", "Food & Beverage", "Cleaning Supplies", "Maintenance", "Office Supplies" });
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            cardForm.Controls.Add(cmbCategory);
            y += 34;

            // Quantity
            var lblQuantity = new Label { Text = "Quantity", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblQuantity);
            nudQuantity = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 10000, Minimum = 0, DecimalPlaces = 0, Value = 100 };
            cardForm.Controls.Add(nudQuantity);
            y += 34;

            // Minimum Stock
            var lblMinStock = new Label { Text = "Min Stock Level", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblMinStock);
            nudMinimumStock = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 1000, Minimum = 0, DecimalPlaces = 0, Value = 50 };
            cardForm.Controls.Add(nudMinimumStock);
            y += 34;

            // Unit
            var lblUnit = new Label { Text = "Unit", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblUnit);
            cmbUnit = new ComboBox { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbUnit.Items.AddRange(new[] { "pieces", "rolls", "bottles", "liters", "pods", "boxes", "kg", "packs" });
            if (cmbUnit.Items.Count > 0) cmbUnit.SelectedIndex = 0;
            cardForm.Controls.Add(cmbUnit);
            y += 34;

            // Unit Cost
            var lblCost = new Label { Text = "Unit Cost (₱)", Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            cardForm.Controls.Add(lblCost);
            nudUnitCost = new NumericUpDown { Font = ThemeHelper.FontBody, Size = new Size(120, 28), Location = new Point(130, y), Maximum = 100000, Minimum = 0, DecimalPlaces = 2, Value = 100 };
            cardForm.Controls.Add(nudUnitCost);
            y += 34;

            AddField(cardForm, "Supplier", ref y, out txtSupplier);
            AddField(cardForm, "Location", ref y, out txtLocation);

            // Active
            chkIsActive = new CheckBox { Text = "  Active", Font = ThemeHelper.FontBody, ForeColor = ThemeHelper.TextDark, AutoSize = true, Location = new Point(130, y), Checked = true };
            cardForm.Controls.Add(chkIsActive);
            y += 30;

            // Buttons
            var btnSave = ThemeHelper.CreatePrimaryButton("Save", 100, 38);
            btnSave.Location = new Point(20, y);
            btnSave.Click += BtnSave_Click;
            cardForm.Controls.Add(btnSave);

            var btnRestock = ThemeHelper.CreateSuccessButton("Restock", 100, 38);
            btnRestock.Location = new Point(130, y);
            btnRestock.Click += BtnRestock_Click;
            cardForm.Controls.Add(btnRestock);

            var btnClear = ThemeHelper.CreateSecondaryButton("Clear", 100, 38);
            btnClear.Location = new Point(240, y);
            btnClear.Click += (s, e) => ClearForm();
            cardForm.Controls.Add(btnClear);

            this.Controls.Add(cardForm);

            // ── Grid Card ────────────────────────────────────────────
            var cardGrid = ThemeHelper.CreateCardPanel();
            cardGrid.Location = new Point(420, 195);
            cardGrid.Size = new Size(this.Width - 460, 480);
            cardGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvSupplies = new DataGridView { Dock = DockStyle.Fill };
            ThemeHelper.StyleDataGridView(dgvSupplies);
            dgvSupplies.CellClick += DgvSupplies_CellClick;
            cardGrid.Controls.Add(dgvSupplies);

            var lblGrid = ThemeHelper.CreateSectionHeader("Inventory List");
            lblGrid.Dock = DockStyle.Top;
            cardGrid.Controls.Add(lblGrid);

            this.Controls.Add(cardGrid);
        }

        private void AddField(Panel parent, string label, ref int y, out TextBox txt)
        {
            var lbl = new Label { Text = label, Font = ThemeHelper.FontBodyBold, ForeColor = ThemeHelper.TextMedium, AutoSize = true, Location = new Point(20, y + 4) };
            parent.Controls.Add(lbl);
            txt = new TextBox { Font = ThemeHelper.FontBody, Size = new Size(250, 28), Location = new Point(130, y), BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            y += 36;
        }

        private void RefreshGrid()
        {
            var rows = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Name, Category, Quantity, Unit, MinimumStock, UnitCost, Supplier, Location, IsActive FROM Supplies", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int qty = reader.GetInt32(3);
                        int minStock = reader.GetInt32(5);
                        decimal cost = reader.GetDecimal(6);
                        
                        rows.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Category = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Quantity = qty,
                            Unit = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            MinStock = minStock,
                            Cost = "₱" + cost.ToString("N2"),
                            Value = "₱" + (qty * cost).ToString("N2"),
                            Supplier = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Location = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            Status = reader.GetBoolean(9) ? "Active" : "Inactive",
                            StockStatus = qty <= minStock ? "Low Stock" : "OK"
                        });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading supplies: " + ex.Message); }

            dgvSupplies.DataSource = null;
            dgvSupplies.DataSource = rows;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    if (_editingId.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE Supplies SET Name=@n, Description=@d, Category=@cat, Quantity=@qty, MinimumStock=@ms, Unit=@u, UnitCost=@uc, Supplier=@sup, Location=@loc, IsActive=@ia WHERE Id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _editingId.Value);
                            cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("@d", txtDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@cat", cmbCategory.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@qty", (int)nudQuantity.Value);
                            cmd.Parameters.AddWithValue("@ms", (int)nudMinimumStock.Value);
                            cmd.Parameters.AddWithValue("@u", cmbUnit.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@uc", nudUnitCost.Value);
                            cmd.Parameters.AddWithValue("@sup", txtSupplier.Text.Trim());
                            cmd.Parameters.AddWithValue("@loc", txtLocation.Text.Trim());
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            cmd.ExecuteNonQuery();
                        }
                        INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Updated supply: {txtName.Text.Trim()}", "Manage Supplies");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand("INSERT INTO Supplies (Name, Description, Category, Quantity, MinimumStock, Unit, UnitCost, Supplier, Location, IsActive, CreatedDate, LastRestockedDate) VALUES (@n, @d, @cat, @qty, @ms, @u, @uc, @sup, @loc, @ia, GETDATE(), GETDATE())", conn))
                        {
                            cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("@d", txtDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@cat", cmbCategory.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@qty", (int)nudQuantity.Value);
                            cmd.Parameters.AddWithValue("@ms", (int)nudMinimumStock.Value);
                            cmd.Parameters.AddWithValue("@u", cmbUnit.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@uc", nudUnitCost.Value);
                            cmd.Parameters.AddWithValue("@sup", txtSupplier.Text.Trim());
                            cmd.Parameters.AddWithValue("@loc", txtLocation.Text.Trim());
                            cmd.Parameters.AddWithValue("@ia", chkIsActive.Checked);
                            cmd.ExecuteNonQuery();
                        }
                        INNEASE.CRM.Helpers.AuditLogger.LogAction("Create", $"Added supply: {txtName.Text.Trim()}", "Manage Supplies");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

            ClearForm();
            RefreshGrid();
            MessageBox.Show("Supply saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestock_Click(object sender, EventArgs e)
        {
            if (!_editingId.HasValue)
            {
                MessageBox.Show("Please select a supply to restock.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int addQuantity = (int)nudQuantity.Value;
            if (addQuantity <= 0)
            {
                MessageBox.Show("Please enter a quantity to add.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("UPDATE Supplies SET Quantity = Quantity + @add, LastRestockedDate=GETDATE() WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@add", addQuantity);
                    cmd.Parameters.AddWithValue("@id", _editingId.Value);
                    cmd.ExecuteNonQuery();
                }
                INNEASE.CRM.Helpers.AuditLogger.LogAction("Update", $"Restocked {addQuantity} units for supply ID {_editingId.Value}", "Manage Supplies");
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }

            ClearForm();
            RefreshGrid();
            MessageBox.Show($"Restocked {addQuantity} units of {txtName.Text}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DgvSupplies_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = (int)dgvSupplies.Rows[e.RowIndex].Cells["Id"].Value;
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, Name, Description, Category, Quantity, MinimumStock, Unit, UnitCost, Supplier, Location, IsActive FROM Supplies WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _editingId = reader.GetInt32(0);
                            txtName.Text = reader.GetString(1);
                            txtDescription.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            
                            string cat = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            if (cmbCategory.Items.Contains(cat)) cmbCategory.SelectedItem = cat;
                            
                            nudQuantity.Value = reader.GetInt32(4);
                            nudMinimumStock.Value = reader.GetInt32(5);
                            
                            string unit = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            if (cmbUnit.Items.Contains(unit)) cmbUnit.SelectedItem = unit;

                            nudUnitCost.Value = reader.GetDecimal(7);
                            txtSupplier.Text = reader.IsDBNull(8) ? "" : reader.GetString(8);
                            txtLocation.Text = reader.IsDBNull(9) ? "" : reader.GetString(9);
                            chkIsActive.Checked = reader.GetBoolean(10);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Database error: " + ex.Message); }
        }

        private void ClearForm()
        {
            _editingId = null;
            txtName.Text = txtDescription.Text = "";
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            nudQuantity.Value = 100;
            nudMinimumStock.Value = 50;
            if (cmbUnit.Items.Count > 0) cmbUnit.SelectedIndex = 0;
            nudUnitCost.Value = 100;
            txtSupplier.Text = txtLocation.Text = "";
            chkIsActive.Checked = true;
        }
    }
}
