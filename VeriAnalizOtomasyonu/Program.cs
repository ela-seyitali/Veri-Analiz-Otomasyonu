using System;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;

namespace VeriAnalizOtomasyonu
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new başlangıçFormu());
        }

        private static void CreateDatabase()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VeriAnalizDB.mdf");
            
            if (!File.Exists(dbPath))
            {
                try
                {
                    // Ana veritabanına bağlan
                    using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True"))
                    {
                        connection.Open();

                        // Veritabanını oluştur
                        string createDbQuery = $@"
                            CREATE DATABASE VeriAnalizDB ON PRIMARY 
                            (NAME = VeriAnalizDB_Data, 
                             FILENAME = '{dbPath}')";

                        using (var command = new SqlCommand(createDbQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    // Tabloları oluştur
                    using (var connection = DatabaseHelper.OpenConnection())
                    {
                        string createTablesQuery = @"
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
                            CREATE TABLE [User] (
                                UserId INT IDENTITY(1,1) PRIMARY KEY,
                                UserName NVARCHAR(100) NOT NULL UNIQUE,
                                Password NVARCHAR(100) NOT NULL,
                                Gender INT NOT NULL,
                                Profession NVARCHAR(100) NOT NULL
                            );
                            
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Dataset')
                            CREATE TABLE Dataset (
                                DatasetID INT IDENTITY(1,1) PRIMARY KEY,
                                UserID INT NOT NULL,
                                DatasetName NVARCHAR(100) NOT NULL,
                                FilePath NVARCHAR(255) NOT NULL,
                                IsDeleted BIT DEFAULT 0,
                                FOREIGN KEY(UserID) REFERENCES [User](UserId)
                            );";

                        using (var command = new SqlCommand(createTablesQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Veritabanı oluşturulurken hata: " + ex.Message);
                }
            }
        }
    }
}
