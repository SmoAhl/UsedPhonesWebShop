using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using Shared.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Api
{
    public static class AccountManagerApi
    {
        /// <summary>
        /// Maps the Account Manager API endpoints.
        /// </summary>
        public static void MapAccountManagerApi(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth");
            group.MapPost("/register", RegisterUser); // No Authorize attribute
            group.MapGet("/users", GetAllUsers).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); // Admin only
            group.MapDelete("/users/{id}", DeleteUser).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); // Admin only
            group.MapPut("/updateuser/{id}", UpdateUser).RequireAuthorization(); // Requires authorization
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="registerRequest">The register request model.</param>
        /// <returns>The registration result.</returns>
        public static async Task<IResult> RegisterUser(RegisterRequest registerRequest)
        {
            try
            {
                Console.WriteLine($"Registration attempt for user: {registerRequest.Email}");
                if (string.IsNullOrWhiteSpace(registerRequest.Password))
                {
                    Console.WriteLine("Password is empty or null.");
                    return Results.BadRequest(new { Error = "Salasana ei voi olla tyhjä." });
                }

                if (string.IsNullOrWhiteSpace(registerRequest.Email) ||
                    string.IsNullOrWhiteSpace(registerRequest.FirstName) ||
                    string.IsNullOrWhiteSpace(registerRequest.LastName) ||
                    string.IsNullOrWhiteSpace(registerRequest.Address) ||
                    string.IsNullOrWhiteSpace(registerRequest.PhoneNumber))
                {
                    Console.WriteLine("One or more fields are empty.");
                    return Results.BadRequest(new { Error = "Kaikki kentät ovat pakollisia." });
                }

                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();
                Console.WriteLine("Database connection opened successfully.");

                var checkUserCommand = connection.CreateCommand();
                checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                checkUserCommand.Parameters.AddWithValue("@Email", registerRequest.Email);

                var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
                if (exists)
                {
                    Console.WriteLine($"User already exists: {registerRequest.Email}");
                    return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." });
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);
                Console.WriteLine("Password hashing successful.");

                var insertUserCommand = connection.CreateCommand();
                insertUserCommand.CommandText = @"
                    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Address, PhoneNumber, Role) 
                    VALUES (@Email, @PasswordHash, @FirstName, @LastName, @Address, @PhoneNumber, 'Customer')";
                insertUserCommand.Parameters.AddWithValue("@Email", registerRequest.Email);
                insertUserCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                insertUserCommand.Parameters.AddWithValue("@FirstName", registerRequest.FirstName);
                insertUserCommand.Parameters.AddWithValue("@LastName", registerRequest.LastName);
                insertUserCommand.Parameters.AddWithValue("@Address", registerRequest.Address);
                insertUserCommand.Parameters.AddWithValue("@PhoneNumber", registerRequest.PhoneNumber);

                Console.WriteLine("Inserting user into database.");
                await insertUserCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"User registered successfully: {registerRequest.Email}");
                return Results.Ok(new { Message = "Rekisteröinti onnistui." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                return Results.Problem("Rekisteröinti epäonnistui.");
            }
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <returns>The list of users.</returns>
        [Authorize(Roles = "Admin")]
        public static async Task<IResult> GetAllUsers()
        {
            try
            {
                Console.WriteLine("Fetching all users.");
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection opened successfully.");

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
                                FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                LastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                                PhoneNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CreatedDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7)
                            });
                        }
                    }

                    Console.WriteLine("Users fetched successfully.");
                    return Results.Ok(users);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex.Message}");
                return Results.Problem($"Virhe käyttäjien hakemisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <param name="updateRequest">The updated user request model.</param>
        /// <returns>The update result.</returns>
        [Authorize]
        public static async Task<IResult> UpdateUser(int id, UpdateUserRequest updateRequest, HttpContext context)
        {
            try
            {
                var user = context.User;
                if (user.Identity == null || !user.Identity.IsAuthenticated)
                {
                    Console.WriteLine("Unauthorized access attempt.");
                    return Results.Unauthorized();
                }

                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    Console.WriteLine("User ID claim not found.");
                    return Results.Unauthorized();
                }

                int userId = int.Parse(userIdClaim.Value);
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin" && userId != id)
                {
                    Console.WriteLine("Unauthorized access attempt.");
                    return Results.Unauthorized();
                }

                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();
                Console.WriteLine("Database connection opened successfully.");

                var command = connection.CreateCommand();
                command.CommandText = "SELECT UserID, Email, Role, PasswordHash, FirstName, LastName, Address, PhoneNumber FROM Users WHERE UserID = @UserID";
                command.Parameters.AddWithValue("@UserID", id);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    Console.WriteLine($"User not found: {id}");
                    return Results.NotFound($"Käyttäjää ID:llä {id} ei löytynyt.");
                }

                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                    UPDATE Users SET 
                        Email = @Email,
                        Role = @Role,
                        PasswordHash = @PasswordHash,
                        FirstName = @FirstName,
                        LastName = @LastName,
                        Address = @Address,
                        PhoneNumber = @PhoneNumber
                    WHERE UserID = @UserID";

                updateCommand.Parameters.AddWithValue("@Email", updateRequest.Email ?? reader.GetString(1));
                updateCommand.Parameters.AddWithValue("@Role", updateRequest.Role ?? reader.GetString(2));
                updateCommand.Parameters.AddWithValue("@PasswordHash", string.IsNullOrWhiteSpace(updateRequest.Password) ? reader.GetString(3) : BCrypt.Net.BCrypt.HashPassword(updateRequest.Password));
                updateCommand.Parameters.AddWithValue("@FirstName", updateRequest.FirstName ?? reader.GetString(4));
                updateCommand.Parameters.AddWithValue("@LastName", updateRequest.LastName ?? reader.GetString(5));
                updateCommand.Parameters.AddWithValue("@Address", updateRequest.Address ?? reader.GetString(6));
                updateCommand.Parameters.AddWithValue("@PhoneNumber", updateRequest.PhoneNumber ?? reader.GetString(7));
                updateCommand.Parameters.AddWithValue("@UserID", id);

                await updateCommand.ExecuteNonQueryAsync();
                Console.WriteLine("User updated successfully.");
                return Results.Ok("Käyttäjän tiedot päivitetty onnistuneesti.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return Results.Problem($"Virhe käyttäjän päivittämisessä: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>The result of the deletion.</returns>
        [Authorize(Roles = "Admin")]
        public static async Task<IResult> DeleteUser(int id)
        {
            try
            {
                Console.WriteLine($"Deleting user ID: {id}");
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();
                Console.WriteLine("Database connection opened successfully.");

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Users WHERE UserID = @UserID";
                command.Parameters.AddWithValue("@UserID", id);

                var result = await command.ExecuteNonQueryAsync();
                if (result == 0)
                {
                    Console.WriteLine("User not found.");
                    return Results.NotFound("Käyttäjää ei löytynyt.");
                }

                Console.WriteLine("User deleted successfully.");
                return Results.Ok("Käyttäjä poistettu onnistuneesti.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return Results.Problem($"Virhe käyttäjän poistamisessa: {ex.Message}");
            }
        }
    }
}