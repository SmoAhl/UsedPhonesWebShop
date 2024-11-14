using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using Shared.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Api
{
    public static class AuthApi
    {
        // Määritetään reitit rekisteröintiä, kirjautumista ja uloskirjautumista varten
        public static void MapAuthApi(this WebApplication app)
        {
            // Rekisteröintipäätepiste, POST-pyyntö käyttäjän rekisteröimiseksi
            app.MapPost("/api/auth/register", RegisterUser);

            // Kirjautumispäätepiste, POST-pyyntö käyttäjän kirjautumiseksi
            app.MapPost("/api/auth/login", LoginUser);

            // Uloskirjautumispäätepiste, POST-pyyntö käyttäjän uloskirjaamiseksi
            app.MapPost("/api/auth/logout", LogoutUser);
        }

        // Metodi käyttäjän rekisteröimiseksi
        private static async Task<IResult> RegisterUser(UserModel user)
        {
            try
            {
                // Avaa yhteys SQLite-tietokantaan asynkronisesti "using"-lohkon avulla, joka sulkee yhteyden automaattisesti
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa tietokantayhteys asynkronisesti

                    // Tarkista, onko käyttäjä jo olemassa tietokannassa annetun sähköpostiosoitteen perusteella
                    var checkUserCommand = connection.CreateCommand();
                    checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    checkUserCommand.Parameters.AddWithValue("@Email", user.Email); // Parametrien avulla ehkäistään SQL-injektiohyökkäykset

                    // Suoritetaan kysely ja tarkistetaan, onko käyttäjä jo olemassa
                    var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
                    if (exists)
                    {
                        return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." }); // Palautetaan virheviesti, jos käyttäjä on jo rekisteröitynyt
                    }

                    // Hashataan käyttäjän salasana turvallisuuden parantamiseksi
                    user.PasswordHash = HashPassword(user.PasswordHash);

                    // Luodaan komento uuden käyttäjän lisäämiseksi tietokantaan
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

                    await command.ExecuteNonQueryAsync(); // Suoritetaan SQL-komento tietokantaan
                }

                return Results.Ok("Rekisteröinti onnistui."); // Palautetaan onnistumisviesti rekisteröinnin onnistuessa
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän rekisteröinnissä: {ex.Message}"); // Palautetaan virheviesti, jos rekisteröinnissä tapahtuu virhe
            }
        }

        // Metodi käyttäjän kirjautumiseksi sisään
        private static async Task<IResult> LoginUser(UserModel user, HttpContext context)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa tietokantayhteys asynkronisesti

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT UserID, PasswordHash, Role FROM Users WHERE Email = @Email";
                    command.Parameters.AddWithValue("@Email", user.Email); // Lisätään käyttäjän sähköposti parametriin
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            return Results.Unauthorized(); // Palauttaa Unauthorized, jos käyttäjää ei löydy
                        }
                        var storedHash = reader.GetString(1); // Haetaan tietokannasta tallennettu salasana-hash
                        if (!VerifyPassword(user.PasswordHash, storedHash)) // Tarkistetaan, vastaako syötetty salasana tietokantaan tallennettua hashia
                        {
                            return Results.Unauthorized(); // Palauttaa Unauthorized, jos salasanat eivät täsmää
                        }
                        var userId = reader.GetInt32(0); // Haetaan käyttäjän UserID
                        var role = reader.GetString(2); // Haetaan käyttäjän rooli

                        // Asetetaan sessio, jotta käyttäjän kirjautumistila voidaan ylläpitää eri sivujen välillä
                        context.Session.SetInt32("UserID", userId); // Tallennetaan käyttäjän ID istuntotietoihin
                        context.Session.SetString("Role", role); // Tallennetaan käyttäjän rooli istuntoon

                        return Results.Ok(new { Message = "Kirjautuminen onnistui." }); // Palautetaan onnistumisviesti kirjautumisen onnistuessa
                    }
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe käyttäjän kirjautumisessa: {ex.Message}"); // Palautetaan virheviesti, jos kirjautumisessa tapahtuu virhe
            }
        }

        // Metodi käyttäjän uloskirjautumiseksi
        private static IResult LogoutUser(HttpContext context)
        {
            try
            {
                context.Session.Clear(); // Tyhjennetään kaikki istuntotiedot, jolloin käyttäjä kirjautuu ulos
                return Results.Ok("Kirjauduttu ulos."); // Palautetaan onnistumisviesti
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe uloskirjautumisessa: {ex.Message}"); // Palautetaan virheviesti, jos uloskirjautumisessa tapahtuu virhe
            }
        }

        // Salasanan hashauksen toteuttava metodi SHA-256-algoritmilla
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create(); // Luodaan uusi SHA256-instanssi
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password)); // Muutetaan salasana tavutauluksi ja hashataan se
            return Convert.ToBase64String(bytes); // Muutetaan hash Base64-muotoon ja palautetaan se
        }

        // Metodi tarkistamaan, vastaako annettu salasana tallennettua hashia
        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword); // Hashataan käyttäjän syöttämä salasana
            return enteredHash == storedHash; // Palautetaan true, jos hashit täsmäävät
        }

    }
}