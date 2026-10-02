using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using InnEase.Infrastructure.Data;
using InnEase.Domain.Models;

namespace INNEASE.CRM.Controllers
{
    /// <summary>
    /// Controller for managing branch operations (CRUD)
    /// </summary>
    public class BranchController
    {
        /// <summary>
        /// Get all branches, optionally filtered by search term
        /// </summary>
        public List<dynamic> GetAllBranches(string searchTerm = null)
        {
            var branches = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string query = "SELECT Id, BranchName, BranchCode, Address, City, State, Country, Phone, Email, IsActive FROM Branches";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query += " WHERE BranchName LIKE @search OR BranchCode LIKE @search OR City LIKE @search";
                    }

                    query += " ORDER BY BranchName";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                branches.Add(new
                                {
                                    Id = reader.GetInt32(0),
                                    BranchName = reader.GetString(1),
                                    BranchCode = reader.GetString(2),
                                    Address = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    City = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    State = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Country = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    Phone = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    Email = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    Status = reader.GetBoolean(9) ? "Active" : "Inactive"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving branches: " + ex.Message, ex);
            }

            return branches;
        }

        /// <summary>
        /// Get a specific branch by ID
        /// </summary>
        public Branch GetBranchById(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, BranchName, BranchCode, Address, City, State, Country, Phone, Email, IsActive FROM Branches WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Branch
                            {
                                Id = reader.GetInt32(0),
                                BranchName = reader.GetString(1),
                                BranchCode = reader.GetString(2),
                                Address = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                City = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                State = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Country = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Phone = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Email = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                IsActive = reader.GetBoolean(9)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving branch: " + ex.Message, ex);
            }

            return null;
        }

        /// <summary>
        /// Create a new branch
        /// </summary>
        public void CreateBranch(Branch branch)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("INSERT INTO Branches (BranchName, BranchCode, Address, City, State, Country, Phone, Email, IsActive) VALUES (@bn, @bc, @addr, @city, @state, @country, @phone, @email, @ia)", conn))
                    {
                        cmd.Parameters.AddWithValue("@bn", branch.BranchName.Trim());
                        cmd.Parameters.AddWithValue("@bc", branch.BranchCode.Trim());
                        cmd.Parameters.AddWithValue("@addr", branch.Address?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@city", branch.City?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@state", branch.State?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@country", branch.Country?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@phone", branch.Phone?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@email", branch.Email?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@ia", branch.IsActive);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating branch: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update an existing branch
        /// </summary>
        public void UpdateBranch(Branch branch)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("UPDATE Branches SET BranchName=@bn, BranchCode=@bc, Address=@addr, City=@city, State=@state, Country=@country, Phone=@phone, Email=@email, IsActive=@ia WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@bn", branch.BranchName.Trim());
                        cmd.Parameters.AddWithValue("@bc", branch.BranchCode.Trim());
                        cmd.Parameters.AddWithValue("@addr", branch.Address?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@city", branch.City?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@state", branch.State?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@country", branch.Country?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@phone", branch.Phone?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@email", branch.Email?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@ia", branch.IsActive);
                        cmd.Parameters.AddWithValue("@id", branch.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating branch: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete a branch by ID
        /// </summary>
        public void DeleteBranch(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("DELETE FROM Branches WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting branch: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update branch status (Active/Inactive)
        /// </summary>
        public void UpdateBranchStatus(int id, bool isActive)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("UPDATE Branches SET IsActive=@ia WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@ia", isActive);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating branch status: " + ex.Message, ex);
            }
        }
    }
}
