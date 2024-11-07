using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Api
{
    public static class AuthApi
    {
        // Määritetään reitit rekisteröintiä ja kirjautumista varten
        public static void MapAuthApi(this WebApplication app)
        {
            // Rekisteröintipäätepiste POST-pyynnölle
            app.MapPost("/api/auth/register", RegisterUser);

            // Kirjautumispäätepiste POST-pyynnölle
            app.MapPost("/api/auth/login", LoginUser);
        }

        // Käyttäjän rekisteröinti
        private static async Task<IResult> RegisterUser(UserModel user)
        {
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
            {
                await connection.OpenAsync(); // Avaa yhteys tietokantaan asynkronisesti

                // Tarkista, onko käyttäjä jo olemassa tietokannassa
                var checkUserCommand = connection.CreateCommand();
                checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                checkUserCommand.Parameters.AddWithValue("@Email", user.Email);

                var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
                if (exists)
                {
                    return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." });
                }

                // Hashaa ja tallenna salasana
                user.PasswordHash = HashPassword(user.PasswordHash);

                // Lisää uusi käyttäjä tietokantaan
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Address, PhoneNumber, CreatedDate)
                    VALUES (@Email, @PasswordHash, @FirstName, @LastName, @Address, @PhoneNumber, CURRENT_TIMESTAMP)";
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Address", user.Address);
                command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);

                await command.ExecuteNonQueryAsync();
            }

            return Results.Ok("Rekisteröinti onnistui.");
        }

        // Käyttäjän kirjautuminen
        private static async Task<IResult> LoginUser(UserModel user)
        {
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
            {
                await connection.OpenAsync(); // Avaa tietokantayhteys

                var command = connection.CreateCommand();
                command.CommandText = "SELECT UserID, PasswordHash, FirstName, LastName, Email FROM Users WHERE Email = @Email";
                command.Parameters.AddWithValue("@Email", user.Email);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return Results.Unauthorized(); // Palauttaa unauthorized, jos käyttäjää ei löydy
                    }

                    var storedHash = reader.GetString(1);
                    if (!VerifyPassword(user.PasswordHash, storedHash)) // Tarkistaa salasanan
                    {
                        return Results.Unauthorized();
                    }

                    // Palauttaa käyttäjäobjektin, jos kirjautuminen onnistui
                    var loggedInUser = new UserModel
                    {
                        UserID = reader.GetInt32(0),
                        Email = reader.GetString(4),
                        FirstName = reader.GetString(2),
                        LastName = reader.GetString(3)
                    };
                    return Results.Ok(loggedInUser);
                }
            }
        }

        // Hashaa salasanan SHA-256-algoritmilla
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // Tarkistaa, vastaako annettu salasana tallennettua hashia
        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }
    }
}
