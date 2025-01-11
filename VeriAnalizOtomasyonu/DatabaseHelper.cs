using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Forms;

public static class DatabaseHelper
{
    private static readonly string _connectionString;
    private static readonly int _commandTimeout = 30;

    static DatabaseHelper()
    {
        try
        {
            // App.config'den bağlantı dizesini al
            _connectionString = ConfigurationManager.ConnectionStrings["VeriAnalizOtomasyonu.Properties.Settings.VeriotodbConnectionString"].ConnectionString;

            // Tabloları kontrol et ve yoksa oluştur
            EnsureTablesExist();
        }
        catch (Exception ex)
        {
            throw new Exception("DatabaseHelper başlatılırken hata: " + ex.Message, ex);
        }
    }

    private static void EnsureTablesExist()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string createTablesQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
                BEGIN
                    CREATE TABLE [User] (
                        UserId INT IDENTITY(1,1) PRIMARY KEY,
                        UserName NVARCHAR(100) NOT NULL UNIQUE,
                        Password NVARCHAR(100) NOT NULL,
                        Gender INT NOT NULL,
                        Profession NVARCHAR(100) NOT NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Dataset')
                BEGIN
                    CREATE TABLE Dataset (
                        DatasetID INT IDENTITY(1,1) PRIMARY KEY,
                        UserID INT NOT NULL,
                        DatasetName NVARCHAR(100) NOT NULL,
                        IsDeleted BIT DEFAULT 0,
                        OriginalFilePath NVARCHAR(255) NOT NULL,
                        FOREIGN KEY(UserID) REFERENCES [User](UserId)
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Report')
                BEGIN
                    CREATE TABLE Report (
                        ReportID INT IDENTITY(1,1) PRIMARY KEY,
                        UserID INT NOT NULL,
                        DatasetID INT NOT NULL,
                        ReportPath NVARCHAR(255) NOT NULL,
                        CreatedDate DATETIME DEFAULT GETDATE(),
                        IsDeleted BIT DEFAULT 0,
                        FOREIGN KEY(UserID) REFERENCES [User](UserId),
                        FOREIGN KEY(DatasetID) REFERENCES Dataset(DatasetID)
                    );
                END";

            using (var command = new SqlCommand(createTablesQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public static SqlConnection OpenConnection()
    {
        try
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            throw new Exception("Veritabanı bağlantısı açılırken hata: " + ex.Message, ex);
        }
    }

    public static void ExecuteNonQuery(string query, Action<SqlCommand> parameterSetup = null)
    {
        using (var connection = OpenConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.CommandTimeout = _commandTimeout;
            parameterSetup?.Invoke(command);
            command.ExecuteNonQuery();
        }
    }

    public static T ExecuteScalar<T>(string query, Action<SqlCommand> parameterSetup = null)
    {
        using (var connection = OpenConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.CommandTimeout = _commandTimeout;
            parameterSetup?.Invoke(command);
            var result = command.ExecuteScalar();
            return result == DBNull.Value ? default : (T)Convert.ChangeType(result, typeof(T));
        }
    }

    public static void ExecuteMultipleQueries(string[] queries)
    {
        using (var connection = OpenConnection())
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandTimeout = _commandTimeout;

                    foreach (string query in queries)
                    {
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public static void ExecuteWithTransaction(Action<SqlConnection, SqlTransaction> action)
    {
        using (var connection = OpenConnection())
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                action(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public static bool DatasetExists(int userId, string datasetName)
    {
        string query = @"
            SELECT COUNT(*) 
            FROM Dataset 
            WHERE UserID = @UserId 
            AND DatasetName = @DatasetName 
            AND IsDeleted = 0";

        return ExecuteScalar<int>(query, cmd =>
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DatasetName", datasetName);
        }) > 0;
    }

    public static bool SaveDataset(int userId, string datasetName, string filePath)
    {
        try
        {
            string query = @"
                INSERT INTO Dataset (UserID, DatasetName, OriginalFilePath, IsDeleted) 
                VALUES (@UserId, @DatasetName, @FilePath, 0)";

            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@DatasetName", datasetName);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static DataTable GetUserDatasets(int userId)
    {
        string query = @"
            SELECT DatasetName, OriginalFilePath 
            FROM Dataset 
            WHERE UserID = @UserId AND IsDeleted = 0";

        using (var connection = OpenConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@UserId", userId);
            var dataTable = new DataTable();
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dataTable);
            }
            return dataTable;
        }
    }

    public static bool DeleteDataset(int userId, string datasetName)
    {
        try
        {
            string query = @"
                UPDATE Dataset 
                SET IsDeleted = 1
                WHERE UserID = @UserId 
                AND DatasetName = @DatasetName";

            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@DatasetName", datasetName);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string GetDatasetFilePath(int userId, string datasetName)
    {
        string query = @"
            SELECT OriginalFilePath
            FROM Dataset
            WHERE UserID = @UserId
            AND DatasetName = @DatasetName 
            AND IsDeleted = 0";

        return ExecuteScalar<string>(query, cmd =>
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DatasetName", datasetName);
        });
    }

    public static string GetUserDirectory(int userId)
    {
        return Path.Combine(Application.StartupPath, "Datasets", userId.ToString());
    }

    public static int SaveReport(int userId, int datasetId, string reportPath)
    {
        try
        {
            string query = @"
                INSERT INTO Report (UserID, DatasetID, OriginalFilePath, CreatedDate, IsDeleted)
                OUTPUT INSERTED.ReportID
                VALUES (@UserId, @DatasetID, @FilePath, GETDATE(), 0)";

            return ExecuteScalar<int>(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@DatasetID", datasetId);
                cmd.Parameters.AddWithValue("@FilePath", reportPath);
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Rapor kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 0;
        }
    }

    public static DataTable GetUserReports(int userId)
    {
        string query = @"
            SELECT r.ReportID, r.OriginalFilePath, r.CreatedDate, d.DatasetName
            FROM Report r
            JOIN Dataset d ON r.DatasetID = d.DatasetID
            WHERE r.UserID = @UserId AND r.IsDeleted = 0
            ORDER BY r.CreatedDate DESC";

        using (var connection = OpenConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@UserId", userId);
            var dataTable = new DataTable();
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dataTable);
            }
            return dataTable;
        }
    }

    public static bool DeleteReport(int reportId)
    {
        try
        {
            string query = @"
                UPDATE Report 
                SET IsDeleted = 1
                WHERE ReportID = @ReportID";

            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@ReportID", reportId);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}