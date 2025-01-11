using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Windows.Forms.DataVisualization.Charting;
using DevExpress.XtraEditors;
using System.Data.SqlClient;
using LiveCharts.Wpf;
using LiveCharts;
using DevExpress.XtraBars.Ribbon;
using VeriAnalizOtomasyonu.Properties;

namespace VeriAnalizOtomasyonu
{
    public partial class mainForm : DevExpress.XtraEditors.XtraForm
    {
        SqlConnection con;
        public DataTable dataTable;
        private readonly int userId;
        private readonly VeriTabanıİşlemleri veriTabanıİşlemleri;
        private Görselleştirme görselleştirme;
        private RibbonControl ribbonControl1; // Add this line
        private System.ComponentModel.ComponentResourceManager resources;
        private string currentDatasetName;

        public mainForm(int userId)
        {
            InitializeComponent();
            resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));
            con = new SqlConnection();
            this.userId = userId;
            veriTabanıİşlemleri = new VeriTabanıİşlemleri();
            görselleştirme = new Görselleştirme();
            dataTable = new DataTable();

            // DevExpress tema ayarları
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            try
            {
                string savedTheme = Settings.Default.ThemeName;
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    this.LookAndFeel.SkinName = savedTheme;
                }
                else
                {
                    this.LookAndFeel.SkinName = "DevExpress Style";
                }
            }
            catch
            {
                this.LookAndFeel.SkinName = "DevExpress Style";
            }

            // Ana form boyutunu ayarlıyoruz
            

        }

        public mainForm()
        {
            InitializeComponent();
            con = new SqlConnection();
            görselleştirme = new Görselleştirme();
            dataTable = new DataTable();
        }


        private void mainForm_Load(object sender, EventArgs e)
        {
            // Panel ekleyelim
            this.Controls.Add(VeriSetiPanel1);

            // Diğer analizler panelini ekleyelim
            this.Controls.Add(diğerAnalizlerPanel);
        }
        public void verisetiYükle()
        {
            // Önce kullanıcının mevcut veri setlerini göster
            var userDatasets = DatabaseHelper.GetUserDatasets(userId);
            if (userDatasets.Rows.Count > 0)
            {
                using (var datasetForm = new Form())
                {
                    datasetForm.Text = "Mevcut Veri Setleri";
                    datasetForm.Size = new Size(500, 400);
                    datasetForm.StartPosition = FormStartPosition.CenterParent;

                    var listView = new ListView
                    {
                        Dock = DockStyle.Fill,
                        View = View.Details,
                        FullRowSelect = true
                    };
                    listView.Columns.Add("Veri Seti Adı", 200);
                    listView.Columns.Add("Dosya Yolu", 250);

                    foreach (DataRow row in userDatasets.Rows)
                    {
                        var item = new ListViewItem(row["DatasetName"].ToString());
                        item.SubItems.Add(row["OriginalFilePath"].ToString());
                        listView.Items.Add(item);
                    }

                    var btnYeni = new Button
                    {
                        Text = "Yeni Veri Seti Yükle",
                        Dock = DockStyle.Bottom
                    };
                    btnYeni.Click += (s, e) =>
                    {
                        datasetForm.DialogResult = DialogResult.Yes;
                        datasetForm.Close();
                    };

                    var btnSeç = new Button
                    {
                        Text = "Seçili Veri Setini Aç",
                        Dock = DockStyle.Bottom
                    };
                    btnSeç.Click += (s, e) =>
                    {
                        if (listView.SelectedItems.Count > 0)
                        {
                            string filePath = listView.SelectedItems[0].SubItems[1].Text;
                            if (File.Exists(filePath))
                            {
                                LoadDatasetFromFile(filePath);
                                datasetForm.DialogResult = DialogResult.OK;
                                datasetForm.Close();
                            }
                            else
                            {
                                MessageBox.Show("Veri seti dosyası bulunamadı.",
                                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    };

                    var btnSil = new Button
                    {
                        Text = "Seçili Veri Setini Sil",
                        Dock = DockStyle.Bottom
                    };
                    btnSil.Click += (s, e) =>
                    {
                        if (listView.SelectedItems.Count > 0)
                        {
                            string datasetName = listView.SelectedItems[0].Text;
                            string filePath = listView.SelectedItems[0].SubItems[1].Text;

                            var deleteConfirmResult = MessageBox.Show(
                                "Seçili veri setini silmek istediğinizden emin misiniz?\nBu işlem geri alınamaz.",
                                "Silme Onayı",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (deleteConfirmResult == DialogResult.Yes)
                            {
                                try
                                {
                                    // Veritabanından sil
                                    if (DatabaseHelper.DeleteDataset(userId, datasetName))
                                    {
                                        bool dosyaSilindi = false;
                                        bool dizinTemizlendi = false;

                                        // Dosyayı fiziksel olarak sil
                                        if (File.Exists(filePath))
                                        {
                                            try
                                            {
                                                File.Delete(filePath);
                                                dosyaSilindi = true;

                                                // Kullanıcının dizinini kontrol et
                                                string userDirectory = DatabaseHelper.GetUserDirectory(userId);
                                                if (Directory.Exists(userDirectory))
                                                {
                                                    // Dizinde başka dosya kalmadıysa dizini sil
                                                    if (!Directory.EnumerateFiles(userDirectory).Any())
                                                    {
                                                        Directory.Delete(userDirectory, false);
                                                        dizinTemizlendi = true;
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                MessageBox.Show(
                                                    $"Dosya silinirken hata oluştu: {ex.Message}",
                                                    "Uyarı",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Warning);
                                            }
                                        }

                                        // ListViewden kaldır
                                        listView.SelectedItems[0].Remove();

                                        string message = "Veri seti başarıyla silindi.";
                                        if (dosyaSilindi) message += "\nDosya sisteminden de kaldırıldı.";
                                        if (dizinTemizlendi) message += "\nBoş dizin temizlendi.";

                                        MessageBox.Show(
                                            message,
                                            "Başarılı",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);

                                        // Eğer başka veri seti kalmadıysa formu kapat
                                        if (listView.Items.Count == 0)
                                        {
                                            datasetForm.DialogResult = DialogResult.Yes;
                                            datasetForm.Close();
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show(
                                            "Veri seti silinirken bir hata oluştu.",
                                            "Hata",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(
                                        $"Veri seti silinirken bir hata oluştu: {ex.Message}",
                                        "Hata",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                "Lütfen silmek istediğiniz veri setini seçin.",
                                "Uyarı",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    };

                    // Butonları forma ekle (sıralama önemli)
                    datasetForm.Controls.Add(btnYeni);
                    datasetForm.Controls.Add(btnSeç);
                    datasetForm.Controls.Add(btnSil);
                    datasetForm.Controls.Add(listView);

                    var result = datasetForm.ShowDialog();
                    if (result != DialogResult.Yes)
                    {
                        return; // Kullanıcı yeni veri seti yüklemeyi seçmezse çık
                    }
                }
            }

            // Mevcut yükleme kodu buradan devam eder...
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Veri Seti Yükle"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                
                // currentDatasetName'i burada da set et
                currentDatasetName = fileNameWithoutExtension;

                // Kullanıcıya özel dizin oluştur
                string userDirectory = Path.Combine(Application.StartupPath, "Datasets", userId.ToString());
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                // Hedef dosya yolu (kullanıcı ID'si ile)
                string targetFilePath = Path.Combine(userDirectory, Path.GetFileName(filePath));

                // Aynı kullanıcı için aynı isimli dataset var mı kontrol et
                bool dbExists = DatabaseHelper.DatasetExists(userId, fileNameWithoutExtension);
                bool fileExists = File.Exists(targetFilePath);

                if (dbExists || fileExists)
                {
                    string message = dbExists && fileExists
                        ? "Bu veri seti hem veritabanında hem de dizinde mevcut."
                        : dbExists
                            ? "Bu veri seti veritabanında mevcut."
                            : "Bu isimde bir dosya dizinde mevcut.";

                    var result = MessageBox.Show(
                        $"{message}\nÜzerine yazmak ister misiniz?",
                        "Uyarı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        if (dbExists)
                        {
                            // Veritabanındaki kaydı tamamen sil
                            DatabaseHelper.DeleteDataset(userId, fileNameWithoutExtension);
                        }
                        if (fileExists)
                        {
                            try
                            {
                                // Dosyayı sil
                                File.Delete(targetFilePath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Eski dosya silinirken hata oluştu: {ex.Message}",
                                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }
                }

                // Dosya var mı kontrol et
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Dosya bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Dosya boyutu kontrolü (10 MB üzeri)
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Dosya boyutu 10 MB'den büyük olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // Önce dosyayı oku ve doğrula
                    dataTable = new DataTable();
                    using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
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

                    // Dosya boş mu kontrol et
                    if (dataTable.Rows.Count == 0)
                    {
                        MessageBox.Show("Dosya boş.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Dosya geçerliyse kullanıcının dizinine kopyala
                    File.Copy(filePath, targetFilePath, true);

                    // Veri setini işleme
                    this.dataTable = dataTable;

                    // Veri setini veritabanına kaydet
                    bool saveResult = DatabaseHelper.SaveDataset(userId, fileNameWithoutExtension, targetFilePath);

                    if (saveResult)
                    {
                        MessageBox.Show("Veri seti başarıyla yüklendi ve kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Veritabanına kayıt başarısız olursa dosyayı sil
                        if (File.Exists(targetFilePath))
                        {
                            File.Delete(targetFilePath);
                        }
                        MessageBox.Show("Veri seti kaydedilirken bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Hata durumunda kopyalanan dosyayı temizle
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                    }
                    MessageBox.Show($"Beklenmeyen bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            return;
        }

        private void VeriSetiYükle_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            verisetiYükle();
        }

        private void veriSetiniGörüntüle_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (dataTable != null)
            {
                VeriSetiPanel1.Controls.Clear(); // Paneldeki önceki kontrolleri temizle

                // DataGridView ekleyelim ve panelin içine yerleştirelim
                DataGridView dataGridView = new DataGridView
                {
                    Name = "dataGridView",
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    DataSource = dataTable,
                    AllowUserToAddRows = false // Kullanıcının yeni satır eklemesini engelle
                };
                VeriSetiPanel1.Controls.Add(dataGridView);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // Sütun içeriğini analiz ederek veri tipini tahmin eden fonksiyon
        public Type TahminEdilenVeriTipi(DataTable dataTable, DataColumn column)
        {
            bool tümüInteger = true;
            bool tümüDecimal = true;
            bool tümüDateTime = true;

            // Kültürel ayarları kullanarak dönüşüm yap
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            foreach (DataRow row in dataTable.Rows)
            {
                object değer = row[column];
                if (değer == DBNull.Value) continue; // Boş değerleri atla

                string değerMetni = değer.ToString();

                // Sayısal mı?
                if (!int.TryParse(değerMetni, System.Globalization.NumberStyles.Integer, culture, out _))
                    tümüInteger = false;
                if (!decimal.TryParse(değerMetni, System.Globalization.NumberStyles.Number, culture, out _))
                    tümüDecimal = false;

                // Tarih mi?
                if (!DateTime.TryParse(değerMetni, culture, System.Globalization.DateTimeStyles.None, out _))
                    tümüDateTime = false;

                // Hiçbiri değilse metin olduğunu varsay
                if (!tümüInteger && !tümüDecimal && !tümüDateTime)
                    return typeof(string);
            }

            // Öncelikli veri tipini döndür
            if (tümüInteger) return typeof(int);
            if (tümüDecimal) return typeof(decimal);
            if (tümüDateTime) return typeof(DateTime);

            // Varsayılan olarak metin tipi
            return typeof(string);
        }

        public void veriSetiniGörüntüle(DataTable dataTable)
        {
            if (dataTable != null)
            {
                VeriSetiPanel1.Controls.Clear(); // Paneldeki önceki kontrolleri temizle

                // DataGridView ekleyelim ve panelin içine yerleştirelim
                DataGridView dataGridView = new DataGridView
                {
                    Name = "dataGridView",
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    DataSource = dataTable,
                    AllowUserToAddRows = false // Kullanıcının yeni satır eklemesini engelle
                };
                VeriSetiPanel1.Controls.Add(dataGridView);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // Sayısal sütunları almak
        private List<string> GetNumericalColumns()
        {
            List<string> numericalColumns = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                Type columnType = TahminEdilenVeriTipi(dataTable, column);

                // Sayısal veri tiplerini kontrol et
                if (columnType == typeof(int) || columnType == typeof(double) || columnType == typeof(decimal) ||
                    columnType == typeof(float) || columnType == typeof(long) || columnType == typeof(short) ||
                    columnType == typeof(int?) || columnType == typeof(double?) || columnType == typeof(decimal?) ||
                    columnType == typeof(float?) || columnType == typeof(long?) || columnType == typeof(short?))
                {
                    numericalColumns.Add(column.ColumnName);
                }
            }
            return numericalColumns;
        }

        // Kullanıcıdan sütun seçmek için form oluşturma
        private List<string> SelectColumnsForAnalysis(List<string> availableColumns, string formTitle)
        {
            Form selectColumnsForm = new Form
            {
                Text = formTitle,
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent
            };

            CheckedListBox checkedListBox = new CheckedListBox
            {
                Location = new Point(10, 10),
                Size = new Size(360, 200)
            };

            foreach (var column in availableColumns)
            {
                checkedListBox.Items.Add(column);
            }

            selectColumnsForm.Controls.Add(checkedListBox);

            Button calculateButton = new Button
            {
                Text = "Hesapla",
                Location = new Point(150, 220),
                DialogResult = DialogResult.OK
            };

            selectColumnsForm.Controls.Add(calculateButton);

            return selectColumnsForm.ShowDialog() == DialogResult.OK ? checkedListBox.CheckedItems.Cast<string>().ToList() : new List<string>();
        }

        // IQR hesaplama işlemleri
        private void CalculateIQRForColumns(List<string> selectedColumns)
        {
            Dictionary<string, (double Q1, double Q3, double IQR)> iqrResults = new Dictionary<string, (double Q1, double Q3, double IQR)>();

            foreach (string column in selectedColumns)
            {
                List<double> values = new List<double>();
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row[column] != DBNull.Value)
                    {
                        values.Add(Convert.ToDouble(row[column]));
                    }
                }

                values.Sort();
                int count = values.Count;

                double Q1 = values[(int)(0.25 * (count - 1))];
                double Q3 = values[(int)(0.75 * (count - 1))];
                double IQR = Q3 - Q1;

                iqrResults[column] = (Q1, Q3, IQR);
            }

            ShowIQROnlyResults(iqrResults);
        }

        private void CalculateOutliersForColumns(List<string> selectedColumns)
        {
            Dictionary<string, (double Q1, double Q3, double IQR, List<double> Outliers)> outlierResults = new Dictionary<string, (double Q1, double Q3, double IQR, List<double> Outliers)>();

            foreach (string column in selectedColumns)
            {
                List<double> values = new List<double>();
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row[column] != DBNull.Value)
                    {
                        values.Add(Convert.ToDouble(row[column]));
                    }
                }

                values.Sort();
                int count = values.Count;

                double Q1 = values[(int)(0.25 * (count - 1))];
                double Q3 = values[(int)(0.75 * (count - 1))];
                double IQR = Q3 - Q1;

                double lowerLimit = Q1 - 1.5 * IQR;
                double upperLimit = Q3 + 1.5 * IQR;

                List<double> outliers = values.Where(v => v < lowerLimit || v > upperLimit).ToList();

                outlierResults[column] = (Q1, Q3, IQR, outliers);
            }

            ShowOutlierResults(outlierResults);
        }

        private void ShowIQROnlyResults(Dictionary<string, (double Q1, double Q3, double IQR)> iqrResults)
        {
            // Yeni bir form oluştur
            Form resultForm = new Form
            {
                Text = "IQR Sonuçları",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            // ListView oluştur ve ayarlarını yap
            ListView listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill
            };

            // Sütun başlıklarını ekle
            listView.Columns.Add("Sütun Adı", 150);
            listView.Columns.Add("Q1", 100);
            listView.Columns.Add("Q3", 100);
            listView.Columns.Add("IQR", 100);

            // Sonuçları ekle
            foreach (var result in iqrResults)
            {
                ListViewItem item = new ListViewItem(result.Key);
                item.SubItems.Add(result.Value.Q1.ToString("N2"));
                item.SubItems.Add(result.Value.Q3.ToString("N2"));
                item.SubItems.Add(result.Value.IQR.ToString("N2"));
                listView.Items.Add(item);
            }

            // ListView'i forma ekle
            resultForm.Controls.Add(listView);

            // Formu göster
            resultForm.ShowDialog();
        }


        private void ShowOutlierResults(Dictionary<string, (double Q1, double Q3, double IQR, List<double> Outliers)> outlierResults)
        {
            diğerAnalizlerPanel.Controls.Clear();

            // Scrollable panel oluştur
            Panel scrollablePanel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill
            };
            diğerAnalizlerPanel.Controls.Add(scrollablePanel);

            // "Aykırı Değer Analizi" başlığı
            Label titleLabel = new Label
            {
                Text = "Aykırı Değer Analizi",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(10, 10), // Sol üst köşe
                AutoSize = true
            };
            scrollablePanel.Controls.Add(titleLabel);

            int yOffset = titleLabel.Bottom + 20; // Başlığın altına yerleştirme
            int xOffset = 10;

            // Sütun adlarını ekleme
            foreach (var columnName in outlierResults.Keys)
            {
                Label columnLabel = new Label
                {
                    Text = columnName,
                    Font = new Font("Arial", 12, FontStyle.Bold), // Yazı boyutu büyütüldü
                    Location = new Point(xOffset, yOffset),
                    AutoSize = true
                };
                scrollablePanel.Controls.Add(columnLabel);
                xOffset += 200; // Sütunlar arasında daha fazla boşluk
            }

            yOffset += 40; // Sütun adlarının altına geçiş
            xOffset = 10; // Yeniden başa dön

            foreach (var result in outlierResults)
            {
                // IQR ve sınırları göster
                double lowerLimit = result.Value.Q1 - 1.5 * result.Value.IQR;
                double upperLimit = result.Value.Q3 + 1.5 * result.Value.IQR;

                Label iqrLabel = new Label
                {
                    Text = $"IQR: {result.Value.IQR:F2}",
                    Location = new Point(xOffset, yOffset),
                    AutoSize = true
                };
                scrollablePanel.Controls.Add(iqrLabel);

                Label lowerLimitLabel = new Label
                {
                    Text = $"Alt Sınır: {lowerLimit:F2}",
                    Location = new Point(xOffset, iqrLabel.Bottom + 5),
                    AutoSize = true
                };
                scrollablePanel.Controls.Add(lowerLimitLabel);

                Label upperLimitLabel = new Label
                {
                    Text = $"Üst Sınır: {upperLimit:F2}",
                    Location = new Point(xOffset, lowerLimitLabel.Bottom + 5),
                    AutoSize = true
                };
                scrollablePanel.Controls.Add(upperLimitLabel);

                Label outlierCountLabel = new Label
                {
                    Text = $"Aykırı Değer Sayısı: {result.Value.Outliers.Count}",
                    Location = new Point(xOffset, upperLimitLabel.Bottom + 5),
                    AutoSize = true
                };
                scrollablePanel.Controls.Add(outlierCountLabel);

                // Aykırı değer sayısı 0'dan büyükse "Göster" butonu ekle
                if (result.Value.Outliers.Count > 0)
                {
                    Button showOutliersButton = new Button
                    {
                        Text = "Göster",
                        Location = new Point(xOffset, outlierCountLabel.Bottom + 5),
                        AutoSize = true
                    };
                    showOutliersButton.Click += (s, e) => ShowOutliers(result.Key, result.Value.Outliers);
                    scrollablePanel.Controls.Add(showOutliersButton);
                }

                xOffset += 200; // Bir sonraki sütuna geçiş
            }
        }

        private void ShowOutliers(string columnName, List<double> outliers)
        {
            // Yeni bir form açarak aykırı değerleri detaylı şekilde gösterir
            Form outlierForm = new Form
            {
                Text = $"{columnName} Sütunu - Aykırı Değerler",
                Size = new Size(400, 300),
                AutoScroll = true
            };

            TextBox outliersTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Text = string.Join(", ", outliers)
            };

            outlierForm.Controls.Add(outliersTextBox);
            outlierForm.ShowDialog();
        }



        private void sayısalAnalizTablo_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // TableLayoutPanel oluştur
                TableLayoutPanel tableLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    ColumnCount = 9, // Kolonlar: Sütun Adı, Ortalama, Medyan, Mod, Min, Max, Standart Sapma, Varyans, IQR
                    RowCount = numericalColumns.Count + 1 // Başlıklar + Sütun sayısı
                };

                // Başlıkları ekle
                tableLayout.Controls.Add(new Label { Text = "Sütun Adı", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 0, 0);
                tableLayout.Controls.Add(new Label { Text = "Ortalama", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 1, 0);
                tableLayout.Controls.Add(new Label { Text = "Medyan", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 2, 0);
                tableLayout.Controls.Add(new Label { Text = "Mod", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 3, 0);
                tableLayout.Controls.Add(new Label { Text = "Min", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 4, 0);
                tableLayout.Controls.Add(new Label { Text = "Max", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 5, 0);
                tableLayout.Controls.Add(new Label { Text = "Standart Sapma", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 6, 0);
                tableLayout.Controls.Add(new Label { Text = "Varyans", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 7, 0);
                tableLayout.Controls.Add(new Label { Text = "IQR", AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) }, 8, 0);

                // Sayısal sütunlar için istatistik hesapla ve ekle
                int rowIndex = 1; // İlk satır başlık olduğu için 1'den başlıyoruz
                foreach (string column in numericalColumns)
                {
                    // Sütundaki değerleri al ve sıralı listeye çevir
                    List<double> values = dataTable.AsEnumerable()
                        .Where(row => row[column] != DBNull.Value)
                        .Select(row => Convert.ToDouble(row[column]))
                        .OrderBy(val => val)
                        .ToList();

                    if (values.Count == 0) continue;

                    // Ortalama, Medyan, Mod, Min ve Max değerlerini hesapla
                    double mean = values.Average();
                    double median = values.Count % 2 == 0
                        ? (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0
                        : values[values.Count / 2];
                    double mode = values.GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key)
                        .First().Key;
                    double min = values.Min();
                    double max = values.Max();

                    // Standart sapma ve varyans hesapla
                    double variance = values.Average(v => Math.Pow(v - mean, 2));
                    double standardDeviation = Math.Sqrt(variance);

                    // Çeyrekler ve IQR hesapla
                    double q1 = values[(int)(values.Count * 0.25)];
                    double q3 = values[(int)(values.Count * 0.75)];
                    double iqr = q3 - q1;

                    // Hesaplanan değerleri tabloya ekle
                    tableLayout.Controls.Add(new Label { Text = column, AutoSize = true }, 0, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = mean.ToString("F2"), AutoSize = true }, 1, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = median.ToString("F2"), AutoSize = true }, 2, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = mode.ToString("F2"), AutoSize = true }, 3, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = min.ToString("F2"), AutoSize = true }, 4, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = max.ToString("F2"), AutoSize = true }, 5, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = standardDeviation.ToString("F2"), AutoSize = true }, 6, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = variance.ToString("F2"), AutoSize = true }, 7, rowIndex);
                    tableLayout.Controls.Add(new Label { Text = iqr.ToString("F2"), AutoSize = true }, 8, rowIndex);

                    rowIndex++;
                }

                // TableLayoutPanel'i diğerAnalizlerPanel'e ekle
                diğerAnalizlerPanel.Controls.Clear();
                diğerAnalizlerPanel.Controls.Add(tableLayout);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        // İstatistik panelini oluşturan yardımcı metod
        private void CreateAndShowStatisticsPanel()
        {
            List<string> numericalColumns = GetNumericalColumns();
            TableLayoutPanel statsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 8,
                RowCount = numericalColumns.Count + 1,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single // Görünürlük için opsiyonel
            };

            // Başlıkları ekle
            string[] headers = { "Sütun", "Ortalama", "Medyan", "Mod", "Std. Sapma", "Varyans", "Min-Max", "IQR" };
            for (int i = 0; i < headers.Length; i++)
            {
                statsPanel.Controls.Add(new Label
                {
                    Text = headers[i],
                    Font = new Font("Arial", 11, FontStyle.Bold),
                    Anchor = AnchorStyles.Left,
                    AutoSize = true
                }, i, 0);
            }

            // İstatistikleri hesapla ve göster
            int row = 1;
            foreach (string column in numericalColumns)
            {
                var values = dataTable.AsEnumerable()
                    .Where(r => r[column] != DBNull.Value)
                    .Select(r => Convert.ToDouble(r[column]))
                    .ToList();

                if (values.Count > 0)
                {
                    double mean = values.Average();
                    double median = CalculateMedian(values);
                    double mode = CalculateMode(values);
                    double stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
                    double variance = stdDev * stdDev;
                    double min = values.Min();
                    double max = values.Max();
                    double iqr = CalculateIQR(values);

                    // Her bir hücreyi ayrı ayrı ekle ve konumlandır
                    statsPanel.Controls.Add(new Label { Text = column, AutoSize = true }, 0, row);
                    statsPanel.Controls.Add(new Label { Text = $"{mean:F2}", AutoSize = true }, 1, row);
                    statsPanel.Controls.Add(new Label { Text = $"{median:F2}", AutoSize = true }, 2, row);
                    statsPanel.Controls.Add(new Label { Text = $"{mode:F2}", AutoSize = true }, 3, row);
                    statsPanel.Controls.Add(new Label { Text = $"{stdDev:F2}", AutoSize = true }, 4, row);
                    statsPanel.Controls.Add(new Label { Text = $"{variance:F2}", AutoSize = true }, 5, row);
                    statsPanel.Controls.Add(new Label { Text = $"{min:F2} - {max:F2}", AutoSize = true }, 6, row);
                    statsPanel.Controls.Add(new Label { Text = $"{iqr:F2}", AutoSize = true }, 7, row);
                    row++;
                }
            }

            // Her sütunun genişliğini eşit ayarla
            for (int i = 0; i < statsPanel.ColumnCount; i++)
            {
                statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / statsPanel.ColumnCount));
            }

            görselleştirmePanel.Controls.Clear();
            görselleştirmePanel.Controls.Add(statsPanel);
        }

        // Yardımcı metodlar
        private double CalculateMedian(List<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
            return sortedValues[count / 2];
        }

        private double CalculateMode(List<double> values)
        {
            return values.GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
        }

        private double CalculateStdDev(List<double> values, double mean)
        {
            return Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
        }

        private double CalculateIQR(List<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;

            double q1 = CalculateMedian(sortedValues.Take(count / 2).ToList());
            double q3 = CalculateMedian(sortedValues.Skip((count + 1) / 2).ToList());

            return q3 - q1;
        }








        private void çizgiGrafiğiItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Çizgi Grafiği Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in numericalColumns)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların çizgi grafiğini oluştur
                    List<string> selectedColumns = checkedListBox.CheckedItems.Cast<string>().ToList();
                    if (selectedColumns.Count < 2)
                    {
                        MessageBox.Show("Lütfen bir kategori sütunu ve bir değer sütunu seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string categoryColumn = selectedColumns[0];
                    string valueColumn = selectedColumns[1];

                    // Görselleştirme.cs'deki ModernÇizgiGrafiğiOlustur metodunu çağır
                    var görselleştirme = new Görselleştirme();
                    görselleştirme.ModernÇizgiGrafiğiOlustur(görselleştirmePanel, dataTable, categoryColumn, valueColumn);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }





        private void ekranıTemizle_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // VeriSetiPanel1 üzerindeki alanı temizle
            VeriSetiPanel1.Controls.Clear();

            // diğerAnalizlerPanel üzerindeki alanı temizle
            diğerAnalizlerPanel.Controls.Clear();

            // görselleştirmePanel üzerindeki alanı temizle
            görselleştirmePanel.Controls.Clear();
        }

        private void satırSayısıItem_Click_1(object sender, EventArgs e)
        {

            if (dataTable != null)
            {
                int rowCount = dataTable.Rows.Count;

                // Sonucu gösteren bir Label ekleyelim
                Label rowCountLabel = new Label
                {
                    Text = $"Veri setindeki satır sayısı: {rowCount}",
                    AutoSize = true,
                    Location = new Point(10, 10) // Label'in konumunu ayarlayın
                };

                // Paneldeki önceki kontrolleri temizle ve yeni Label'i ekle
                diğerAnalizlerPanel.Controls.Clear();
                diğerAnalizlerPanel.Controls.Add(rowCountLabel);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void sütunSayısıItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                int columnCount = dataTable.Columns.Count;

                // Sonucu gösteren bir Label ekleyelim
                Label columnCountLabel = new Label
                {
                    Text = $"Veri setindeki sütun sayısı: {columnCount}",
                    AutoSize = true,
                    Location = new Point(10, 10) // Label'in konumunu ayarlayın
                };

                // Paneldeki önceki kontrolleri temizle ve yeni Label'i ekle
                diğerAnalizlerPanel.Controls.Clear();
                diğerAnalizlerPanel.Controls.Add(columnCountLabel);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void sütunİsimleriItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Paneldeki önceki kontrolleri temizle
                diğerAnalizlerPanel.Controls.Clear();

                // Sütun isimlerini gösteren bir Label ekleyelim
                int xOffset = 10; // X konumu için başlangıç değeri
                int yOffset = 10; // Y konumu için başlangıç değeri
                int panelWidth = diğerAnalizlerPanel.Width; // Panelin genişliği

                foreach (DataColumn column in dataTable.Columns)
                {
                    Label columnLabel = new Label
                    {
                        Text = column.ColumnName,
                        AutoSize = true,
                        Location = new Point(xOffset, yOffset) // Label'in konumunu ayarlayın
                    };

                    // Label'in genişliğini al
                    columnLabel.AutoSize = true;
                    diğerAnalizlerPanel.Controls.Add(columnLabel);
                    columnLabel.AutoSize = false;

                    // X konumunu güncelle
                    xOffset += columnLabel.Width + 10; // Bir sonraki Label için X konumunu artırın

                    // Eğer X konumu panelin genişliğini aşarsa, bir sonraki satıra geç
                    if (xOffset + columnLabel.Width > panelWidth)
                    {
                        xOffset = 10; // X konumunu sıfırla
                        yOffset += columnLabel.Height + 10; // Y konumunu artır
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void sütunVeriTipiItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Paneldeki önceki kontrolleri temizle
                diğerAnalizlerPanel.Controls.Clear();

                // Sütun isimlerini ve tahmini veri tiplerini gösteren Label'leri ekleyelim
                int xOffset = 10; // X başlangıç konumu
                int yOffset = 10; // Y başlangıç konumu
                int panelWidth = diğerAnalizlerPanel.Width; // Panelin genişliği
                int labelSpacing = 10; // Label'ler arası boşluk

                foreach (DataColumn column in dataTable.Columns)
                {
                    // Gerçek veri tipini tahmin et
                    Type gerçekVeriTipi = TahminEdilenVeriTipi(dataTable, column);

                    // Yeni bir Label oluştur
                    Label columnLabel = new Label
                    {
                        Text = $"{column.ColumnName} ({gerçekVeriTipi.Name})",
                        AutoSize = true
                    };

                    // Label'i geçerli konuma yerleştir
                    columnLabel.Location = new Point(xOffset, yOffset);

                    // Label'i panele ekle
                    diğerAnalizlerPanel.Controls.Add(columnLabel);

                    // X konumunu güncelle
                    xOffset += columnLabel.Width + labelSpacing;

                    // Eğer X konumu panelin genişliğini aşarsa bir sonraki satıra geç
                    if (xOffset + columnLabel.Width > panelWidth)
                    {
                        xOffset = 10; // X başlangıç konumuna sıfırla
                        yOffset += columnLabel.Height + labelSpacing; // Y konumunu artır
                    }
                }

                // Panelin içeriğinin güncellendiğinden emin olun
                diğerAnalizlerPanel.Refresh();
            }
            else
            {
                // Veri seti yüklenmemişse uyarı göster
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void nullVeriSayısıItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Null veya boş veri sayısını hesapla
                int nullVeriSayisi = 0;

                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        if (item == null || item == DBNull.Value || string.IsNullOrWhiteSpace(item.ToString()))
                        {
                            nullVeriSayisi++;
                        }
                    }
                }

                // Sonucu gösteren bir Label ekleyelim
                Label nullVeriSayisiLabel = new Label
                {
                    Text = $"Veri setindeki null veya boş veri sayısı: {nullVeriSayisi}",
                    AutoSize = true,
                    Location = new Point(10, 10) // Label'in konumunu ayarlayın
                };

                // Paneldeki önceki kontrolleri temizle ve yeni Label'i ekle
                diğerAnalizlerPanel.Controls.Clear();
                diğerAnalizlerPanel.Controls.Add(nullVeriSayisiLabel);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ortalamaItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Ortalamasını Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların ortalamalarını hesapla
                    Dictionary<string, double> ortalamalar = new Dictionary<string, double>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        double toplam = 0;
                        int sayac = 0;
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                toplam += Convert.ToDouble(row[sutun]);
                                sayac++;
                            }
                        }
                        if (sayac > 0)
                        {
                            ortalamalar[sutun] = toplam / sayac;
                        }
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var ortalama in ortalamalar)
                    {
                        Label ortalamaLabel = new Label
                        {
                            Text = $"{ortalama.Key} sütununun ortalaması: {ortalama.Value}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(ortalamaLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MedyanItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Medyanını Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların medyanlarını hesapla
                    Dictionary<string, double> medyanlar = new Dictionary<string, double>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        List<double> degerler = new List<double>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                degerler.Add(Convert.ToDouble(row[sutun]));
                            }
                        }
                        degerler.Sort();
                        double medyan;
                        int sayac = degerler.Count;
                        if (sayac % 2 == 0)
                        {
                            medyan = (degerler[sayac / 2 - 1] + degerler[sayac / 2]) / 2;
                        }
                        else
                        {
                            medyan = degerler[sayac / 2];
                        }
                        medyanlar[sutun] = medyan;
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var medyan in medyanlar)
                    {
                        Label medyanLabel = new Label
                        {
                            Text = $"{medyan.Key} sütununun medyanı: {medyan.Value}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(medyanLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void modItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Modunu Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların modlarını hesapla
                    Dictionary<string, double> modlar = new Dictionary<string, double>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        Dictionary<double, int> frekanslar = new Dictionary<double, int>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                double deger = Convert.ToDouble(row[sutun]);
                                if (frekanslar.ContainsKey(deger))
                                {
                                    frekanslar[deger]++;
                                }
                                else
                                {
                                    frekanslar[deger] = 1;
                                }
                            }
                        }
                        double mod = frekanslar.OrderByDescending(x => x.Value).First().Key;
                        modlar[sutun] = mod;
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var mod in modlar)
                    {
                        Label modLabel = new Label
                        {
                            Text = $"{mod.Key} sütununun modu: {mod.Value}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(modLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void standartSapmaItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Standart Sapmasını Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların standart sapmalarını hesapla
                    Dictionary<string, double> standartSapmalar = new Dictionary<string, double>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        List<double> degerler = new List<double>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                degerler.Add(Convert.ToDouble(row[sutun]));
                            }
                        }
                        double ortalama = degerler.Average();
                        double toplamKareFark = degerler.Sum(d => Math.Pow(d - ortalama, 2));
                        double standartSapma = Math.Sqrt(toplamKareFark / degerler.Count);
                        standartSapmalar[sutun] = standartSapma;
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var standartSapma in standartSapmalar)
                    {
                        Label standartSapmaLabel = new Label
                        {
                            Text = $"{standartSapma.Key} sütununun standart sapması: {standartSapma.Value}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(standartSapmaLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void VaryansItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Varyansını Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların varyanslarını hesapla
                    Dictionary<string, double> varyanslar = new Dictionary<string, double>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        List<double> degerler = new List<double>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                degerler.Add(Convert.ToDouble(row[sutun]));
                            }
                        }
                        double ortalama = degerler.Average();
                        double toplamKareFark = degerler.Sum(d => Math.Pow(d - ortalama, 2));
                        double varyans = toplamKareFark / degerler.Count;
                        varyanslar[sutun] = varyans;
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var varyans in varyanslar)
                    {
                        Label varyansLabel = new Label
                        {
                            Text = $"{varyans.Key} sütununun varyansı: {varyans.Value}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(varyansLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void minMaxItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> sayisalSutunlar = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                    if (veriTipi == typeof(int) || veriTipi == typeof(double) || veriTipi == typeof(decimal))
                    {
                        sayisalSutunlar.Add(column.ColumnName);
                    }
                }

                if (sayisalSutunlar.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Min-Max Değerlerini Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in sayisalSutunlar)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Hesapla",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların min-max değerlerini hesapla
                    Dictionary<string, (double Min, double Max)> minMaxDegerler = new Dictionary<string, (double Min, double Max)>();
                    foreach (string sutun in checkedListBox.CheckedItems)
                    {
                        List<double> degerler = new List<double>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            if (row[sutun] != DBNull.Value)
                            {
                                degerler.Add(Convert.ToDouble(row[sutun]));
                            }
                        }
                        double min = degerler.Min();
                        double max = degerler.Max();
                        minMaxDegerler[sutun] = (min, max);
                    }

                    // Sonuçları gösteren Label'leri ekle
                    diğerAnalizlerPanel.Controls.Clear();
                    int yOffset = 10;
                    foreach (var minMax in minMaxDegerler)
                    {
                        Label minMaxLabel = new Label
                        {
                            Text = $"{minMax.Key} sütununun min değeri: {minMax.Value.Min}, max değeri: {minMax.Value.Max}",
                            AutoSize = true,
                            Location = new Point(10, yOffset)
                        };
                        diğerAnalizlerPanel.Controls.Add(minMaxLabel);
                        yOffset += 25;
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void IQRItem_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal sütunları al
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.",
                                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcıdan sütun seçimi yapmasını iste
                List<string> selectedColumns = SelectColumnsForAnalysis(numericalColumns,
                                                "IQR Hesaplamak İstediğiniz Sütunları Seçin");

                if (selectedColumns.Any())
                {
                    // IQR hesaplaması yap ve sonuçları göster
                    CalculateIQRForColumns(selectedColumns);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.",
                                "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DisplayIQRResults(Dictionary<string, (decimal Q1, decimal Q3, decimal IQR)> iqrResults)
        {
            // Sonuçları göstermek için yeni bir DataTable oluştur
            DataTable resultsTable = new DataTable();
            resultsTable.Columns.Add("Sütun Adı");
            resultsTable.Columns.Add("1. Çeyrek (Q1)");
            resultsTable.Columns.Add("3. Çeyrek (Q3)");
            resultsTable.Columns.Add("IQR Değeri");

            // IQR sonuçlarını tabloya ekle
            foreach (var result in iqrResults)
            {
                resultsTable.Rows.Add(result.Key,
                                      result.Value.Q1.ToString("F2"),
                                      result.Value.Q3.ToString("F2"),
                                      result.Value.IQR.ToString("F2"));
            }

            // Sonuçları göstermek için bir DataGridView kullan
            Form resultsForm = new Form
            {
                Text = "IQR Sonuçları",
                Size = new Size(600, 400)
            };

            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = resultsTable,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            resultsForm.Controls.Add(dgv);
            resultsForm.ShowDialog();
        }

        private void AykırıDeğerItem_Click_1(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<string> selectedColumns = SelectColumnsForAnalysis(numericalColumns, "Aykırı Değerleri Tespit Etmek İstediğiniz Sütunları Seçin");

                if (selectedColumns.Any())
                {
                    CalculateOutliersForColumns(selectedColumns);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void NullVeriDoldurItem_Click(object sender, EventArgs e)
        {
            if (dataTable == null)
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // DataTable'dan sütun adlarını alıyoruz
            List<string> columnNames = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList();

            // NullVeriDoldurmaForm formunu gösteriyoruz
            using (NullVeriDoldurmaForm form = new NullVeriDoldurmaForm(columnNames))
            {
                // Eğer formu başarıyla onayladıysak (Doldur butonuna tıklandı)
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string columnName = form.SelectedColumn;
                    string fillMethod = form.SelectedMethod;
                    string fixedValue = form.FixedValue;

                    // Geçerli bir sütun adı kontrolü
                    if (string.IsNullOrWhiteSpace(columnName) || !dataTable.Columns.Contains(columnName))
                    {
                        MessageBox.Show("Geçerli bir sütun adı girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Geçerli bir doldurma yöntemi kontrolü
                    if (string.IsNullOrWhiteSpace(fillMethod) ||
                        !(fillMethod.Equals("Ortalama", StringComparison.OrdinalIgnoreCase) ||
                          fillMethod.Equals("Medyan", StringComparison.OrdinalIgnoreCase) ||
                          fillMethod.Equals("Sabit Değer", StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Geçerli bir doldurma yöntemi girin (Ortalama, Medyan, Sabit Değer).",
                            "Hata",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // Null değerleri dolduruyoruz
                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (row[columnName] == DBNull.Value || string.IsNullOrWhiteSpace(row[columnName]?.ToString()))
                        {
                            if (fillMethod.Equals("Ortalama", StringComparison.OrdinalIgnoreCase))
                            {
                                double average = dataTable.AsEnumerable()
                                    .Where(r => r[columnName] != DBNull.Value && !string.IsNullOrWhiteSpace(r[columnName]?.ToString()))
                                    .Average(r => Convert.ToDouble(r[columnName]));

                                row[columnName] = average;
                            }
                            else if (fillMethod.Equals("Medyan", StringComparison.OrdinalIgnoreCase))
                            {
                                var sortedValues = dataTable.AsEnumerable()
                                    .Where(r => r[columnName] != DBNull.Value && !string.IsNullOrWhiteSpace(r[columnName]?.ToString()))
                                    .Select(r => Convert.ToDouble(r[columnName]))
                                    .OrderBy(v => v).ToList();

                                double median = sortedValues.Count % 2 == 0
                                    ? (sortedValues[sortedValues.Count / 2 - 1] + sortedValues[sortedValues.Count / 2]) / 2
                                    : sortedValues[sortedValues.Count / 2];

                                row[columnName] = median;
                            }
                            else if (fillMethod.Equals("Sabit Değer", StringComparison.OrdinalIgnoreCase))
                            {
                                row[columnName] = Convert.ChangeType(fixedValue, dataTable.Columns[columnName].DataType);
                            }
                        }
                    }

                    MessageBox.Show("Null değerler başarıyla dolduruldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("İşlem iptal edildi.", "İptal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }





        private void başkaHesabaGirişYap_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DialogResult result = MessageBox.Show("Bu işlemi yapmak istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {

                // Yeni giriş formunu aç
                girişFormu loginForm = new girişFormu();
                this.Hide();
                loginForm.ShowDialog();
                this.Close();
            }
        }

        private void çıkışYap_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (CikisOnayForm cikisForm = new CikisOnayForm())
            {
                cikisForm.ShowDialog();

                if (cikisForm.IsConfirmed)
                {
                    // Uygulamayı kapat
                    Application.Exit();
                }
                else
                {
                    // İşlem iptal edildi
                    MessageBox.Show("İşlem iptal edildi.", "İptal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void uygulamayıKapat_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DialogResult result = MessageBox.Show("Uygulamayı kapatmak istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
            // Kullanıcı 'No' derse hiçbir şey yapılmaz ve mesaj kutusu kapanır

        }

        private List<string> GetCategoricalColumns()
        {
            return dataTable.Columns.Cast<DataColumn>()
                .Where(col => col.DataType == typeof(string))
                .Select(col => col.ColumnName)
                .ToList();
        }






        private void PastaGrafiği_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının tek bir sütun seçmesini sağlamak için CheckedListBox yerine ListBox kullanıyoruz.
                Form sutunSecimFormu = new Form
                {
                    Text = "Pasta Grafiği Görüntülemek İstediğiniz Sütunu Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                ListBox listBox = new ListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200),
                    SelectionMode = SelectionMode.One
                };

                foreach (string sutun in numericalColumns)
                {
                    listBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(listBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    if (listBox.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen bir sütun seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selectedColumn = listBox.SelectedItem.ToString();

                    Görselleştirme görselleştirme = new Görselleştirme();
                    görselleştirme.ModernPastaGrafiğiOlustur(görselleştirmePanel, dataTable, selectedColumn, selectedColumn);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private void ÇizgiGrafiği_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının tek bir sütun seçmesi için form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Çizgi Grafiği Görüntülemek İstediğiniz Sütunu Seçin",
                    Size = new Size(400, 200),
                    StartPosition = FormStartPosition.CenterParent
                };

                Label label = new Label
                {
                    Text = "Sütun Seçin:",
                    Location = new Point(10, 20),
                    Size = new Size(360, 20)
                };
                sutunSecimFormu.Controls.Add(label);
                System.Windows.Forms.ComboBox comboBox = new System.Windows.Forms.ComboBox
                {
                    Location = new Point(10, 50),
                    Size = new Size(360, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                foreach (string sutun in numericalColumns)
                {
                    comboBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(comboBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 100),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    if (comboBox.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen bir sütun seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selectedColumn = comboBox.SelectedItem.ToString();

                    // Görselleştirme.cs'deki ModernÇizgiGrafiğiOlustur metodunu çağır
                    var görselleştirme = new Görselleştirme();
                    görselleştirme.ModernÇizgiGrafiğiOlustur(görselleştirmePanel, dataTable, selectedColumn, selectedColumn);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AlanGrafiği_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının sayısal sütunları seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Alan Grafiği Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                CheckedListBox checkedListBox = new CheckedListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200)
                };
                foreach (string sutun in numericalColumns)
                {
                    checkedListBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(checkedListBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen sütunların alan grafiğini oluştur
                    List<string> selectedColumns = checkedListBox.CheckedItems.Cast<string>().ToList();
                    if (selectedColumns.Count == 0)
                    {
                        MessageBox.Show("Lütfen en az bir sütun seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Kullanıcıdan kategori sütununu seçmesini iste
                    var kategoriSecimFormu = new Form
                    {
                        Text = "Kategori Sütununu Seçin",
                        Size = new Size(400, 200),
                        StartPosition = FormStartPosition.CenterParent
                    };
                    System.Windows.Forms.ComboBox kategoriComboBox = new System.Windows.Forms.ComboBox
                    {
                        Location = new Point(10, 45),
                        Size = new Size(360, 30),
                        DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
                    };


                    List<string> categoricalColumns = GetCategoricalColumns();
                    foreach (string sutun in categoricalColumns)
                    {
                        kategoriComboBox.Items.Add(sutun);
                    }

                    kategoriSecimFormu.Controls.Add(kategoriComboBox);

                    Button onaylaButton = new Button
                    {
                        Text = "Onayla",
                        Location = new Point(150, 100),
                        DialogResult = DialogResult.OK
                    };

                    kategoriSecimFormu.Controls.Add(onaylaButton);

                    // Kategori seçimini yap
                    if (kategoriSecimFormu.ShowDialog() == DialogResult.OK)
                    {
                        string selectedCategory = kategoriComboBox.SelectedItem?.ToString();
                        if (string.IsNullOrEmpty(selectedCategory))
                        {
                            MessageBox.Show("Lütfen bir kategori sütunu seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Görselleştirme sınıfındaki metodu çağır
                        görselleştirme.AlanGrafiğiOlustur(görselleştirmePanel, dataTable, selectedCategory, selectedColumns);
                    }
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DağılımGrafiği_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count < 2)
                {
                    MessageBox.Show("Veri setinde en az iki sayısal veri tipli sütun bulunmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının X ve Y sütunlarını seçmesi için bir form oluştur
                Form sutunSecimFormu = new Form
                {
                    Text = "Dağılım Grafiği Görüntülemek İstediğiniz Sütunları Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                Label xLabel = new Label
                {
                    Text = "X Sütunu Seçin:",
                    Location = new Point(10, 20),
                    Size = new Size(360, 20)
                };
                sutunSecimFormu.Controls.Add(xLabel);

                System.Windows.Forms.ComboBox xComboBox = new System.Windows.Forms.ComboBox
                {
                    Location = new Point(10, 50),
                    Size = new Size(360, 30),
                    DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
                };

                foreach (string sutun in numericalColumns)
                {
                    xComboBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(xComboBox);

                Label yLabel = new Label
                {
                    Text = "Y Sütunu Seçin:",
                    Location = new Point(10, 90),
                    Size = new Size(360, 20)
                };
                sutunSecimFormu.Controls.Add(yLabel);

                System.Windows.Forms.ComboBox yComboBox = new System.Windows.Forms.ComboBox
                {
                    Location = new Point(10, 120),
                    Size = new Size(360, 30),
                    DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
                };

                foreach (string sutun in numericalColumns)
                {
                    yComboBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(yComboBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 180),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                // Formu göster ve kullanıcıdan veri al
                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    if (xComboBox.SelectedItem == null || yComboBox.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen hem X hem de Y sütunlarını seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string xColumn = xComboBox.SelectedItem.ToString();
                    string yColumn = yComboBox.SelectedItem.ToString();

                    // Görselleştirme.cs'deki DağılımGrafiğiOlustur metodunu çağır
                    var görselleştirme = new Görselleştirme();
                    görselleştirme.DağılımGrafiğiOlustur(görselleştirmePanel, dataTable, xColumn, yColumn);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Histogram_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal veri tipli sütunları bul
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count == 0)
                {
                    MessageBox.Show("Veri setinde sayısal veri tipli sütun bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcının tek bir sütun seçmesini sağlamak için ListBox kullanıyoruz.
                Form sutunSecimFormu = new Form
                {
                    Text = "Histogram Grafiği Görüntülemek İstediğiniz Sütunu Seçin",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent
                };

                ListBox listBox = new ListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(360, 200),
                    SelectionMode = SelectionMode.One
                };

                foreach (string sutun in numericalColumns)
                {
                    listBox.Items.Add(sutun);
                }
                sutunSecimFormu.Controls.Add(listBox);

                Button hesaplaButton = new Button
                {
                    Text = "Göster",
                    Location = new Point(150, 220),
                    DialogResult = DialogResult.OK
                };
                sutunSecimFormu.Controls.Add(hesaplaButton);

                if (sutunSecimFormu.ShowDialog() == DialogResult.OK)
                {
                    if (listBox.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen bir sütun seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selectedColumn = listBox.SelectedItem.ToString();

                    Görselleştirme görselleştirme = new Görselleştirme();
                    görselleştirme.HistogramOlustur(görselleştirmePanel, dataTable, selectedColumn);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private static readonly System.Windows.Media.Brush[] CHART_COLORS = new System.Windows.Media.Brush[]
       {
            System.Windows.Media.Brushes.Red,
            System.Windows.Media.Brushes.Blue,
            System.Windows.Media.Brushes.Green,
            System.Windows.Media.Brushes.Orange,
            System.Windows.Media.Brushes.Purple,
            System.Windows.Media.Brushes.Yellow
       };
        private void RadarGrafiği_item_Click(object sender, EventArgs e)
        {
            if (dataTable == null)
            {
                MessageBox.Show("Önce bir veri seti yükleyin.",
                                "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sayısal sütunları al
            var sayisalSutunlar = GetNumericalColumns();

            if (sayisalSutunlar.Count < 3)
            {
                MessageBox.Show("Radar grafiği için en az 3 sayısal sütun gereklidir.",
                                "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Radar Grafiği Ayarları";
                form.Size = new Size(400, 400);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                // Değer sütunları seçimi
                var değerLabel = new Label
                {
                    Text = "Karşılaştırılacak Sütunlar:",
                    Location = new Point(10, 20),
                    AutoSize = true
                };
                form.Controls.Add(değerLabel);

                var değerListBox = new CheckedListBox
                {
                    Location = new Point(10, 50),
                    Size = new Size(360, 250),
                    CheckOnClick = true
                };
                foreach (var sutun in sayisalSutunlar)
                {
                    değerListBox.Items.Add(sutun);
                }
                form.Controls.Add(değerListBox);

                // Göster butonu
                var gösterButton = new Button
                {
                    Text = "Grafiği Oluştur",
                    Location = new Point(130, 320),
                    Size = new Size(120, 30),
                    DialogResult = DialogResult.OK
                };
                form.Controls.Add(gösterButton);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var seçilenSutunlar = değerListBox.CheckedItems.Cast<string>().ToList();

                    if (seçilenSutunlar.Count < 3)
                    {
                        MessageBox.Show("Lütfen en az 3 sütun seçin.",
                                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        // Grafik oluşturma
                        görselleştirme.RadarGrafiğiOlustur(görselleştirmePanel, dataTable, seçilenSutunlar);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Grafik oluşturulurken hata oluştu: {ex.Message}",
                                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }



        private void çoklukTablosu_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                Verisetiİşlemleri verisetiIslemleri = new Verisetiİşlemleri();
                verisetiIslemleri.ÇoklukTablosuOlustur(görselleştirmePanel, dataTable);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void korelasyonAnalizi_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                // Sayısal sütunları al
                List<string> numericalColumns = GetNumericalColumns();

                if (numericalColumns.Count < 2)
                {
                    MessageBox.Show("Korelasyon analizi için en az 2 sayısal sütun gereklidir.",
                                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcıdan sütun seçimi yapmasını iste
                List<string> selectedColumns = SelectColumnsForAnalysis(numericalColumns,
                                                "Korelasyon Analizi İçin Sütunları Seçin");

                if (selectedColumns.Any())
                {
                    Verisetiİşlemleri verisetiIslemleri = new Verisetiİşlemleri();
                    verisetiIslemleri.KorelasyonAnaliziOlustur(görselleştirmePanel, dataTable, selectedColumns);
                }
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.",
                                "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeğerDağılımıGörüntüle_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                Verisetiİşlemleri verisetiIslemleri = new Verisetiİşlemleri();
                verisetiIslemleri.DeğerDağılımıGörüntüle(görselleştirmePanel, dataTable);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void benzersizDeğerSayısı_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                Verisetiİşlemleri verisetiIslemleri = new Verisetiİşlemleri();
                verisetiIslemleri.BenzersizDeğerSayısıGörüntüle(görselleştirmePanel, dataTable);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void veriAnalizGrup_Click(object sender, EventArgs e)
        {

        }

        private void eksikDeğerTablosu_item_Click(object sender, EventArgs e)
        {
            if (dataTable != null)
            {
                Verisetiİşlemleri verisetiIslemleri = new Verisetiİşlemleri();
                verisetiIslemleri.EksikDeğerTablosuGörüntüle(görselleştirmePanel, dataTable);
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        // mainForm.cs
        private void TemaDegistir_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (var temaSecimFormu = new DevExpress.XtraEditors.XtraForm())
            {
                temaSecimFormu.Text = "Tema Seçimi";
                temaSecimFormu.Size = new Size(400, 500);
                temaSecimFormu.StartPosition = FormStartPosition.CenterParent;
                temaSecimFormu.FormBorderStyle = FormBorderStyle.FixedDialog;
                temaSecimFormu.MaximizeBox = false;
                temaSecimFormu.MinimizeBox = false;
                temaSecimFormu.Padding = new Padding(10);

                // Başlık Label'ı
                var titleLabel = new Label
                {
                    Text = "Tema Seçimi",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(15, 15)
                };
                temaSecimFormu.Controls.Add(titleLabel);

                // Alt başlık Label'ı
                var subtitleLabel = new Label
                {
                    Text = "Aşağıdaki temalardan birini seçin veya temayı iptal edin:",
                    Font = new Font("Segoe UI", 9),
                    AutoSize = true,
                    Location = new Point(15, 45)
                };
                temaSecimFormu.Controls.Add(subtitleLabel);

                // Tema ListBox'ı
                var temaListBox = new ListBox
                {
                    DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed,
                    ItemHeight = 60,
                    Location = new Point(15, 75),
                    Size = new Size(355, 300),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var temaSecenekleri = new[]
                {
            new { Name = "Temayı İptal Et", BackColor = Color.White, ForeColor = Color.Black, Description = "Varsayılan DevExpress temasına dön" },
            new { Name = "Koyu Tema", BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White, Description = "Göz yormayan koyu tema" },
            new { Name = "Mavi Tema", BackColor = Color.FromArgb(230, 240, 250), ForeColor = Color.Black, Description = "Ferah mavi tema" },
            new { Name = "Gece Mavisi", BackColor = Color.FromArgb(28, 46, 74), ForeColor = Color.White, Description = "Profesyonel koyu mavi tema" }
        };

                foreach (var tema in temaSecenekleri)
                {
                    temaListBox.Items.Add(tema);
                }

                temaListBox.DrawItem += (s, ev) =>
                {
                    if (ev.Index < 0) return;
                    ev.DrawBackground();

                    var tema = temaListBox.Items[ev.Index] as dynamic;
                    var bounds = ev.Bounds;
                    var g = ev.Graphics;

                    // Seçili öğe için arka plan
                    if ((ev.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(230, 240, 250)))
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }

                    // Tema renk örneği
                    using (var brush = new SolidBrush(tema.BackColor))
                    {
                        var colorRect = new Rectangle(bounds.X + 10, bounds.Y + 10, 40, 40);
                        g.FillRectangle(brush, colorRect);
                        g.DrawRectangle(Pens.Gray, colorRect);
                    }

                    // Tema adı
                    using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
                    {
                        g.DrawString(tema.Name, font, Brushes.Black, bounds.X + 60, bounds.Y + 10);
                    }

                    // Tema açıklaması
                    using (var font = new Font("Segoe UI", 8))
                    {
                        g.DrawString(tema.Description, font, Brushes.DimGray, bounds.X + 60, bounds.Y + 30);
                    }
                };

                temaListBox.SelectedIndex = 0;
                temaSecimFormu.Controls.Add(temaListBox);

                // Uygula butonu
                var applyButton = new DevExpress.XtraEditors.SimpleButton
                {
                    Text = "Uygula",
                    Location = new Point(15, 390),
                    Size = new Size(355, 40),
                    Appearance = {
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            }
                };
                applyButton.Click += (s, args) => temaSecimFormu.DialogResult = DialogResult.OK;
                temaSecimFormu.Controls.Add(applyButton);

                // Tema seçimi ve uygulama
                if (temaSecimFormu.ShowDialog() == DialogResult.OK && temaListBox.SelectedItem != null)
                {
                    var selectedTheme = (dynamic)temaListBox.SelectedItem;
                    try
                    {
                        string skinName;
                        switch (selectedTheme.Name)
                        {
                            case "Temayı İptal Et":
                                skinName = "DevExpress Style";
                                Settings.Default.LastTheme = "DevExpress Style";
                                break;
                            case "Koyu Tema":
                                skinName = "DevExpress Dark Style";
                                Settings.Default.LastTheme = "DevExpress Dark Style";
                                break;
                            case "Mavi Tema":
                                skinName = "Office 2010 Blue";
                                Settings.Default.LastTheme = "Office 2010 Blue";
                                break;
                            case "Gece Mavisi":
                                skinName = "Visual Studio 2013 Dark";
                                Settings.Default.LastTheme = "Visual Studio 2013 Dark";
                                break;
                            default:
                                skinName = "DevExpress Style";
                                Settings.Default.LastTheme = "DevExpress Style";
                                break;
                        }

                        // Temayı uygula
                        this.LookAndFeel.UseDefaultLookAndFeel = false;
                        this.LookAndFeel.SkinName = skinName;

                        // Tüm açık formları güncelle
                        foreach (Form form in Application.OpenForms)
                        {
                            if (form is DevExpress.XtraEditors.XtraForm xtraForm)
                            {
                                xtraForm.LookAndFeel.UseDefaultLookAndFeel = false;
                                xtraForm.LookAndFeel.SkinName = skinName;
                            }
                        }

                        // Tema renklerini kaydet
                        Settings.Default.ThemeBackColor = this.BackColor;
                        Settings.Default.ThemeForeColor = this.ForeColor.ToString();
                        Settings.Default.ThemeName = skinName;
                        Settings.Default.Save();

                        // Başarı mesajı göster
                        MessageBox.Show("Tema başarıyla uygulandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Tema uygulanırken hata oluştu: {ex.Message}",
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ApplyTheme(string themeName, Color backColor, Color foreColor)
        {
            try
            {
                // DevExpress tema ayarı
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = themeName.Contains("Koyu") || themeName.Contains("Gece")
                    ? "DevExpress Dark Style"
                    : "DevExpress Style";

                // Ribbon kontrolü için tema ayarları
                if (this.ribbonControl1 != null)
                {
                    this.ribbonControl1.Refresh();
                }

                // Panel ayarları
                foreach (Control control in this.Controls)
                {
                    if (control is DevExpress.XtraEditors.PanelControl ||
                        control is DevExpress.XtraEditors.GroupControl)
                    {
                        continue; // DevExpress kontrollerini atla
                    }
                    ApplyThemeToControl(control, themeName, backColor, foreColor);
                }

                // Tema tercihini kaydet
                Settings.Default.ThemeName = DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tema uygulanırken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ApplyThemeToControl(Control control, string themeName, Color backColor, Color foreColor)
        {
            // Panel ve GroupBox için renkleri uygula
            if (control is Panel || control is GroupBox)
            {
                control.BackColor = backColor;
                control.ForeColor = foreColor;
            }
            // TextBox için renkleri uygula
            else if (control is TextBox textBox)
            {
                textBox.BackColor = backColor;
                textBox.ForeColor = foreColor;
            }
            // DataGridView için renkleri uygula
            else if (control is DataGridView gridView)
            {
                gridView.BackgroundColor = backColor;
                gridView.DefaultCellStyle.BackColor = backColor;
                gridView.DefaultCellStyle.ForeColor = foreColor;
                gridView.ColumnHeadersDefaultCellStyle.BackColor = CalculateHeaderColor(backColor);
                gridView.ColumnHeadersDefaultCellStyle.ForeColor = foreColor;
            }
            // Button için özel tema
            else if (control is Button button)
            {
                if (themeName.Contains("Koyu") || themeName.Contains("Gece"))
                {
                    button.BackColor = CalculateButtonColor(backColor);
                    button.ForeColor = Color.White;
                }
                else
                {
                    button.BackColor = SystemColors.Control;
                    button.ForeColor = Color.Black;
                }
            }

            // Alt kontroller için tema uygula
            foreach (Control childControl in control.Controls)
            {
                ApplyThemeToControl(childControl, themeName, backColor, foreColor);
            }
        }


        private Color CalculateHeaderColor(Color baseColor)
        {
            return Color.FromArgb(
                Math.Max(baseColor.R - 20, 0),
                Math.Max(baseColor.G - 20, 0),
                Math.Max(baseColor.B - 20, 0)
            );
        }

        private Color CalculateButtonColor(Color baseColor)
        {
            return Color.FromArgb(
                Math.Min(baseColor.R + 30, 255),
                Math.Min(baseColor.G + 30, 255),
                Math.Min(baseColor.B + 30, 255)
            );
        }




        private string FormatDecimal(double value)
        {
            if (value >= 1000000)
                return $"{value / 1000000:N2}M";
            if (value >= 1000)
                return $"{value / 1000:N2}K";
            return $"{value:N2}";
        }



        private void VarsayılanAnalizler_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (dataTable != null)
            {
                // 1. Veri setini VeriSetiPanel1'de göster
                veriSetiniGörüntüle(dataTable);

                // 2. İstatistiksel panel oluştur ve görselleştirmePanel'e ekle
                CreateAndShowStatisticsPanel();

                // 3. Grafik panelini oluştur
                diğerAnalizlerPanel.Controls.Clear();

                // Kontrol paneli oluştur
                Panel controlPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 50,
                    Padding = new Padding(10),
                    BackColor = Color.WhiteSmoke
                };
                // Sütun seçimi için ComboBox
                System.Windows.Forms.ComboBox columnSelector = new System.Windows.Forms.ComboBox
                {
                    Width = 200,
                    Height = 30,
                    Location = new Point(10, 15),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10)
                };


                // Sayısal sütunları ComboBox'a ekle
                List<string> numericalColumns = GetNumericalColumns();
                columnSelector.Items.AddRange(numericalColumns.ToArray());

                // Varsayılan olarak ilk sütunu seç
                if (numericalColumns.Count > 0)
                    columnSelector.SelectedIndex = 0;
                // Güncelle butonu
                Button updateButton = new Button
                {
                    Text = "Grafiği Güncelle",
                    Width = 140,
                    Height = 30,
                    Location = new Point(220, 15),
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    FlatAppearance = { BorderSize = 0 }
                };

                // Grafik tipi seçimi için ComboBox
                System.Windows.Forms.ComboBox chartTypeSelector = new System.Windows.Forms.ComboBox
                {
                    Width = 150,
                    Height = 30,
                    Location = new Point(370, 15),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10)
                };

                // Grafik tiplerini ekle
                chartTypeSelector.Items.AddRange(new string[] {
            "Alan Grafiği",
            "Çizgi Grafiği",
            "Histogram"
        });
                chartTypeSelector.SelectedIndex = 0;

                // Grafik paneli
                var graphPanel = new DevExpress.XtraEditors.PanelControl
                {
                    Dock = DockStyle.Fill
                };

                // Güncelle butonuna tıklama olayı
                updateButton.Click += (s, args) =>
                {
                    if (columnSelector.SelectedItem == null) return;

                    string selectedColumn = columnSelector.SelectedItem.ToString();
                    string selectedChartType = chartTypeSelector.SelectedItem.ToString();

                    graphPanel.Controls.Clear();

                    switch (selectedChartType)
                    {
                        case "Alan Grafiği":
                            görselleştirme.AlanGrafiğiOlustur(graphPanel, dataTable,
                                dataTable.Columns[0].ColumnName, new List<string> { selectedColumn });
                            break;
                        case "Çizgi Grafiği":
                            görselleştirme.ModernÇizgiGrafiğiOlustur(graphPanel, dataTable,
                                dataTable.Columns[0].ColumnName, selectedColumn);
                            break;
                        case "Histogram":
                            görselleştirme.HistogramOlustur(graphPanel, dataTable, selectedColumn);
                            break;
                        case "Pasta Grafiği":
                            görselleştirme.ModernPastaGrafiğiOlustur(graphPanel, dataTable,
                                dataTable.Columns[0].ColumnName, selectedColumn);
                            break;
                    }
                };

                // Kontrolleri panele ekle
                controlPanel.Controls.AddRange(new Control[] {
            columnSelector,
            updateButton,
            chartTypeSelector
        });

                // Açıklama etiketi
                Label descriptionLabel = new Label
                {
                    Text = "İpucu: Farklı sütunları ve grafik tiplerini seçerek veriyi farklı şekillerde görselleştirebilirsiniz.",
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    BackColor = Color.WhiteSmoke,
                    Padding = new Padding(10, 5, 10, 5),
                    Font = new Font("Segoe UI", 9)
                };

                // Tüm panelleri ana panele ekle
                diğerAnalizlerPanel.Controls.AddRange(new Control[] {
            controlPanel,
            graphPanel,
            descriptionLabel
        });

                // İlk grafiği göster
                updateButton.PerformClick();
            }
            else
            {
                MessageBox.Show("Önce bir veri seti yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadDatasetFromFile(string filePath)
        {
            try
            {
                dataTable = new DataTable();
                using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
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
                
                // Dosya adını al ve currentDatasetName'e ata
                currentDatasetName = Path.GetFileNameWithoutExtension(filePath);
                
                MessageBox.Show("Veri seti başarıyla yüklendi.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri seti yüklenirken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private int GetCurrentDatasetId()
        {
            if (string.IsNullOrEmpty(currentDatasetName))
            {
                return 0;
            }

            string query = @"
                SELECT DatasetID 
                FROM Dataset 
                WHERE UserID = @UserID 
                AND IsDeleted = 0 
                AND DatasetName = @DatasetName";

            return DatabaseHelper.ExecuteScalar<int>(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@DatasetName", currentDatasetName);
            });
        }
        private void RaporuGörüntüle_btn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                var result = MessageBox.Show(
                    "Rapor oluşturmak için önce bir veri seti yüklemeniz gerekmektedir. Şimdi veri seti yüklemek ister misiniz?",
                    "Veri Seti Gerekli",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    verisetiYükle(); // Veri seti yükleme metodunu çağır
                    if (dataTable == null || dataTable.Rows.Count == 0)
                    {
                        return; // Veri seti yükleme başarısız olduysa çık
                    }
                }
                else
                {
                    return;
                }
            }

            try
            {
                // Mevcut dataset ID'sini al
                int currentDatasetId = GetCurrentDatasetId();

                if (string.IsNullOrEmpty(currentDatasetName))
                {
                    MessageBox.Show(
                        "Veri seti adı bulunamadı. Lütfen veri setini tekrar yükleyin.",
                        "Hata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                if (currentDatasetId == 0)
                {
                    var result = MessageBox.Show(
                        "Veri seti henüz kaydedilmemiş. Rapor oluşturmak için veri setini kaydetmek ister misiniz?",
                        "Veri Seti Kaydı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            string tempPath = Path.Combine(
                                Application.StartupPath,
                                "Datasets",
                                userId.ToString(),
                                $"{currentDatasetName}.csv"
                            );

                            // Dizin yoksa oluştur
                            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

                            // Veri setini kaydet
                            if (!DatabaseHelper.SaveDataset(userId, currentDatasetName, tempPath))
                            {
                                MessageBox.Show("Veri seti kaydedilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            // Dataset ID'yi yeniden al
                            currentDatasetId = GetCurrentDatasetId();
                            if (currentDatasetId == 0)
                            {
                                MessageBox.Show("Veri seti ID'si alınamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Veri seti kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // rapor.cs formunu doğru parametrelerle oluştur
                rapor raporForm = new rapor(dataTable, userId, currentDatasetId)
                {
                    Text = "Veri Analiz Raporu",
                    Size = new Size(1200, 800),
                    StartPosition = FormStartPosition.CenterScreen,
                    WindowState = FormWindowState.Maximized
                };

                // Sol panel - İçindekiler
                Panel solPanel = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 300,
                    BackColor = Color.FromArgb(24, 37, 80),
                    Padding = new Padding(10) // Panel içindeki tüm elemanlar için padding
                };


                // İçindekiler başlığı
                Label icindekilerBaslik = new Label
                {
                    Text = "İÇİNDEKİLER",
                    Dock = DockStyle.Top,
                    Height = 60,
                    Font = new Font("Times New Roman", 20, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(41, 57, 109),
                    ForeColor = Color.White,
                    Margin = new Padding(0, 0, 0, 20), // Alt tarafta ekstra boşluk
                    BorderStyle = BorderStyle.FixedSingle // Çerçeve efekti
                };
                solPanel.Controls.Add(icindekilerBaslik);

                // İçindekiler listesi
                ListView icindekilerListesi = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    BackColor = Color.FromArgb(33, 50, 95), // Daha koyu arka plan
                    ForeColor = Color.White
                };
                icindekilerListesi.Columns.Add("Bölüm", 280);

                // Liste elemanları
                string[] bolumler = {
                "1. Özet Bilgiler",
                "2. Veri Seti Yapısı",
                "1. Özet Bilgiler",
                "2. Veri Seti Yapısı",
                "3. Veri Kalitesi Analizi",
                "4. İstatistiksel Analiz",
                "5. Korelasyon Analizi"
            };

                foreach (string bolum in bolumler)
                {
                    ListViewItem item = new ListViewItem(bolum);
                    icindekilerListesi.Items.Add(item);
                }

                solPanel.Controls.Add(icindekilerListesi); // Listeyi panele ekle
                raporForm.Controls.Add(solPanel); // Sol paneli ana forma ekle

                // Ana içerik paneli
                Panel icerikPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.White,
                    Padding = new Padding(60, 40, 60, 40)
                };

                // Rapor içeriği için RichTextBox
                RichTextBox raporIcerik = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = Color.White,
                    Font = new Font("Times New Roman", 18),
                    BorderStyle = BorderStyle.None
                };

                icerikPanel.Controls.Add(raporIcerik);

                // Rapor içeriğini oluştur
                OlusturKapsamliRaporIcerigi(raporIcerik);

                // İçindekiler listesi tıklama olayı
                icindekilerListesi.ItemSelectionChanged += (s, ev) =>
                {
                    if (icindekilerListesi.SelectedItems.Count > 0)
                    {
                        string secilenBolum = icindekilerListesi.SelectedItems[0].Text.Split('.')[0];
                        int startIndex = raporIcerik.Find($"{secilenBolum}. ", RichTextBoxFinds.MatchCase);
                        if (startIndex != -1)
                        {
                            raporIcerik.Select(startIndex + 10, 30);
                            raporIcerik.ScrollToCaret();
                        }
                    }
                };

                // Ayırıcı çizgi
                Panel ayiriciPanel = new Panel
                {
                    Width = 1,
                    Dock = DockStyle.Left,
                    BackColor = Color.FromArgb(224, 224, 224)
                };

                // Kontrolleri forma ekle
                raporForm.Controls.AddRange(new Control[] { icerikPanel, ayiriciPanel, solPanel });

                // Raporu göster
                raporForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapor oluşturulurken bir hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OlusturKapsamliRaporIcerigi(RichTextBox rtb)
        {
            // Başlık
            FormatlaRaporBaslik(rtb, "VERİ ANALİZ RAPORU");
            FormatlaIcerik(rtb, $"Oluşturulma Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}\n\n");

            // 1. Özet Bilgiler
            FormatlaAltBaslik(rtb, "1. ÖZET BİLGİLER");
            var ozet = new Dictionary<string, string>
            {
                {"Toplam Kayıt Sayısı", dataTable.Rows.Count.ToString("N0")},
                {"Toplam Sütun Sayısı", dataTable.Columns.Count.ToString("N0")},
                {"Sayısal Sütun Sayısı", GetNumericalColumns().Count.ToString("N0")},
                {"Kategorik Sütun Sayısı", (dataTable.Columns.Count - GetNumericalColumns().Count).ToString("N0")}
            };
            TabloyuEkle(rtb, ozet);

            // 2. Veri Seti Yapısı
            FormatlaAltBaslik(rtb, "2. VERİ SETİ YAPISI");
            VeriSetiYapisiniTablola(rtb);

            // 3. Veri Kalitesi Analizi
            FormatlaAltBaslik(rtb, "3. VERİ KALİTESİ ANALİZİ");
            int toplamHucre = dataTable.Rows.Count * dataTable.Columns.Count;
            int toplamNull = 0;
            var sutunNullSayilari = new Dictionary<string, int>();

            foreach (DataColumn column in dataTable.Columns)
            {
                int nullSayisi = dataTable.AsEnumerable()
                    .Count(r => r[column] == DBNull.Value || string.IsNullOrWhiteSpace(r[column].ToString()));
                sutunNullSayilari[column.ColumnName] = nullSayisi;
                toplamNull += nullSayisi;
            }

            FormatlaIcerik(rtb, $"Toplam Veri Noktası: {toplamHucre:N0}");
            FormatlaIcerik(rtb, $"Toplam Eksik Veri: {toplamNull:N0}");
            FormatlaIcerik(rtb, $"Veri Tamlık Oranı: {((double)(toplamHucre - toplamNull) / toplamHucre * 100):F2}%\n");

            FormatlaIcerik(rtb, "Sütun Bazında Eksik Veri Analizi:");
            foreach (var kvp in sutunNullSayilari.OrderByDescending(x => x.Value))
            {
                double eksikOran = (double)kvp.Value / dataTable.Rows.Count * 100;
                FormatlaIcerik(rtb, $"   • {kvp.Key}: {kvp.Value:N0} eksik ({eksikOran:F2}%)");
            }
            rtb.AppendText("\n");

            // 4. İstatistiksel Analiz
            FormatlaAltBaslik(rtb, "4. İSTATİSTİKSEL ANALİZ");
            List<string> sayisalSutunlar = GetNumericalColumns();

            foreach (string sutun in sayisalSutunlar)
            {
                var degerler = dataTable.AsEnumerable()
                    .Where(r => r[sutun] != DBNull.Value)
                    .Select(r => Convert.ToDouble(r[sutun]))
                    .ToList();

                if (degerler.Any())
                {
                    IstatistikselAnaliziTablola(rtb, sutun, degerler);
                }
            }

            // 5. Korelasyon Analizi
            if (sayisalSutunlar.Count >= 2)
            {
                FormatlaAltBaslik(rtb, "5. KORELASYON ANALİZİ");
                KorelasyonAnaliziTablola(rtb, sayisalSutunlar);
            }
        }

        private void TabloyuEkle(RichTextBox rtb, Dictionary<string, string> veriler)
        {
            int maxKeyLength = veriler.Keys.Max(k => k.Length);
            foreach (var veri in veriler)
            {
                string satir = $"{veri.Key.PadRight(maxKeyLength + 5)}: {veri.Value}";
                FormatlaIcerik(rtb, satir);
            }
            rtb.AppendText("\n");
        }

        private string KorelasyonDuzeyiAciklama(double korelasyon)
        {
            double mutlakKorelasyon = Math.Abs(korelasyon);
            if (mutlakKorelasyon >= 0.9) return "Çok Yüksek Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.7) return "Yüksek Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.5) return "Orta Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.3) return "Zayıf Düzeyde İlişki";
            return "Çok Zayıf Düzeyde İlişki";
        }

        private double HesaplaKorelasyon(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count == 0)
                return 0;

            double xOrt = x.Average();
            double yOrt = y.Average();

            double pay = x.Zip(y, (xi, yi) => (xi - xOrt) * (yi - yOrt)).Sum();
            double paydaX = x.Sum(xi => Math.Pow(xi - xOrt, 2));
            double paydaY = y.Sum(yi => Math.Pow(yi - yOrt, 2));
            double payda = Math.Sqrt(paydaX * paydaY);

            return payda == 0 ? 0 : pay / payda;
        }

        // Formatlama metodlarını sınıf içinde tanımlayalım
        private void FormatlaRaporBaslik(RichTextBox rtb, string text)
        {
            rtb.SelectionAlignment = HorizontalAlignment.Center;
            rtb.SelectionFont = new Font("Times New Roman", 28, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(28, 41, 86);
            rtb.AppendText("\n\n"+text + "\n\n");
            rtb.SelectionAlignment = HorizontalAlignment.Left;
        }

        private void FormatlaAltBaslik(RichTextBox rtb, string text)
        {
            rtb.SelectionFont = new Font("Times New Roman", 24, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(41, 57, 109);
            rtb.AppendText(text + "\n\n");
        }

        private void FormatlaIcerik(RichTextBox rtb, string text, bool addNewLine = true)
        {
            rtb.SelectionFont = new Font("Times New Roman", 18);
            rtb.SelectionColor = Color.Black;
            rtb.AppendText(text);
            if (addNewLine) rtb.AppendText("\n");
        }

        private void TabloyuEkleGelistirmis(RichTextBox rtb, string[] basliklar, List<string[]> satirlar)
        {
            // Sütun genişliklerini hesapla
            int[] genislikler = new int[basliklar.Length];
            for (int i = 0; i < basliklar.Length; i++)
            {
                genislikler[i] = basliklar[i].Length;
                foreach (var satir in satirlar)
                {
                    if (satir[i].Length > genislikler[i])
                        genislikler[i] = satir[i].Length;
                }
            }

            // Tablo başlıkları
            rtb.SelectionFont = new Font("Times New Roman", 18, FontStyle.Bold);
            rtb.SelectionColor = Color.FromArgb(41, 57, 109);
            rtb.AppendText(string.Join(" | ", basliklar) + "\n");
            rtb.AppendText(new string('-', basliklar.Sum(b => b.Length) + (basliklar.Length - 1) * 3) + "\n");

            // Satırlar
            rtb.SelectionFont = new Font("Times New Roman", 18);
            rtb.SelectionColor = Color.Black;
            foreach (var satir in satirlar)
            {
                rtb.AppendText(string.Join(" | ", satir) + "\n");
            }
            rtb.AppendText("\n");
        }

        private void VeriSetiYapisiniTablola(RichTextBox rtb)
        {
            string[] basliklar = { "Sütun Adı", "Veri Tipi", "Benzersiz Değer", "Eksik Veri" };
            var satirlar = new List<string[]>();

            foreach (DataColumn column in dataTable.Columns)
            {
                Type veriTipi = TahminEdilenVeriTipi(dataTable, column);
                int benzersizDegerSayisi = dataTable.AsEnumerable()
                    .Select(r => r[column.ColumnName])
                    .Distinct()
                    .Count();
                int nullDegerSayisi = dataTable.AsEnumerable()
                    .Count(r => r[column.ColumnName] == DBNull.Value ||
                  string.IsNullOrWhiteSpace(r[column.ColumnName].ToString()));

                satirlar.Add(new string[] {
            column.ColumnName,
            veriTipi.Name,
            benzersizDegerSayisi.ToString("N0"),
            nullDegerSayisi.ToString("N0")
        });
            }

            TabloyuEkleGelistirmis(rtb, basliklar, satirlar);
        }

        private void IstatistikselAnaliziTablola(RichTextBox rtb, string sutun, List<double> degerler)
        {
            string[] basliklar = { "Metrik", "Değer" };
            var satirlar = new List<string[]>();

            double ortalama = degerler.Average();
            var siraliDegerler = degerler.OrderBy(v => v).ToList();
            double medyan = siraliDegerler.Count % 2 == 0
                ? (siraliDegerler[siraliDegerler.Count / 2 - 1] + siraliDegerler[siraliDegerler.Count / 2]) / 2
                : siraliDegerler[siraliDegerler.Count / 2];

            var mod = degerler.GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .First();

            double standartSapma = Math.Sqrt(degerler.Average(v => Math.Pow(v - ortalama, 2)));
            double varyans = Math.Pow(standartSapma, 2);

            int n = siraliDegerler.Count;
            double q1 = siraliDegerler[(int)(n * 0.25)];
            double q3 = siraliDegerler[(int)(n * 0.75)];
            double iqr = q3 - q1;

            satirlar.AddRange(new[] {
        new[] { "Ortalama", $"{ortalama:F2}" },
        new[] { "Medyan", $"{medyan:F2}" },
        new[] { "Mod", $"{mod.Key:F2} (Frekans: {mod.Count():N0})" },
        new[] { "Standart Sapma", $"{standartSapma:F2}" },
        new[] { "Varyans", $"{varyans:F2}" },
        new[] { "Minimum", $"{degerler.Min():F2}" },
        new[] { "Maksimum", $"{degerler.Max():F2}" },
        new[] { "Q1 (1. Çeyrek)", $"{q1:F2}" },
        new[] { "Q3 (3. Çeyrek)", $"{q3:F2}" },
        new[] { "IQR", $"{iqr:F2}" }
    });

            FormatlaIcerik(rtb, $"\n► {sutun} İstatistikleri:", true);
            TabloyuEkleGelistirmis(rtb, basliklar, satirlar);
        }

        private void KorelasyonAnaliziTablola(RichTextBox rtb, List<string> sayisalSutunlar)
        {
            if (sayisalSutunlar.Count < 2) return;

            string[] basliklar = { "Sütun 1", "Sütun 2", "Korelasyon", "İlişki Düzeyi" };
            var satirlar = new List<string[]>();

            foreach (string sutun1 in sayisalSutunlar)
            {
                foreach (string sutun2 in sayisalSutunlar)
                {
                    if (sutun1.CompareTo(sutun2) < 0)
                    {
                        var degerler1 = dataTable.AsEnumerable()
                            .Where(r => r[sutun1] != DBNull.Value && r[sutun2] != DBNull.Value)
                            .Select(r => Convert.ToDouble(r[sutun1]))
                            .ToList();
                        var degerler2 = dataTable.AsEnumerable()
                            .Where(r => r[sutun1] != DBNull.Value && r[sutun2] != DBNull.Value)
                            .Select(r => Convert.ToDouble(r[sutun2]))
                            .ToList();

                        if (degerler1.Any())
                        {
                            double korelasyon = HesaplaKorelasyon(degerler1, degerler2);
                            satirlar.Add(new string[] {
                        sutun1,
                        sutun2,
                        $"{korelasyon:F3}",
                        KorelasyonDuzeyiAciklama(korelasyon)
                    });
                        }
                    }
                }
            }

            TabloyuEkleGelistirmis(rtb, basliklar, satirlar);
        }

    }
}
