using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using InnEase.Infrastructure.Data;
using InnEase.Domain.Models;

namespace INNEASE.CRM.Controllers
{
    /// <summary>
    /// Controller for managing reservation operations (CRUD)
    /// </summary>
    public class ReservationController
    {
        /// <summary>
        /// Get all reservations, optionally filtered by search term
        /// </summary>
        public List<dynamic> GetAllReservations(string searchTerm = null)
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
                throw new Exception("Error retrieving reservations: " + ex.Message, ex);
            }

            return reservations;
        }

        /// <summary>
        /// Get a specific reservation by ID
        /// </summary>
        public Reservation GetReservationById(int id)
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
                            return new Reservation
                            {
                                Id = reader.GetInt32(0),
                                GuestId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                CheckInDate = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                                CheckOutDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                RoomType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Status = reader.IsDBNull(5) ? "Pending" : reader.GetString(5),
                                TotalAmount = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving reservation: " + ex.Message, ex);
            }

            return null;
        }

        /// <summary>
        /// Create a new reservation
        /// </summary>
        public void CreateReservation(Reservation reservation)
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating reservation: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update an existing reservation
        /// </summary>
        public void UpdateReservation(Reservation reservation)
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
                        cmd.Parameters.AddWithValue("@id", reservation.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating reservation: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete a reservation by ID
        /// </summary>
        public void DeleteReservation(int id)
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting reservation: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update reservation status
        /// </summary>
        public void UpdateReservationStatus(int id, string status)
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating reservation status: " + ex.Message, ex);
            }
        }
    }
}
