using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Font = iTextSharp.text.Font; // Resolve ambiguity

namespace VeriAnalizOtomasyonu
{
    public partial class rapor : DevExpress.XtraEditors.XtraForm
    {
        private DataTable dataTable;
        private readonly int userId;
        private readonly int datasetId;
        private string reportPath;

        public rapor(DataTable dataTable, int userId, int datasetId)
        {
            InitializeComponent();
            this.dataTable = dataTable;
            this.userId = userId;
            this.datasetId = datasetId;
            InitializeRaporForm();
        }

        private void InitializeRaporForm()
        {
            this.Text = "Veri Analiz Raporu";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Üst panel - Araç çubuğu
            Panel ustPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(41, 57, 109),
                Padding = new Padding(10)
            };

            // Kaydedilmiş Raporlar butonu
            Button kaydedilmisRaporlarBtn = new Button
            {
                Text = "Kaydedilmiş Raporlar",
                Dock = DockStyle.Right,
                Width = 180,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };
            kaydedilmisRaporlarBtn.Click += KaydedilmisRaporlarBtn_Click;
            ustPanel.Controls.Add(kaydedilmisRaporlarBtn);

            // PDF Kaydet butonu
            Button pdfKaydetBtn = new Button
            {
                Text = "PDF Olarak Kaydet",
                Dock = DockStyle.Right,
                Width = 180,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };
            pdfKaydetBtn.Click += PdfKaydetBtn_Click;
            ustPanel.Controls.Add(pdfKaydetBtn);

            this.Controls.Add(ustPanel);
        }

        private void PdfKaydetBtn_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PDF Dosyası (*.pdf)|*.pdf";
                saveFileDialog.Title = "Raporu Kaydet";
                saveFileDialog.FileName = $"Veri_Analiz_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        SaveReportToPdf(filePath);
                        SaveReportToDatabase(filePath);

                        MessageBox.Show("Rapor başarıyla kaydedildi ve veritabanına eklendi.",
                            "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Rapor kaydedilirken hata oluştu: {ex.Message}",
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveReportToDatabase(string pdfPath)
        {
            try
            {
                // Raporun kopyasını uygulama dizininde sakla
                string reportsDirectory = Path.Combine(Application.StartupPath, "Reports", userId.ToString());
                Directory.CreateDirectory(reportsDirectory);

                string fileName = Path.GetFileName(pdfPath);
                string destinationPath = Path.Combine(reportsDirectory, fileName);
                File.Copy(pdfPath, destinationPath, true);

                // Veritabanına kaydet
                int reportId = DatabaseHelper.SaveReport(userId, datasetId, destinationPath);
                if (reportId == 0)
                {
                    throw new Exception("Rapor veritabanına kaydedilemedi.");
                }
                this.reportPath = destinationPath;
            }
            catch (Exception ex)
            {
                throw new Exception("Rapor veritabanına kaydedilirken hata oluştu: " + ex.Message);
            }
        }

        private void SaveReportToPdf(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                Document pdfDoc = new Document();
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // Yazı tipi ayarları (Türkçe karakter desteği için)
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var normalFont = new Font(bf, 12);
                var titleFont = new Font(bf, 20, iTextSharp.text.Font.BOLD);
                var headerFont = new Font(bf, 16, iTextSharp.text.Font.BOLD);

                // Rapor içeriğini PDF'e ekleyelim
                pdfDoc.Add(new Paragraph("VERİ ANALİZ RAPORU", titleFont));
                pdfDoc.Add(new Paragraph($"Oluşturulma Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}", normalFont));
                pdfDoc.Add(new Paragraph("\n"));

                // 1. Özet Bilgiler
                pdfDoc.Add(new Paragraph("1. ÖZET BİLGİLER", headerFont));
                pdfDoc.Add(new Paragraph($"Toplam Kayıt Sayısı        : {dataTable.Rows.Count}", normalFont));
                pdfDoc.Add(new Paragraph($"Toplam Sütun Sayısı        : {dataTable.Columns.Count}", normalFont));
                pdfDoc.Add(new Paragraph($"Sayısal Sütun Sayısı       : {GetNumericalColumns().Count}", normalFont));
                pdfDoc.Add(new Paragraph($"Kategorik Sütun Sayısı     : {dataTable.Columns.Count - GetNumericalColumns().Count}", normalFont));
                pdfDoc.Add(new Paragraph("\n"));

                // 2. Veri Seti Yapısı
                AddDatasetStructure(pdfDoc, normalFont, headerFont);

                // 3. Veri Kalitesi Analizi
                AddDataQualityAnalysis(pdfDoc, normalFont, headerFont);

                // 4. İstatistiksel Analiz
                AddStatisticalAnalysis(pdfDoc, normalFont, headerFont);

                // 5. Korelasyon Analizi
                AddCorrelationAnalysis(pdfDoc, normalFont, headerFont);

                pdfDoc.Close();
            }
        }

