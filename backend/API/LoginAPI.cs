using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Api
{
    public static class LoginApi
    {
        /// <summary>
        /// Maps the Login API endpoints.
        /// </summary>
        public static void MapLoginApi(this WebApplication app)
        {
            app.MapPost("/api/auth/login", LoginUser);
            app.MapPost("/api/auth/logout", LogoutUser);
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        /// <param name="user">The user model.</param>
        /// <returns>The login result.</returns>
        private static async Task<IResult> LoginUser(UserModel user)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, PasswordHash, Role FROM Users WHERE Email = @Email";
                    command.Parameters.AddWithValue("@Email", user.Email);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            return Results.Unauthorized();
                        }

                        var storedHash = reader.GetString(1);
                        if (!VerifyPassword(user.PasswordHash, storedHash))
                        {
                            return Results.Unauthorized();
                        }

                        var userId = reader.GetInt32(0);
                        var role = reader.GetString(2);

                        return Results.Ok(new { Message = "Kirjautuminen onnistui." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän kirjautumisessa: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs out a user.
        /// </summary>
        /// <returns>The logout result.</returns>
        private static IResult LogoutUser()
        {
            return Results.Ok("Kirjauduttu ulos.");
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

        /// <summary>
        /// Verifies a password.
        /// </summary>
        /// <param name="enteredPassword">The entered password.</param>
        /// <param name="storedHash">The stored hash.</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }
    }
}