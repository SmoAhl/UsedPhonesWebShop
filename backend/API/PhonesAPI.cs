using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using Shared.DTOs;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api
{
    // PhonesApi-luokka määrittää API-reitit ja metodit, joiden avulla hallitaan puhelinten tietoja SQLite-tietokannassa.
    // Luokan metodit noudattavat yhtenäistä rakennetta, jossa käytetään seuraavia käytäntöjä:
    // - Asynkroninen suoritustapa (async) mahdollistaa ei-estävän suorituksen ja parantaa sovelluksen skaalautuvuutta.
    // - "using"-lohkoja tietokantayhteyksien hallintaan, mikä varmistaa yhteyksien automaattisen sulkemisen suorituksen jälkeen ja ehkäisee resurssivuotoja.
    // - Try-catch-lohkot, jotka käsittelevät mahdolliset virheet hallitusti ja palauttavat selkeät virheviestit `Results.Problem()`-metodin avulla.
    // - Parametrien käyttö SQL-komennoissa `Parameters.AddWithValue()`-menetelmällä estää SQL-injektiohyökkäykset ja parantaa tietoturvaa.

    // API-reittien määrittely sisältää seuraavat metodit:
    // - `MapPhonesApi`: Rekisteröi API-reitit (GET, POST, DELETE, PATCH) ja linkittää ne vastaaviin metodien toteutuksiin.
    // - `GetPhones`: Hakee puhelimet tietokannasta ja mahdollistaa suodatuksen valinnaisilla parametreilla.
    // - `AddPhone`: Lisää uuden puhelimen tietokantaan ja palauttaa luodun puhelimen tiedot JSON-muodossa.
    // - `DeletePhone`: Poistaa tietyn puhelimen tietokannasta ID:n perusteella.
    // - `UpdatePhonePartial`: Päivittää osittain puhelimen tiedot annetun ID:n perusteella käyttäen DTO-luokkaa, jotta vain tarvittavat kentät päivittyvät.


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
            //PUT: Kokonaispäivitys. Korvaa koko resurssin.
            //PATCH: Osittainen päivitys. Päivittää vain tietyt resurssin osat.
        }

        // GetPhones metodi, joka hakee puhelimia tietokannasta ja mahdollistaa suodatuksen valinnaisilla parametreilla
        // Tämä metodi hyödyntää valinnaisia kyselyparametreja, kuten "brand", "model", "condition" ja "maxPrice",
        // jotka suodattavat tuloksia käyttäjän haluamien kriteerien perusteella.
        // Näin ollen metodi mahdollistaa joustavan tavan hakea tietokannasta puhelimia yksilöllisiin tarpeisiin sopivalla tavalla.
        private static async Task<IResult> GetPhones([FromQuery] string? brand = null, [FromQuery] string? model = null, [FromQuery] string? condition = null, [FromQuery] decimal? maxPrice = null)
        {
            // Alustetaan tyhjä lista, johon tallennetaan haetut puhelimet. Tämä lista palautetaan lopuksi API:n vastauksena.
            var phones = new List<PhoneModel>();

            try
            {
                // Avaa yhteys SQLite-tietokantaan. Käytämme asynkronista yhteyden avaamista, jotta se ei estä muiden prosessien suoritusta.
                using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
                {
                    await connection.OpenAsync(); // Avaa tietokantayhteyden asynkronisesti, mikä mahdollistaa skaalautuvuuden, kun useita pyyntöjä käsitellään samanaikaisesti.

                    // Luodaan uusi komento, jonka kautta määritetään SQL-kysely tietokantaa varten.
                    var command = connection.CreateCommand();

                    // Rakennetaan SQL-kysely, joka hakee kaikki puhelimet.
                    // "WHERE 1=1" mahdollistaa suodatusehtojen dynaamisen lisäämisen helposti, ilman että tarvitsisi huolehtia siitä, lisätäänkö WHERE vai AND.
                    // Kaikki lisäsuodattimet voivat käyttää suoraan AND-operaattoria.
                    var query = "SELECT PhoneID, Brand, Model, Price, Description, Condition, StockQuantity FROM Phones WHERE 1=1";

                    // Tarkistetaan, onko käyttäjä antanut arvon "brand"-parametriin. Jos kyllä, lisätään kyselyyn suodatus kyseisen merkin perusteella.
                    if (!string.IsNullOrEmpty(brand))
                    {
                        query += " AND Brand = @brand"; // SQL-kyselyyn lisätään suodatusehto, jossa merkki vastaa käyttäjän syöttämää arvoa.
                        command.Parameters.AddWithValue("@brand", brand); // Parametrien avulla vältetään SQL-injektiot.
                    }

                    // Tarkistetaan, onko käyttäjä antanut arvon "model"-parametriin. Jos kyllä, lisätään suodatus kyseisen mallin perusteella.
                    if (!string.IsNullOrEmpty(model))
                    {
                        query += " AND Model = @model"; // Lisätään suodatus, joka rajaa haun tiettyyn malliin.
                        command.Parameters.AddWithValue("@model", model); // Parametrin lisäys estää SQL-injektiohyökkäyksiä.
                    }

                    // Tarkistetaan, onko käyttäjä antanut arvon "condition"-parametriin. Jos kyllä, lisätään suodatus puhelimen kunnon mukaan.
                    if (!string.IsNullOrEmpty(condition))
                    {
                        query += " AND Condition = @condition"; // Lisätään suodatus puhelimen kunnon perusteella (esim. "Uusi", "Hyvä", "Käytetty").
                        command.Parameters.AddWithValue("@condition", condition); // Parametrin lisääminen SQL-kyselyyn.
                    }

                    // Tarkistetaan, onko käyttäjä antanut "maxPrice"-arvon. Jos kyllä, lisätään suodatus maksimiarvon perusteella.
                    if (maxPrice.HasValue)
                    {
                        query += " AND Price <= @maxPrice"; // Lisätään suodatus hintakatolle, jolloin haetaan puhelimet, jotka maksavat vähemmän tai saman verran kuin annettu arvo.
                        command.Parameters.AddWithValue("@maxPrice", maxPrice.Value); // Lisätään parametrina, joka estää SQL-injektiot.
                    }

                    // Asetetaan komennon kyselytekstiksi rakennettu kysely.
                    command.CommandText = query;

                    // Suoritetaan SQL-kysely ja käsitellään sen tulokset. Käytetään asynkronista lukijaa.
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        // Jokainen tietokannan rivi lisätään "phones"-listaan PhoneModel-oliona.
                        // Tämä tekee tietojen käsittelystä tehokasta, sillä käytämme vahvatyyppistä oliota tietojen hallintaan.
                        phones.Add(new PhoneModel
                        {
                            PhoneID = reader.GetInt32(0),       // Haetaan PhoneID (ensimmäinen sarake, index 0)
                            Brand = reader.GetString(1),       // Haetaan Brand (toinen sarake, index 1)
                            Model = reader.GetString(2),       // Haetaan Model (kolmas sarake, index 2)
                            Price = reader.GetDecimal(3),      // Haetaan Price (neljäs sarake, index 3)
                            Description = reader.GetString(4), // Haetaan Description (viides sarake, index 4)
                            Condition = reader.GetString(5),   // Haetaan Condition (kuudes sarake, index 5)
                            StockQuantity = reader.GetInt32(6) // Haetaan StockQuantity (seitsemäs sarake, index 6)
                        });
                    }
                }

                // Palautetaan tulokset JSON-muodossa. Tämä mahdollistaa front-end-sovelluksen käyttävän tuloksia helposti.
                return Results.Ok(phones);
            }
            catch (Exception ex)
            {
                // Jos jokin menee vikaan (esim. tietokantayhteys ei onnistu tai komennon suoritus epäonnistuu), käsitellään virhe tässä kohtaa.
                // Palautetaan yleinen virheviesti. Tämä parantaa API:n virheensietokykyä ja auttaa kehittäjiä virhetilanteiden diagnosoinnissa.
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

                    // Päivitetään puhelin-olion tiedot vain, jos updatedFields-kentät eivät ole null-arvoja
                    // Null-coalescing-operaattori (??) tarkistaa, onko vasemmanpuoleinen arvo null.
                    // Jos arvo on null, käytetään oikeanpuoleista arvoa, joka on nykyinen olion kenttäarvo.
                    phone.Brand = updatedFields.Brand ?? phone.Brand; // Esim. Päivitä Brand, jos updatedFields.Brand ei ole null
                    phone.Model = updatedFields.Model ?? phone.Model;
                    phone.Price = updatedFields.Price ?? phone.Price;
                    phone.Description = updatedFields.Description ?? phone.Description;
                    phone.Condition = updatedFields.Condition ?? phone.Condition;
                    phone.StockQuantity = updatedFields.StockQuantity ?? phone.StockQuantity;
                    // Tällä logiikalla päivitetään vain ne kentät, joihin on annettu uusi arvo (ei null).
                    // Tämä lähestymistapa varmistaa, että olemassa olevat arvot säilyvät, jos päivitysarvo on null.

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
}

