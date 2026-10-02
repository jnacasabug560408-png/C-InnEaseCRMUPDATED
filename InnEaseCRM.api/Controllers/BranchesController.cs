using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using InnEase.Domain.Models;
using InnEase.Infrastructure.Data;

namespace InnEaseCRM.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        /// <summary>
        /// Get all branches, optionally filtered by search term
        /// </summary>
        [HttpGet]
        public ActionResult<List<dynamic>> GetAll([FromQuery] string? searchTerm = null)
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
                return StatusCode(500, new { error = "Error retrieving branches", message = ex.Message });
            }

            return Ok(branches);
        }

        /// <summary>
        /// Get a specific branch by ID
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<Branch> GetById(int id)
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
                            return Ok(new Branch
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
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving branch", message = ex.Message });
            }

            return NotFound();
        }

        /// <summary>
        /// Create a new branch
        /// </summary>
        [HttpPost]
        public ActionResult Create([FromBody] Branch branch)
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
                return Ok(new { message = "Branch created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error creating branch", message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing branch
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] Branch branch)
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
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Branch updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating branch", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a branch by ID
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
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
                return Ok(new { message = "Branch deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting branch", message = ex.Message });
            }
        }

        /// <summary>
        /// Update branch status (Active/Inactive)
        /// </summary>
        [HttpPatch("{id}/status")]
        public ActionResult UpdateStatus(int id, [FromBody] bool isActive)
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
                return Ok(new { message = "Branch status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating branch status", message = ex.Message });
            }
        }
    }
}
