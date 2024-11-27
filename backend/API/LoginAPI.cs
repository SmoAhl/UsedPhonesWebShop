using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.DTOs;

namespace Backend.Api
{
    public static class LoginApi
    {
        public static void MapLoginApi(this WebApplication app)
        {
            app.MapPost("/api/auth/login", LoginUser);
            app.MapPost("/api/auth/logout", LogoutUser);
            app.MapGet("/api/auth/currentuser", GetCurrentUser)
               .RequireAuthorization();
        }

        private static async Task<IResult> LoginUser(LoginRequest loginRequest, HttpContext context)
        {
            try
            {
                Console.WriteLine($"Login attempt for user: {loginRequest.Email}");
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();
                Console.WriteLine("Database connection opened successfully.");

                var command = connection.CreateCommand();
                command.CommandText = "SELECT UserID, PasswordHash, Role FROM Users WHERE Email = @Email";
                command.Parameters.AddWithValue("@Email", loginRequest.Email);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    Console.WriteLine("User not found.");
                    return Results.Unauthorized();
                }

                var storedHash = reader.GetString(1);
                Console.WriteLine($"Stored Hash: {storedHash}");
                Console.WriteLine($"Entered Hash: {BCrypt.Net.BCrypt.HashPassword(loginRequest.Password)}");

                if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, storedHash))
                {
                    Console.WriteLine("Invalid password.");
                    return Results.Unauthorized();
                }

                var userId = reader.GetInt32(0);
                var role = reader.GetString(2);
                Console.WriteLine($"User authenticated: {loginRequest.Email}, Role: {role}");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(jwtKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, loginRequest.Email),
                        new Claim(ClaimTypes.Role, role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                Console.WriteLine("Token generated successfully.");
                return Results.Ok(new { Token = tokenString, Expiration = tokenDescriptor.Expires });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return Results.Problem($"Error during login: {ex.Message}");
            }
        }

        private static IResult LogoutUser()
        {
            Console.WriteLine("User logged out.");
            return Results.Ok("Kirjauduttu ulos.");
        }

        private static async Task<IResult> GetCurrentUser(HttpContext context)
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
            Console.WriteLine($"Fetching data for user ID: {userId}");

            using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
            await connection.OpenAsync();
            Console.WriteLine("Database connection opened successfully.");

            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserID, Email, Role, FirstName, LastName, Address, PhoneNumber FROM Users WHERE UserID = $id";
            command.Parameters.AddWithValue("$id", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var userModel = new UserModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                };

                Console.WriteLine("User data retrieved successfully.");
                return Results.Ok(userModel);
            }

            Console.WriteLine("User not found.");
            return Results.Unauthorized();
        }

        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            Console.WriteLine($"Stored Hash: {storedHash}");
            Console.WriteLine($"Entered Hash: {BCrypt.Net.BCrypt.HashPassword(enteredPassword)}");

            bool isValid = BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
            if (!isValid)
            {
                Console.WriteLine("Error during login: Invalid salt version");
            }
            return isValid;
        }

        private class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        private static string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                                     ?? "Your_Secret_Key_Should_Be_Long_Enough";
    }
}