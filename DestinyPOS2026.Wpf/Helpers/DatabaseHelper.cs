using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using DestinyPOS2026.Wpf.Models;
using System.Linq;


namespace DestinyPOS2026.Wpf.Helpers;

public static class DatabaseHelper
{
    private static readonly string DbPath =
        Path.Combine(AppContext.BaseDirectory, "destinypos.db");

    public static void InitializeDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Barcode TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Price REAL NOT NULL,
            Stock INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS Sales (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Date TEXT NOT NULL,
            PaymentMethod TEXT NOT NULL,
            Total REAL NOT NULL,
            Items TEXT NOT NULL
        );
        ";
        cmd.ExecuteNonQuery();
    }

    // ===== Products =====
    public static List<Product> GetProducts()
    {
        var list = new List<Product>();
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Products";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Product
            {
                Id = reader.GetInt32(0),
                Barcode = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4)
            });
        }

        return list;
    }

    public static Product? GetProductByBarcode(string barcode)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Products WHERE Barcode=$barcode LIMIT 1";
        cmd.Parameters.AddWithValue("$barcode", barcode);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                Barcode = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4)
            };
        }

        return null;
    }

    public static void AddProduct(Product p)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO Products (Barcode, Name, Price, Stock)
        VALUES ($barcode, $name, $price, $stock)
        ";
        cmd.Parameters.AddWithValue("$barcode", p.Barcode);
        cmd.Parameters.AddWithValue("$name", p.Name);
        cmd.Parameters.AddWithValue("$price", p.Price);
        cmd.Parameters.AddWithValue("$stock", p.Stock);

        cmd.ExecuteNonQuery();
    }

    public static void UpdateProduct(Product p)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        UPDATE Products
        SET Barcode=$barcode, Name=$name, Price=$price, Stock=$stock
        WHERE Id=$id
        ";
        cmd.Parameters.AddWithValue("$id", p.Id);
        cmd.Parameters.AddWithValue("$barcode", p.Barcode);
        cmd.Parameters.AddWithValue("$name", p.Name);
        cmd.Parameters.AddWithValue("$price", p.Price);
        cmd.Parameters.AddWithValue("$stock", p.Stock);

        cmd.ExecuteNonQuery();
    }

    public static void DeleteProduct(int id)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Products WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static void DeductProductStock(int productId, int quantity)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        UPDATE Products
        SET Stock = Stock - $quantity
        WHERE Id=$id AND Stock >= $quantity
        ";
        cmd.Parameters.AddWithValue("$id", productId);
        cmd.Parameters.AddWithValue("$quantity", quantity);

        cmd.ExecuteNonQuery();
    }

    // ===== Sales =====
    public static void LogSale(SaleRecord sale)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO Sales (Date, PaymentMethod, Total, Items)
        VALUES ($date, $method, $total, $items)
        ";
        cmd.Parameters.AddWithValue("$date", sale.Date.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$method", sale.PaymentMethod);
        cmd.Parameters.AddWithValue("$total", sale.Total);

        // Serialize Items to JSON
        var itemsJson = System.Text.Json.JsonSerializer.Serialize(sale.Items);
        cmd.Parameters.AddWithValue("$items", itemsJson);

        cmd.ExecuteNonQuery();
    }

    public static List<SaleRecord> GetSales()
    {
        var sales = new List<SaleRecord>();

        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Date, PaymentMethod, Total, Items FROM Sales ORDER BY Date DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var itemsJson = reader.GetString(4);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItem>>(itemsJson) ?? new();

            sales.Add(new SaleRecord
            {
                Id = reader.GetInt32(0),
                Date = DateTime.Parse(reader.GetString(1)),
                PaymentMethod = reader.GetString(2),
                Total = reader.GetDecimal(3),
                Items = items
            });
        }

        return sales;
    }

    // ===== NEW: Get last sale =====
    public static SaleRecord? GetLastSale()
    {
        var sales = GetSales();
        return sales.FirstOrDefault(); // latest sale, since GetSales() orders DESC
    }
}
