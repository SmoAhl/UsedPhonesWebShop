using Microsoft.Data.Sqlite;

namespace Backend.Data
{
    public static class DatabaseInitializer
    {
        // Tämä metodi alustaa tietokantataulut, jos niitä ei ole vielä olemassa
        public static async Task Initialize()
        {
            // using-lauselma avaa yhteyden tietokantaan ja sulkee sen automaattisesti, kun lohko päättyy
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
            {
                await connection.OpenAsync(); // Yhteyden avaaminen asynkronisesti

                // Luodaan taulut vuokaavion mukaisesti

                // Phones-taulu (tallentaa puhelinten tiedot)
                var createPhonesTable = connection.CreateCommand();
                createPhonesTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Phones (
                        PhoneID INTEGER PRIMARY KEY AUTOINCREMENT, -- PK, puhelimen yksilöllinen tunniste
                        Brand TEXT NOT NULL,                         -- Puhelimen merkki
                        Model TEXT NOT NULL,                         -- Puhelimen malli
                        Price REAL NOT NULL,                         -- Puhelimen hinta
                        Description TEXT,                           -- Puhelimen kuvaus
                        Condition TEXT NOT NULL,                    -- Puhelimen kunto
                        StockQuantity INTEGER                       -- Varastossa oleva määrä
                    )";
                await createPhonesTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti

                // Users-taulu (tallentaa käyttäjien tiedot)
                var createUsersTable = connection.CreateCommand();
                createUsersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT, -- PK, käyttäjän yksilöllinen tunniste
                        Role TEXT NOT NULL,                        -- Käyttäjän rooli
                        Email TEXT NOT NULL UNIQUE,                -- Käyttäjän sähköpostiosoite
                        PasswordHash TEXT NOT NULL,                -- Käyttäjän salasanan hash
                        FirstName TEXT NOT NULL,                   -- Käyttäjän etunimi
                        LastName TEXT NOT NULL,                    -- Käyttäjän sukunimi
                        Address TEXT,                              -- Käyttäjän osoite
                        PhoneNumber TEXT,                          -- Käyttäjän puhelinnumero
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP, -- Luontipäivämäärä
                        SessionID TEXT                             -- Käyttäjän istunto-ID
                    )";
                await createUsersTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti

                // Orders-taulu (tallentaa tilaukset)
                var createOrdersTable = connection.CreateCommand();
                createOrdersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Orders (
                        OrderID INTEGER PRIMARY KEY AUTOINCREMENT, -- PK, tilauksen yksilöllinen tunniste
                        UserID INTEGER NOT NULL,                   -- FK, viittaa Users-tauluun
                        OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP, -- Tilauksen päivämäärä
                        TotalPrice REAL NOT NULL,                  -- Tilauksen kokonaishinta
                        Status TEXT,                               -- Tilauksen tila
                        FOREIGN KEY (UserID) REFERENCES Users(UserID) -- Määrittää, että UserID viittaa Users-tauluun
                    )";
                await createOrdersTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti

                // OrderItems-taulu (tallentaa tilausten yksittäiset tuotteet)
                var createOrderItemsTable = connection.CreateCommand();
                createOrderItemsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OrderItems (
                        OrderItemID INTEGER PRIMARY KEY AUTOINCREMENT, -- PK, tilauserän yksilöllinen tunniste
                        OrderID INTEGER NOT NULL,                      -- FK, viittaa Orders-tauluun
                        PhoneID INTEGER NOT NULL,                      -- FK, viittaa Phones-tauluun
                        Quantity INTEGER NOT NULL,                     -- Tuotemäärä
                        Price REAL NOT NULL,                           -- Tuotteen hinta tilaushetkellä
                        FOREIGN KEY (OrderID) REFERENCES Orders(OrderID), -- Määrittää, että OrderID viittaa Orders-tauluun
                        FOREIGN KEY (PhoneID) REFERENCES Phones(PhoneID)  -- Määrittää, että PhoneID viittaa Phones-tauluun
                    )";
                await createOrderItemsTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti

                // ShoppingCart-taulu (tallentaa käyttäjän ostoskorin)
                var createShoppingCartTable = connection.CreateCommand();
                createShoppingCartTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ShoppingCart (
                        CartID INTEGER PRIMARY KEY AUTOINCREMENT,  -- PK, ostoskorin yksilöllinen tunniste
                        UserID INTEGER NOT NULL UNIQUE,            -- FK, viittaa Users-tauluun, yksi käyttäjä = yksi ostoskori
                        FOREIGN KEY (UserID) REFERENCES Users(UserID) -- Määrittää, että UserID viittaa Users-tauluun
                    )";
                await createShoppingCartTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti

                // CartItems-taulu (tallentaa ostoskoriin lisätyt tuotteet)
                var createCartItemsTable = connection.CreateCommand();
                createCartItemsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CartItems (
                        CartItemID INTEGER PRIMARY KEY AUTOINCREMENT, -- PK, ostoskoriin lisätyn tuotteen yksilöllinen tunniste
                        CartID INTEGER NOT NULL,                      -- FK, viittaa ShoppingCart-tauluun
                        PhoneID INTEGER NOT NULL,                     -- FK, viittaa Phones-tauluun
                        Quantity INTEGER NOT NULL,                    -- Tuotemäärä ostoskorissa
                        FOREIGN KEY (CartID) REFERENCES ShoppingCart(CartID), -- Määrittää, että CartID viittaa ShoppingCart-tauluun
                        FOREIGN KEY (PhoneID) REFERENCES Phones(PhoneID)      -- Määrittää, että PhoneID viittaa Phones-tauluun
                    )";
                await createCartItemsTable.ExecuteNonQueryAsync(); // Taulun luonti asynkronisesti
            }
            // Tähän päättyessä using-lohko sulkee automaattisesti tietokantayhteyden
            // Tämä tapahtuu kutsumalla SqliteConnection-olion Dispose-metodia
        }
    }
}
