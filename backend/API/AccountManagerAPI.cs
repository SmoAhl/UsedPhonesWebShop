using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using Shared.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Api
{
    public static class AccountManagerApi
    {
        /// <summary>
        /// Maps the Account Manager API endpoints.
        /// </summary>
        public static void MapAccountManagerApi(this WebApplication app)
        {
            app.MapPost("/api/auth/register", RegisterUser);
            app.MapGet("/api/auth/users", GetAllUsers);
            app.MapDelete("/api/auth/users/{id}", DeleteUser);
            app.MapPut("/api/auth/updateuser/{id}", UpdateUser);
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="user">The user model.</param>
        /// <returns>The registration result.</returns>
        public static async Task<IResult> RegisterUser(UserModel user)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var checkUserCommand = connection.CreateCommand();
                    checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    checkUserCommand.Parameters.AddWithValue("@Email", user.Email);

                    var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
                    if (exists)
                    {
                        return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." });
                    }

                    user.PasswordHash = HashPassword(user.PasswordHash);

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Users (Email, Role, PasswordHash, FirstName, LastName, Address, PhoneNumber, CreatedDate)
                        VALUES (@Email, @Role, @PasswordHash, @FirstName, @LastName, @Address, @PhoneNumber, CURRENT_TIMESTAMP)";
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Address", user.Address);
                    command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);

                    await command.ExecuteNonQueryAsync();
                }

                return Results.Ok("Rekisteröinti onnistui.");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän rekisteröinnissä: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <returns>The list of users.</returns>
        public static async Task<IResult> GetAllUsers()
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, Email, Role, FirstName, LastName, Address, PhoneNumber, CreatedDate FROM Users";

                    var users = new List<UserModel>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new UserModel
                            {
                                UserID = reader.GetInt32(0),
                                Email = reader.GetString(1),
                                Role = reader.GetString(2),
                                FirstName = reader.GetString(3),
                                LastName = reader.GetString(4),
                                Address = reader.GetString(5),
                                PhoneNumber = reader.GetString(6),
                                CreatedDate = reader.GetDateTime(7)
                            });
                        }
                    }

                    return Results.Ok(users);
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjien hakemisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <param name="updatedUser">The updated user model.</param>
        /// <returns>The update result.</returns>
        public static async Task<IResult> UpdateUser(int id, UpdateUserModel updatedUser)
        {
            if (updatedUser == null)
            {
                return Results.BadRequest("Päivityspyyntö on virheellinen.");
            }

            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, Email, Role, PasswordHash, FirstName, LastName, Address, PhoneNumber FROM Users WHERE UserID = @UserID";
                    command.Parameters.AddWithValue("@UserID", id);

                    using var reader = await command.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return Results.NotFound($"Käyttäjää ID:llä {id} ei löytynyt.");
                    }

                    var user = new UserModel
                    {
                        UserID = reader.GetInt32(0),
                        Email = reader.GetString(1),
                        Role = reader.GetString(2),
                        PasswordHash = reader.GetString(3),
                        FirstName = reader.GetString(4),
                        LastName = reader.GetString(5),
                        Address = reader.GetString(6),
                        PhoneNumber = reader.GetString(7)
                    };

                    user.Email = updatedUser.Email ?? user.Email;
                    user.Role = updatedUser.Role ?? user.Role;
                    user.PasswordHash = updatedUser.PasswordHash != null ? HashPassword(updatedUser.PasswordHash) : user.PasswordHash;
                    user.FirstName = updatedUser.FirstName ?? user.FirstName;
                    user.LastName = updatedUser.LastName ?? user.LastName;
                    user.Address = updatedUser.Address ?? user.Address;
                    user.PhoneNumber = updatedUser.PhoneNumber ?? user.PhoneNumber;

                    command = connection.CreateCommand();
                    command.CommandText = @"
                    UPDATE Users
                    SET Email = @Email, Role = @Role, PasswordHash = @PasswordHash, FirstName = @FirstName, LastName = @LastName, Address = @Address, PhoneNumber = @PhoneNumber
                    WHERE UserID = @UserID";

                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Address", user.Address);
                    command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                    command.Parameters.AddWithValue("@UserID", id);

                    await command.ExecuteNonQueryAsync();
                    return Results.Ok(user);
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän tietojen päivittämisessä: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>The result of the deletion.</returns>
        public static async Task<IResult> DeleteUser(int id)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Users WHERE UserID = @UserID";
                    command.Parameters.AddWithValue("@UserID", id);

                    var result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        return Results.NotFound("Käyttäjää ei löytynyt.");
                    }

                    return Results.Ok("Käyttäjä poistettu onnistuneesti.");
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän poistamisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Hashes a password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>The hashed password.</returns>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}