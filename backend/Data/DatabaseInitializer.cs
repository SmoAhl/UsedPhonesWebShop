using Microsoft.Data.Sqlite;
using System.Text;

namespace Backend.Data
{
    public static class DatabaseInitializer
    {
        public static async Task Initialize()
        {
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"))
            {
                await connection.OpenAsync();

                var createPhonesTable = connection.CreateCommand();
                createPhonesTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Phones (
                        PhoneID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Brand TEXT NOT NULL,
                        Model TEXT NOT NULL,
                        Price REAL NOT NULL,
                        Description TEXT,
                        Condition TEXT NOT NULL,
                        StockQuantity INTEGER
                    )";
                await createPhonesTable.ExecuteNonQueryAsync();

                var createUsersTable = connection.CreateCommand();
                createUsersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Role TEXT NOT NULL,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        PhoneNumber TEXT NOT NULL,
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";
                await createUsersTable.ExecuteNonQueryAsync();

                var createOrdersTable = connection.CreateCommand();
                createOrdersTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Orders (
                        OrderID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL,
                        OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        TotalPrice REAL NOT NULL,
                        Status TEXT,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";
                await createOrdersTable.ExecuteNonQueryAsync();

                var createOrderItemsTable = connection.CreateCommand();
                createOrderItemsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OrderItems (
                        OrderItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                        OrderID INTEGER NOT NULL,
                        PhoneID INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        Price REAL NOT NULL,
                        FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
                        FOREIGN KEY (PhoneID) REFERENCES Phones(PhoneID)
                    )";
                await createOrderItemsTable.ExecuteNonQueryAsync();

                var createShoppingCartTable = connection.CreateCommand();
                createShoppingCartTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ShoppingCart (
                        CartID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL UNIQUE,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";
                await createShoppingCartTable.ExecuteNonQueryAsync();

                var createCartItemsTable = connection.CreateCommand();
                createCartItemsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CartItems (
                        CartItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                        CartID INTEGER NOT NULL,
                        PhoneID INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        FOREIGN KEY (CartID) REFERENCES ShoppingCart(CartID),
                        FOREIGN KEY (PhoneID) REFERENCES Phones(PhoneID)
                    )";
                await createCartItemsTable.ExecuteNonQueryAsync();

                await SeedData(connection);
                await SeedPhones(connection);
            }
        }

        private static async Task SeedData(SqliteConnection connection)
        {
            var checkAdminCommand = connection.CreateCommand();
            checkAdminCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = 'admin@usedphoneshop.com'";
            var adminExists = (long)await checkAdminCommand.ExecuteScalarAsync() > 0;

            if (!adminExists)
            {
                var insertAdminCommand = connection.CreateCommand();
                string adminPassword = "Admin123!";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                insertAdminCommand.CommandText = @"
                    INSERT INTO Users (Role, Email, PasswordHash, FirstName, LastName, Address, PhoneNumber)
                    VALUES ('Admin', 'admin@usedphoneshop.com', @PasswordHash, 'Admin', 'User', '123 Admin Street', '123456789')";
                insertAdminCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);

                await insertAdminCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Admin käyttäjä lisätty: admin@usedphoneshop.com");
            }

            var checkCustomerCommand = connection.CreateCommand();
            checkCustomerCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = 'customer@usedphoneshop.com'";
            var customerExists = (long)await checkCustomerCommand.ExecuteScalarAsync() > 0;

            if (!customerExists)
            {
                var insertCustomerCommand = connection.CreateCommand();
                string customerPassword = "Customer123!";
                string customerPasswordHash = BCrypt.Net.BCrypt.HashPassword(customerPassword);

                insertCustomerCommand.CommandText = @"
                    INSERT INTO Users (Role, Email, PasswordHash, FirstName, LastName, Address, PhoneNumber)
                    VALUES ('Customer', 'customer@usedphoneshop.com', @CustomerPasswordHash, 'Test', 'Customer', '456 Customer Lane', '987654321')";
                insertCustomerCommand.Parameters.AddWithValue("@CustomerPasswordHash", customerPasswordHash);

                await insertCustomerCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Testiasiakas lisätty: customer@usedphoneshop.com");
            }
        }

        private static async Task SeedPhones(SqliteConnection connection)
        {
            var checkPhonesCommand = connection.CreateCommand();
            checkPhonesCommand.CommandText = "SELECT COUNT(*) FROM Phones";
            var phonesExist = (long)await checkPhonesCommand.ExecuteScalarAsync() > 0;

            if (!phonesExist)
            {
                var insertPhonesCommand = connection.CreateCommand();
                insertPhonesCommand.CommandText = @"
                    INSERT INTO Phones (Brand, Model, Price, Description, Condition, StockQuantity) VALUES
                    ('Apple', 'iPhone 12', 799.99, 'Latest model with A14 Bionic chip', 'New', 10),
                    ('Samsung', 'Galaxy S21', 699.99, 'Flagship model with Exynos 2100', 'New', 15),
                    ('Google', 'Pixel 5', 599.99, '5G capable with Snapdragon 765G', 'New', 8),
                    ('OnePlus', '8T', 499.99, 'Fast and smooth with Snapdragon 865', 'New', 12),
                    ('Sony', 'Xperia 5 II', 649.99, 'Compact flagship with Snapdragon 865', 'New', 5)";
                await insertPhonesCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Esimerkkipuhelimet lisätty tietokantaan.");
            }
        }
    }
}
