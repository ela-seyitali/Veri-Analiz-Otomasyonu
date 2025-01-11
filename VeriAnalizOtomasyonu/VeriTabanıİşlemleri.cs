using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace VeriAnalizOtomasyonu
{
    public class VeriTabanıİşlemleri
    {
        private static readonly object _lock = new object();

        public VeriTabanıİşlemleri()
        {
            EnsureTableExists();
        }

        public void EnsureTableExists()
        {
            lock (_lock)
            {
                try
                {
                    using (var connection = DatabaseHelper.OpenConnection())
                    {
                        string query = @"
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Dataset')
                            CREATE TABLE Dataset (
                                DatasetID INT IDENTITY(1,1) PRIMARY KEY,
                                UserID INT NOT NULL,
                                DatasetName NVARCHAR(100) NOT NULL,
                                IsDeleted BIT DEFAULT 0,
                                OriginalFilePath NVARCHAR(255) NOT NULL
                            )";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tablo oluşturulurken bir hata oluştu: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SaveDataset(int userId, string datasetName, string filePath)
        {
            try
            {
                DatabaseHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    string query = @"
                        INSERT INTO Dataset 
                        (UserID, DatasetName, IsDeleted, OriginalFilePath) 
                        VALUES (@UserID, @DatasetName, @IsDeleted, @OriginalFilePath)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Transaction = transaction;
                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@DatasetName", datasetName);
                        command.Parameters.AddWithValue("@IsDeleted", false);
                        command.Parameters.AddWithValue("@OriginalFilePath", filePath);

                        command.ExecuteNonQuery();
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri seti kaydedilirken bir hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DisplayDataset(int datasetId, Panel veriSetiPanel1)
        {
            lock (_lock)
            {
                try
                {
                    string filePath;
                    using (var connection = DatabaseHelper.OpenConnection())
                    {
                        string query = "SELECT OriginalFilePath FROM Dataset WHERE DatasetID = @DatasetID AND IsDeleted = 0";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@DatasetID", datasetId);
                            filePath = command.ExecuteScalar()?.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        throw new Exception("Veri seti bulunamadı veya dosya mevcut değil.");
                    }

                    DataTable dataTable = new DataTable();
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string[] headers = sr.ReadLine().Split(',');
                        foreach (string header in headers)
                        {
                            dataTable.Columns.Add(header);
                        }

                        while (!sr.EndOfStream)
                        {
                            string[] rows = sr.ReadLine().Split(',');
                            if (rows.Length == headers.Length)
                            {
                                dataTable.Rows.Add(rows);
                            }
                        }
                    }

                    veriSetiPanel1.Invoke((MethodInvoker)delegate
                    {
                        veriSetiPanel1.Controls.Clear();
                        DataGridView dataGridView = new DataGridView
                        {
                            Name = "dataGridView",
                            Dock = DockStyle.Fill,
                            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                            DataSource = dataTable,
                            AllowUserToAddRows = false
                        };
                        veriSetiPanel1.Controls.Add(dataGridView);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Veri seti görüntülenirken bir hata oluştu: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void DeleteDataset(int datasetId)
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery(
                    "UPDATE Dataset SET IsDeleted = 1 WHERE DatasetID = @DatasetID",
                    command => command.Parameters.AddWithValue("@DatasetID", datasetId)
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri seti silinirken bir hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public DataTable ListDatasets(int userId)
        {
            var dataTable = new DataTable();
            try
            {
                using (var connection = DatabaseHelper.OpenConnection())
                {
                    string query = @"
                        SELECT DatasetID, DatasetName 
                        FROM Dataset 
                        WHERE UserID = @UserID AND IsDeleted = 0";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri setleri listelenirken bir hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dataTable;
        }

        public void UpdateDatabaseSchema()
        {
            try
            {
                string[] queries = new string[]
                {
                    // Önce mevcut tabloyu yedekle
                    "CREATE TABLE IF NOT EXISTS Dataset_backup AS SELECT * FROM Dataset;",
                    
                    // Eski tabloyu sil
                    "DROP TABLE IF EXISTS Dataset;",
                    
                    // Tabloyu doğru şemayla yeniden oluştur
                    @"CREATE TABLE IF NOT EXISTS Dataset (
                        DatasetID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL,
                        DatasetName VARCHAR(100) NOT NULL,
                        IsDeleted BOOLEAN DEFAULT 0,
                        OriginalFilePath VARCHAR(255) NOT NULL COLLATE BINARY
                    );",
                    
                    // Yedeklenen verileri geri yükle
                    @"INSERT INTO Dataset (DatasetID, UserID, DatasetName, IsDeleted, OriginalFilePath)
                    SELECT DatasetID, UserID, DatasetName, IsDeleted, '' FROM Dataset_backup;",
                    
                    // Yedek tabloyu sil
                    "DROP TABLE IF EXISTS Dataset_backup;"
                };

                DatabaseHelper.ExecuteMultipleQueries(queries);
                MessageBox.Show("Veritabanı şeması başarıyla güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı şeması güncellenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}