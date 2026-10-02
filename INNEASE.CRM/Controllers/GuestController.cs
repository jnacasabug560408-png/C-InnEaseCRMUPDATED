using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using InnEase.Infrastructure.Data;
using InnEase.Domain.Models;

namespace INNEASE.CRM.Controllers
{
    /// <summary>
    /// Controller for managing guest operations (CRUD)
    /// </summary>
    public class GuestController
    {
        /// <summary>
        /// Get all guests, optionally filtered by search term
        /// </summary>
        public List<dynamic> GetAllGuests(string searchTerm = null)
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
                throw new Exception("Error retrieving guests: " + ex.Message, ex);
            }

            return guests;
        }

        /// <summary>
        /// Get a specific guest by ID
        /// </summary>
        public Guest GetGuestById(int id)
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
                            return new Guest
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
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving guest: " + ex.Message, ex);
            }

            return null;
        }

        /// <summary>
        /// Create a new guest
        /// </summary>
        public void CreateGuest(Guest guest)
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating guest: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update an existing guest
        /// </summary>
        public void UpdateGuest(Guest guest)
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
                        cmd.Parameters.AddWithValue("@id", guest.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating guest: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete a guest by ID
        /// </summary>
        public void DeleteGuest(int id)
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting guest: " + ex.Message, ex);
            }
        }
    }
}
