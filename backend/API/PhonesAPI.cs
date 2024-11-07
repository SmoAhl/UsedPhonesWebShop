using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api
{
    public static class PhonesApi
    {
        // Määritetään API-reitit sovelluksen käynnistyksen yhteydessä
        public static void MapPhonesApi(this WebApplication app)
        {
            // Määrittää GET-pyynnön reitille /api/phones, joka palauttaa kaikki puhelimet tietokannasta JSON-muodossa
            app.MapGet("/api/phones", GetPhones);

            // Määrittää POST-pyynnön reitille /api/phones, joka lisää uuden puhelimen tietokantaan
            app.MapPost("/api/phones", AddPhone);

            // Määrittää DELETE-pyynnön reitille /api/phones/{id}, joka poistaa puhelimen tietokannasta annettuun id:hen perustuen
            app.MapDelete("/api/phones/{id}", DeletePhone);

            // PATCH-pyyntö reitille /api/phones/{id}, joka päivittää annettuja kenttiä puhelimen tiedoissa
            app.MapPatch("/api/phones/{id}", UpdatePhonePartial);
        }

        // GetPhones metodi, joka hakee kaikki puhelimet tietokannasta ja palauttaa ne JSON-muodossa
        //
        // Tämä logiikka mahdollistaa kaikkien puhelimien noutamisen kerralla, jolloin niitä voidaan näyttää
        // käyttöliittymässä esimerkiksi listana. Asynkroninen käsittely mahdollistaa tehokkaan tietokantayhteyden
        // hallinnan, ja JSON-muoto mahdollistaa tiedon sujuvan siirron web-ympäristössä.

        private static async Task<IResult> GetPhones()
        {
            var phones = new List<PhoneModel>(); // Lista, johon tallennetaan haetut puhelimet

            try
            {
                // Avaa tietokantayhteys käyttämällä asynkronista yhteyden avausta
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa yhteyden tietokantaan asynkronisesti

                    // Luodaan komento, joka suorittaa SQL-kyselyn puhelintietojen hakemiseksi
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT PhoneID, Brand, Model, Price, Description, Condition, StockQuantity FROM Phones";

                    // Suorittaa kyselyn asynkronisesti ja käsittelee tuloksia readerin avulla
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) // Käy läpi jokaisen tietokannasta haetun rivin
                    {
                        // Lisää uusi PhoneModel-olio puhelinlistaan, täyttäen tiedot haetuista arvoista.
                        // Huomaa: Indeksit vastaavat SQL-kyselyssä määritettyjen sarakkeiden järjestystä.
                        // Jos muutamme kyselyn sarakkeiden järjestystä, tulee vastaavasti muuttaa indeksien paikkoja vastaamaan uutta järjestystä.
                        phones.Add(new PhoneModel
                        {
                            PhoneID = reader.GetInt32(0),       // Haetaan PhoneID-kentän arvo (index 0)
                            Brand = reader.GetString(1),       // Haetaan Brand-kentän arvo (index 1)
                            Model = reader.GetString(2),       // Haetaan Model-kentän arvo (index 2)
                            Price = reader.GetDecimal(3),      // Haetaan Price-kentän arvo (index 3)
                            Description = reader.GetString(4), // Haetaan Description-kentän arvo (index 4)
                            Condition = reader.GetString(5),   // Haetaan Condition-kentän arvo (index 5)
                            StockQuantity = reader.GetInt32(6) // Haetaan StockQuantity-kentän arvo (index 6)
                        });
                    }
                }
                return Results.Ok(phones); // Palauttaa listan haetuista puhelimista JSON-muodossa.
            }
            catch (Exception ex) // Jos virhe ilmenee, ohjataan virhe tähän kohtaan
            {
                // Palautetaan yleinen virheviesti, jos haku epäonnistuu
                return Results.Problem($"Virhe puhelinten hakemisessa: {ex.Message}");
            }
        }

        // AddPhone metodi, joka lisää uuden puhelimen tietokantaan ja palauttaa luodun puhelimen JSON-muodossa
        private static async Task<IResult> AddPhone(PhoneModel phone)
        {
            // Tarkistetaan, että puhelimen pakolliset kentät on täytetty ja hinta on suurempi kuin 0
            if (string.IsNullOrEmpty(phone.Brand) || string.IsNullOrEmpty(phone.Model) || phone.Price <= 0 || phone.StockQuantity <= 0)
            {
                // Palautetaan virheviesti, jos tarkistukset epäonnistuvat
                return Results.BadRequest("Kaikki kentät eivät ole täytettyjä tai arvo on virheellinen.");
            }

            try
            {
                // Avataan tietokantayhteys SQLite-tietokantaan "using"-lohkon sisällä, joka varmistaa, että yhteys suljetaan automaattisesti
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa tietokantayhteyden asynkronisesti

                    // Luodaan komento tietokantaan lisättävien puhelintietojen tallentamiseksi
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    INSERT INTO Phones (Brand, Model, Price, Description, Condition, StockQuantity)
                    VALUES (@Brand, @Model, @Price, @Description, @Condition, @StockQuantity)";
                    // Tämä SQL-komento lisää uuden rivin Phones-tauluun, täyttäen kentät annetulla tiedolla

                    // Määritetään SQL-kyselylle parametrit ja niiden tyypit ja arvot
                    command.Parameters.Add("@Brand", SqliteType.Text).Value = phone.Brand;            // Puhelimen merkki
                    command.Parameters.Add("@Model", SqliteType.Text).Value = phone.Model;            // Puhelimen malli
                    command.Parameters.Add("@Price", SqliteType.Real).Value = phone.Price;            // Puhelimen hinta
                    command.Parameters.Add("@Description", SqliteType.Text).Value = phone.Description; // Puhelimen kuvaus
                    command.Parameters.Add("@Condition", SqliteType.Text).Value = phone.Condition;    // Puhelimen kunto
                    command.Parameters.Add("@StockQuantity", SqliteType.Integer).Value = phone.StockQuantity; // Varastossa oleva määrä

                    // Suoritetaan SQL-komento tietokantaan asynkronisesti, lisäten uuden puhelimen tiedot tietokantaan
                    await command.ExecuteNonQueryAsync();

                    // Määritetään uusi komento hakemaan viimeksi lisätyn rivin ID tietokannasta
                    command.CommandText = "SELECT last_insert_rowid()";
                    // Suoritetaan komento ja haetaan juuri lisätyn puhelimen ID
                    var phoneId = (long)await command.ExecuteScalarAsync();

                    // Asetetaan PhoneModel-olion PhoneID-kenttään juuri lisätty ID
                    phone.PhoneID = (int)phoneId;

                    // Palautetaan onnistumisviesti "Created"-statuksella sekä juuri lisätyn puhelimen tiedot JSON-muodossa
                    return Results.Created($"/api/phones/{phone.PhoneID}", phone);
                }
            }
            catch (Exception ex) // Jos virhe ilmenee, ohjataan virhe tähän kohtaan
            {
                // Palautetaan yleinen virheviesti, jos lisäys epäonnistuu
                return Results.Problem($"Virhe puhelimen lisäämisessä: {ex.Message}");
            }
        }

        // Metodi, joka poistaa puhelimen tietokannasta annettuun id:hen perustuen
        private static async Task<IResult> DeletePhone(int id)
        {
            // Tarkistetaan että syötetty ID ei ole negatiivinen luku.
            if (id <= 0)
            {
                return Results.BadRequest("Annettu ID on virheellinen. ID:n tulee olla suurempi kuin 0.");
            }

            try
            {
                // Avataan tietokantayhteys SQLite-tietokantaan "using"-lohkon sisällä, joka varmistaa, että yhteys suljetaan automaattisesti
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa tietokantayhteyden asynkronisesti

                    // Luodaan komento, joka suorittaa SQL-kyselyn puhelimen poistamiseksi annettuun id:hen perustuen
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Phones WHERE PhoneID = @id";
                    command.Parameters.AddWithValue("@id", id);

                    // Suoritetaan SQL-komento tietokantaan asynkronisesti
                    var result = await command.ExecuteNonQueryAsync();

                    // Tarkistetaan, löytyikö ja poistettiinko puhelin
                    if (result == 0)
                    {
                        // Jos yhtään riviä ei poistettu, palautetaan NotFound-virhe
                        return Results.NotFound($"Puhelinta ID:llä {id} ei löytynyt.");
                    }

                    // Palautetaan onnistumisviesti, jos puhelin poistettiin
                    return Results.Ok($"Puhelin ID:llä {id} poistettiin onnistuneesti.");
                }
            }
            catch (Exception ex) // Jos virhe ilmenee, ohjataan virhe tähän kohtaan
            {
                // Palautetaan yleinen virheviesti, jos poisto epäonnistuu
                return Results.Problem($"Virhe puhelimen poistamisessa: {ex.Message}");
            }
        }

        // UpdatePhonePartial metodi, joka päivittää osittain puhelimen tiedot annetun ID:n perusteella
        // Tämä metodi käyttää DTO-luokkaa nimeltä UpdatePhoneModel puhelimen tietojen osittaiseen päivittämiseen.
        // DTO-luokka mahdollistaa päivitysten rajaamisen vain tiettyihin kenttiin.
        // Käyttäjä voi esimerkiksi päivittää vain hinnan ja kuvauksen ilman, että muut tiedot muuttuvat.
        private static async Task<IResult> UpdatePhonePartial(int id, [FromBody] UpdatePhoneModel updatedFields)
        {
            // Tarkistetaan ensin, että päivitysobjekti ei ole null
            if (updatedFields == null)
            {
                return Results.BadRequest("Päivityspyyntö on virheellinen.");
            }

            try
            {
                // Avataan tietokantayhteys SQLite-tietokantaan "using"-lohkon sisällä, joka varmistaa, että yhteys suljetaan automaattisesti
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync();

                    // Haetaan nykyiset tiedot puhelimesta annettuun ID:hen perustuen
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT PhoneID, Brand, Model, Price, Description, Condition, StockQuantity FROM Phones WHERE PhoneID = @id";
                    command.Parameters.AddWithValue("@id", id);

                    using var reader = await command.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return Results.NotFound($"Puhelinta ID:llä {id} ei löytynyt.");
                    }

                    // Luodaan PhoneModel-olio nykyisillä tiedoilla
                    var phone = new PhoneModel
                    {
                        PhoneID = reader.GetInt32(0),
                        Brand = reader.GetString(1),
                        Model = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Description = reader.GetString(4),
                        Condition = reader.GetString(5),
                        StockQuantity = reader.GetInt32(6)
                    };

                    // Päivitetään vain ne kentät, jotka on annettu päivitysobjektissa
                    // Jos päivitysarvo on null, säilytetään nykyinen arvo
                    phone.Brand = updatedFields.Brand ?? phone.Brand;
                    phone.Model = updatedFields.Model ?? phone.Model;
                    phone.Price = updatedFields.Price ?? phone.Price;
                    phone.Description = updatedFields.Description ?? phone.Description;
                    phone.Condition = updatedFields.Condition ?? phone.Condition;
                    phone.StockQuantity = updatedFields.StockQuantity ?? phone.StockQuantity;

                    // Validointia: Tarkistetaan, että päivitetyt kentät ovat edelleen validit (esim. hinta positiivinen, varastosaldo ei negatiivinen)
                    if (phone.Price <= 0 || phone.StockQuantity < 0)
                    {
                        return Results.BadRequest("Päivityksen jälkeen arvo on virheellinen: Hinnan täytyy olla positiivinen ja varastosaldon ei voi olla negatiivinen.");
                    }

                    // Luodaan komento päivitettyjen tietojen tallentamiseksi tietokantaan
                    command = connection.CreateCommand();
                    command.CommandText = @"
                    UPDATE Phones
                    SET Brand = @Brand, Model = @Model, Price = @Price, Description = @Description, Condition = @Condition, StockQuantity = @StockQuantity
                    WHERE PhoneID = @id";

                    // Määritetään SQL-komentoon uudet päivitetyt parametrit
                    command.Parameters.AddWithValue("@Brand", phone.Brand);
                    command.Parameters.AddWithValue("@Model", phone.Model);
                    command.Parameters.AddWithValue("@Price", phone.Price);
                    command.Parameters.AddWithValue("@Description", phone.Description);
                    command.Parameters.AddWithValue("@Condition", phone.Condition);
                    command.Parameters.AddWithValue("@StockQuantity", phone.StockQuantity);
                    command.Parameters.AddWithValue("@id", id);

                    // Suoritetaan päivitetty komento tietokantaan
                    await command.ExecuteNonQueryAsync();
                    return Results.Ok(phone); // Palautetaan päivitetty puhelin JSON-muodossa
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe puhelimen osittaisessa päivittämisessä: {ex.Message}");
            }
        }
    }

    // DTO-luokka, joka mahdollistaa osittaiset päivitykset
    // Tämä luokka mahdollistaa vain tiettyjen kenttien päivittämisen puhelimen tiedoissa.
    // Esimerkiksi, jos käyttäjä haluaa päivittää vain puhelimen hinnan, hän voi tehdä sen ilman, että muut tiedot muuttuvat.
    public class UpdatePhoneModel
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? Condition { get; set; }
        public int? StockQuantity { get; set; }
    }
}