        private void AddDatasetStructure(Document pdfDoc, Font normalFont, Font headerFont)
        {
            pdfDoc.Add(new Paragraph("2. VERİ SETİ YAPISI", headerFont));
            pdfDoc.Add(new Paragraph("Sütun Adı | Veri Tipi | Benzersiz Değer | Eksik Veri", normalFont));
            pdfDoc.Add(new Paragraph(new string('-', 80), normalFont));

            foreach (DataColumn column in dataTable.Columns)
            {
                int benzersizDeger = dataTable.AsEnumerable().Select(r => r[column]).Distinct().Count();
                int eksikVeri = dataTable.AsEnumerable().Count(r => r[column] == DBNull.Value);
                pdfDoc.Add(new Paragraph($"{column.ColumnName} | {column.DataType.Name} | {benzersizDeger} | {eksikVeri}", normalFont));
            }
            pdfDoc.Add(new Paragraph("\n"));
        }

        private void AddDataQualityAnalysis(Document pdfDoc, Font normalFont, Font headerFont)
        {
            pdfDoc.Add(new Paragraph("3. VERİ KALİTESİ ANALİZİ", headerFont));

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

            pdfDoc.Add(new Paragraph($"Toplam Veri Noktası: {toplamHucre:N0}", normalFont));
            pdfDoc.Add(new Paragraph($"Toplam Eksik Veri: {toplamNull:N0}", normalFont));
            pdfDoc.Add(new Paragraph($"Veri Tamlık Oranı: {((double)(toplamHucre - toplamNull) / toplamHucre * 100):F2}%\n", normalFont));

            pdfDoc.Add(new Paragraph("Sütun Bazında Eksik Veri Analizi:", normalFont));
            foreach (var kvp in sutunNullSayilari.OrderByDescending(x => x.Value))
            {
                double eksikOran = (double)kvp.Value / dataTable.Rows.Count * 100;
                pdfDoc.Add(new Paragraph($"   • {kvp.Key}: {kvp.Value:N0} eksik ({eksikOran:F2}%)", normalFont));
            }
            pdfDoc.Add(new Paragraph("\n"));
        }

