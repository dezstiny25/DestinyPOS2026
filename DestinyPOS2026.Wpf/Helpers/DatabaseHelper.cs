using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DestinyPOS2026.Wpf.Helpers;

public static class DatabaseHelper
{
    private static string DbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventory.db");

    public static void InitializeDatabase()
    {
        if (!File.Exists(DbFile))
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE Products (
                    ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Barcode TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL,
                    StockQuantity INTEGER NOT NULL
                );

                INSERT INTO Products (Barcode, Name, Price, StockQuantity) VALUES
                ('123456', 'Gaming Mouse', 1500, 10),
                ('234567', 'Mechanical Keyboard', 4500, 5),
                ('345678', 'RTX 4070 GPU', 45000, 2);
            ";
            command.ExecuteNonQuery();
        }
    }

    public static (string Name, decimal Price)? GetProductByBarcode(string barcode)
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Price FROM Products WHERE Barcode = @barcode";
        command.Parameters.AddWithValue("@barcode", barcode);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            string name = reader.GetString(0);
            decimal price = reader.GetDecimal(1);
            return (name, price);
        }

        return null;
    }
}
