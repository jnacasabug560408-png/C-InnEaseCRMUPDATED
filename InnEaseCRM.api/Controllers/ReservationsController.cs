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
    public class ReservationsController : ControllerBase
    {
        /// <summary>
        /// Get all reservations, optionally filtered by search term
        /// </summary>
        [HttpGet]
        public ActionResult<List<dynamic>> GetAll([FromQuery] string? searchTerm = null)
        {
            var reservations = new List<dynamic>();
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    string query = @"SELECT r.Id, r.GuestId, g.FirstName, g.LastName, r.CheckInDate, r.CheckOutDate, 
                                     r.RoomType, r.Status, r.TotalAmount 
                                     FROM Reservations r 
                                     LEFT JOIN Guests g ON r.GuestId = g.Id";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query += " WHERE g.FirstName LIKE @search OR g.LastName LIKE @search OR r.RoomType LIKE @search";
                    }

                    query += " ORDER BY r.CheckInDate DESC";

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
                                reservations.Add(new
                                {
                                    Id = reader.GetInt32(0),
                                    GuestId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                    GuestName = (reader.IsDBNull(2) ? "" : reader.GetString(2)) + " " + 
                                               (reader.IsDBNull(3) ? "" : reader.GetString(3)),
                                    CheckInDate = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                    CheckOutDate = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                                    RoomType = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    Status = reader.IsDBNull(7) ? "Pending" : reader.GetString(7),
                                    TotalAmount = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving reservations", message = ex.Message });
            }

            return Ok(reservations);
        }

        /// <summary>
        /// Get a specific reservation by ID
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<Reservation> GetById(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand(@"SELECT Id, GuestId, CheckInDate, CheckOutDate, RoomType, Status, TotalAmount 
                                                  FROM Reservations WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Reservation
                            {
                                Id = reader.GetInt32(0),
                                GuestId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                CheckInDate = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                                CheckOutDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                RoomType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Status = reader.IsDBNull(5) ? "Pending" : reader.GetString(5),
                                TotalAmount = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving reservation", message = ex.Message });
            }

            return NotFound();
        }

        /// <summary>
        /// Create a new reservation
        /// </summary>
        [HttpPost]
        public ActionResult Create([FromBody] Reservation reservation)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand(@"INSERT INTO Reservations (GuestId, CheckInDate, CheckOutDate, RoomType, Status, TotalAmount) 
                                                      VALUES (@gid, @cid, @cod, @rt, @status, @amount)", conn))
                    {
                        cmd.Parameters.AddWithValue("@gid", reservation.GuestId);
                        cmd.Parameters.AddWithValue("@cid", reservation.CheckInDate);
                        cmd.Parameters.AddWithValue("@cod", reservation.CheckOutDate);
                        cmd.Parameters.AddWithValue("@rt", reservation.RoomType?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@status", reservation.Status ?? "Pending");
                        cmd.Parameters.AddWithValue("@amount", reservation.TotalAmount);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Reservation created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error creating reservation", message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing reservation
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] Reservation reservation)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand(@"UPDATE Reservations SET GuestId=@gid, CheckInDate=@cid, CheckOutDate=@cod, 
                                                      RoomType=@rt, Status=@status, TotalAmount=@amount WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@gid", reservation.GuestId);
                        cmd.Parameters.AddWithValue("@cid", reservation.CheckInDate);
                        cmd.Parameters.AddWithValue("@cod", reservation.CheckOutDate);
                        cmd.Parameters.AddWithValue("@rt", reservation.RoomType?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@status", reservation.Status ?? "Pending");
                        cmd.Parameters.AddWithValue("@amount", reservation.TotalAmount);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Reservation updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating reservation", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a reservation by ID
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("DELETE FROM Reservations WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Reservation deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting reservation", message = ex.Message });
            }
        }

        /// <summary>
        /// Update reservation status
        /// </summary>
        [HttpPatch("{id}/status")]
        public ActionResult UpdateStatus(int id, [FromBody] string status)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    using (var cmd = new SqlCommand("UPDATE Reservations SET Status=@status WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "Reservation status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating reservation status", message = ex.Message });
            }
        }
    }
}
