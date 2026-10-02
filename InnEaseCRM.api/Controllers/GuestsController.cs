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
    public class GuestsController : ControllerBase
    {
        /// <summary>
        /// Get all guests, optionally filtered by search term
        /// </summary>
        [HttpGet]
        public ActionResult<List<dynamic>> GetAll([FromQuery] string? searchTerm = null)
        {
            var guests = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string query = "SELECT Id, FirstName, LastName, Email, Phone, Address, Nationality, IsVIP, LoyaltyPoints FROM Guests";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query += " WHERE FirstName LIKE @search OR LastName LIKE @search OR Email LIKE @search";
                    }

                    query += " ORDER BY LastName, FirstName";

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
                                guests.Add(new
                                {
                                    Id = reader.GetInt32(0),
                                    FirstName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    LastName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Address = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Nationality = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    IsVIP = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                                    LoyaltyPoints = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving guests", message = ex.Message });
            }

            return Ok(guests);
        }

        /// <summary>
        /// Get a specific guest by ID
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<Guest> GetById(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("SELECT Id, FirstName, LastName, Email, Phone, Address, Nationality, IsVIP, LoyaltyPoints, DateRegistered, Notes FROM Guests WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Guest
                            {
                                Id = reader.GetInt32(0),
                                FirstName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                LastName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Address = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Nationality = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                IsVIP = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                                LoyaltyPoints = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                                DateRegistered = reader.IsDBNull(9) ? DateTime.Now : reader.GetDateTime(9),
                                Notes = reader.IsDBNull(10) ? "" : reader.GetString(10)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving guest", message = ex.Message });
            }

            return NotFound();
        }

        /// <summary>
        /// Create a new guest
        /// </summary>
        [HttpPost]
        public ActionResult Create([FromBody] Guest guest)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("INSERT INTO Guests (FirstName, LastName, Email, Phone, Address, Nationality, IsVIP, LoyaltyPoints, DateRegistered, Notes) VALUES (@fn, @ln, @email, @phone, @addr, @nat, @vip, @points, @reg, @notes)", conn))
                    {
                        cmd.Parameters.AddWithValue("@fn", guest.FirstName?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@ln", guest.LastName?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@email", guest.Email?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@phone", guest.Phone?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@addr", guest.Address?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@nat", guest.Nationality?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@vip", guest.IsVIP);
                        cmd.Parameters.AddWithValue("@points", guest.LoyaltyPoints);
                        cmd.Parameters.AddWithValue("@reg", guest.DateRegistered);
                        cmd.Parameters.AddWithValue("@notes", guest.Notes?.Trim() ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Guest created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error creating guest", message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing guest
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] Guest guest)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("UPDATE Guests SET FirstName=@fn, LastName=@ln, Email=@email, Phone=@phone, Address=@addr, Nationality=@nat, IsVIP=@vip, LoyaltyPoints=@points, Notes=@notes WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@fn", guest.FirstName?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@ln", guest.LastName?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@email", guest.Email?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@phone", guest.Phone?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@addr", guest.Address?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@nat", guest.Nationality?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@vip", guest.IsVIP);
                        cmd.Parameters.AddWithValue("@points", guest.LoyaltyPoints);
                        cmd.Parameters.AddWithValue("@notes", guest.Notes?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Guest updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating guest", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a guest by ID
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("DELETE FROM Guests WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Guest deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting guest", message = ex.Message });
            }
        }
    }
}