        private void AddStatisticalAnalysis(Document pdfDoc, Font normalFont, Font headerFont)
        {
            pdfDoc.Add(new Paragraph("4. İSTATİKSEL ANALİZ", headerFont));
            List<string> sayisalSutunlar = GetNumericalColumns();

            foreach (string sutun in sayisalSutunlar)
            {
                var degerler = dataTable.AsEnumerable()
                    .Where(r => r[sutun] != DBNull.Value)
                    .Select(r => Convert.ToDouble(r[sutun]))
                    .ToList();

                if (degerler.Any())
                {
                    pdfDoc.Add(new Paragraph($"\n► {sutun} İstatistikleri:", normalFont));

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

                    pdfDoc.Add(new Paragraph($"Ortalama: {ortalama:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Medyan: {medyan:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Mod: {mod.Key:F2} (Frekans: {mod.Count():N0})", normalFont));
                    pdfDoc.Add(new Paragraph($"Standart Sapma: {standartSapma:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Varyans: {varyans:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Minimum: {degerler.Min():F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Maksimum: {degerler.Max():F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Q1 (1. Çeyrek): {q1:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"Q3 (3. Çeyrek): {q3:F2}", normalFont));
                    pdfDoc.Add(new Paragraph($"IQR: {iqr:F2}", normalFont));
                }
            }
        }

        private void AddCorrelationAnalysis(Document pdfDoc, Font normalFont, Font headerFont)
        {
            List<string> sayisalSutunlar = GetNumericalColumns();
            if (sayisalSutunlar.Count < 2) return;

            pdfDoc.Add(new Paragraph("5. KORELASYON ANALİZİ", headerFont));

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
                            pdfDoc.Add(new Paragraph($"{sutun1} - {sutun2}: {korelasyon:F3} ({KorelasyonDuzeyiAciklama(korelasyon)})", normalFont));
                        }
                    }
                }
            }
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

        private string KorelasyonDuzeyiAciklama(double korelasyon)
        {
            double mutlakKorelasyon = Math.Abs(korelasyon);
            if (mutlakKorelasyon >= 0.9) return "Çok Yüksek Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.7) return "Yüksek Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.5) return "Orta Düzeyde İlişki";
            if (mutlakKorelasyon >= 0.3) return "Zayıf Düzeyde İlişki";
            return "Çok Zayıf Düzeyde İlişki";
        }

        private List<string> GetNumericalColumns()
        {
            List<string> numericalColumns = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                Type columnType = TahminEdilenVeriTipi(dataTable, column);

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

        public Type TahminEdilenVeriTipi(DataTable dataTable, DataColumn column)
        {
            bool tümüInteger = true;
            bool tümüDecimal = true;
            bool tümüDateTime = true;

            var culture = System.Globalization.CultureInfo.InvariantCulture;

            foreach (DataRow row in dataTable.Rows)
            {
                object değer = row[column];
                if (değer == DBNull.Value) continue;

                string değerMetni = değer.ToString();

                if (!int.TryParse(değerMetni, System.Globalization.NumberStyles.Integer, culture, out _))
                    tümüInteger = false;
                if (!decimal.TryParse(değerMetni, System.Globalization.NumberStyles.Number, culture, out _))
                    tümüDecimal = false;
                if (!DateTime.TryParse(değerMetni, culture, System.Globalization.DateTimeStyles.None, out _))
                    tümüDateTime = false;

                if (!tümüInteger && !tümüDecimal && !tümüDateTime)
                    return typeof(string);
            }

            if (tümüInteger) return typeof(int);
            if (tümüDecimal) return typeof(decimal);
            if (tümüDateTime) return typeof(DateTime);

            return typeof(string);
        }

        private List<KorelasyonSonucu> GetKorelasyonAnalizi()
        {
            var korelasyonSonuclari = new List<KorelasyonSonucu>();
            var sayisalSutunlar = GetNumericalColumns();

            foreach (var sutun1 in sayisalSutunlar)
            {
                foreach (var sutun2 in sayisalSutunlar)
                {
                    if (sutun1 != sutun2)
                    {
                        var degerler1 = dataTable.AsEnumerable()
                            .Where(r => r[sutun1] != DBNull.Value && r[sutun2] != DBNull.Value)
                            .Select(r => Convert.ToDouble(r[sutun1]))
                            .ToList();
                        var degerler2 = dataTable.AsEnumerable()
                            .Where(r => r[sutun1] != DBNull.Value && r[sutun2] != DBNull.Value)
                            .Select(r => Convert.ToDouble(r[sutun2]))
                            .ToList();

                        if (degerler1.Count > 0 && degerler2.Count > 0)
                        {
                            double korelasyon = HesaplaKorelasyon(degerler1, degerler2);
                            string iliskiDuzeyi = KorelasyonDuzeyiAciklama(korelasyon);
                            korelasyonSonuclari.Add(new KorelasyonSonucu
                            {
                                Sutun1 = sutun1,
                                Sutun2 = sutun2,
                                Korelasyon = korelasyon,
                                IliskiDuzeyi = iliskiDuzeyi
                            });
                        }
                    }
                }
            }

            return korelasyonSonuclari;
        }

        private class KorelasyonSonucu
        {
            public string Sutun1 { get; set; }
            public string Sutun2 { get; set; }
            public double Korelasyon { get; set; }
            public string IliskiDuzeyi { get; set; }
        }

        private void KaydedilmisRaporlarBtn_Click(object sender, EventArgs e)
        {
            var reports = DatabaseHelper.GetUserReports(userId);
            if (reports.Rows.Count == 0)
            {
                MessageBox.Show("Henüz kaydedilmiş rapor bulunmamaktadır.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Kaydedilmiş Raporlar";
                form.Size = new Size(800, 400);
                form.StartPosition = FormStartPosition.CenterParent;

                var listView = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };

                listView.Columns.Add("Rapor ID", 100);
                listView.Columns.Add("Veri Seti", 200);
                listView.Columns.Add("Oluşturma Tarihi", 200);
                listView.Columns.Add("Dosya Yolu", 300);

                foreach (DataRow row in reports.Rows)
                {
                    var item = new ListViewItem(row["ReportID"].ToString());
                    item.SubItems.Add(row["DatasetName"].ToString());
                    item.SubItems.Add(Convert.ToDateTime(row["CreatedDate"]).ToString("dd.MM.yyyy HH:mm"));
                    item.SubItems.Add(row["OriginalFilePath"].ToString());
                    listView.Items.Add(item);
                }

                var openButton = new Button
                {
                    Text = "Aç",
                    Dock = DockStyle.Bottom
                };

                openButton.Click += (s, ev) =>
                {
                    if (listView.SelectedItems.Count > 0)
                    {
                        string path = listView.SelectedItems[0].SubItems[3].Text;
                        if (File.Exists(path))
                        {
                            System.Diagnostics.Process.Start(path);
                        }
                        else
                        {
                            MessageBox.Show("Rapor dosyası bulunamadı.",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };

                var deleteButton = new Button
                {
                    Text = "Sil",
                    Dock = DockStyle.Bottom
                };

                deleteButton.Click += (s, ev) =>
                {
                    if (listView.SelectedItems.Count > 0)
                    {
                        var result = MessageBox.Show(
                            "Seçili raporu silmek istediğinizden emin misiniz?",
                            "Onay",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            int reportId = int.Parse(listView.SelectedItems[0].Text);
                            string path = listView.SelectedItems[0].SubItems[3].Text;

                            if (DatabaseHelper.DeleteReport(reportId))
                            {
                                try
                                {
                                    if (File.Exists(path))
                                    {
                                        File.Delete(path);
                                    }
                                    listView.SelectedItems[0].Remove();
                                    MessageBox.Show("Rapor başarıyla silindi.",
                                        "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Rapor dosyası silinirken hata oluştu: {ex.Message}",
                                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                };

                form.Controls.Add(listView);
                form.Controls.Add(deleteButton);
                form.Controls.Add(openButton);
                form.ShowDialog();
            }
        }
    }
}
