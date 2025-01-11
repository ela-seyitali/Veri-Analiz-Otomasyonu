using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using DevExpress.XtraEditors;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using DevExpress.XtraGrid;


namespace VeriAnalizOtomasyonu
{
    internal class Verisetiİşlemleri
    {
        mainForm mf;

        public Verisetiİşlemleri(mainForm mainForm)
        {
            mf = new mainForm();
        }

        public Verisetiİşlemleri()
        {
        }

        public void ÇoklukTablosuOlustur(PanelControl panel, DataTable dataTable)
        {
            try
            {
                panel.Controls.Clear();

                // Yeni bir DataGridView oluştur
                DataGridView dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    ReadOnly = true
                };

                // Çokluk tablosu için DataTable oluştur
                DataTable frequencyTable = new DataTable();
                frequencyTable.Columns.Add("Sütun Adı");
                frequencyTable.Columns.Add("Değer");
                frequencyTable.Columns.Add("Frekans");

                // Her sütun için frekansları hesapla
                foreach (DataColumn column in dataTable.Columns)
                {
                    var frequencyDict = new Dictionary<string, int>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string value = row[column].ToString();
                        if (frequencyDict.ContainsKey(value))
                        {
                            frequencyDict[value]++;
                        }
                        else
                        {
                            frequencyDict[value] = 1;
                        }
                    }

                    // Frekansları tabloya ekle
                    foreach (var kvp in frequencyDict)
                    {
                        frequencyTable.Rows.Add(column.ColumnName, kvp.Key, kvp.Value);
                    }
                }

                // DataGridView'e frekans tablosunu ata
                dataGridView.DataSource = frequencyTable;
                panel.Controls.Add(dataGridView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Çokluk tablosu oluşturulurken hata: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void KorelasyonAnaliziOlustur(PanelControl panel, DataTable dataTable, List<string> selectedColumns)
        {
            try
            {
                panel.Controls.Clear();

                if (selectedColumns.Count < 2)
                {
                    MessageBox.Show("Korelasyon analizi için en az 2 sütun seçmelisiniz.",
                                  "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Korelasyon matrisi oluştur
                var correlationMatrix = new double[selectedColumns.Count, selectedColumns.Count];
                var validData = true;

                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    for (int j = 0; j < selectedColumns.Count; j++)
                    {
                        try
                        {
                            var values1 = GetColumnValues(dataTable, selectedColumns[i]);
                            var values2 = GetColumnValues(dataTable, selectedColumns[j]);

                            if (values1.Length == 0 || values2.Length == 0)
                            {
                                validData = false;
                                break;
                            }

                            correlationMatrix[i, j] = CalculateCorrelation(values1, values2);
                        }
                        catch
                        {
                            validData = false;
                            break;
                        }
                    }
                    if (!validData) break;
                }

                if (!validData)
                {
                    MessageBox.Show("Seçili sütunlarda geçerli sayısal veri bulunamadı.",
                                  "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Grafik oluştur
                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    DisableAnimations = true,
                    LegendLocation = LegendLocation.None,
                    BackColor = System.Drawing.Color.White,
                    Padding = new Padding(25)
                };

                // HeatMap için gerekli düzenlemeler
                var heatSeries = new HeatSeries
                {
                    Values = new ChartValues<HeatPoint>(),
                    DataLabels = true,
                    LabelPoint = point => $"{point.Weight:N2}"
                };

                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    for (int j = 0; j < selectedColumns.Count; j++)
                    {
                        heatSeries.Values.Add(new HeatPoint(i, j, correlationMatrix[i, j]));
                    }
                }

                chart.Series.Add(heatSeries);

                // X ve Y eksenlerini yapılandırma
                chart.AxisX.Add(new Axis
                {
                    Labels = selectedColumns,
                    LabelsRotation = 45,
                    Separator = new Separator
                    {
                        Step = 1,
                        StrokeThickness = 0.5,
                        StrokeDashArray = new DoubleCollection { 2 }
                    },
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Black
                });

                chart.AxisY.Add(new Axis
                {
                    Labels = selectedColumns,
                    Separator = new Separator
                    {
                        Step = 1,
                        StrokeThickness = 0.5,
                        StrokeDashArray = new DoubleCollection { 2 }
                    },
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Black
                });

                panel.Controls.Add(chart);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Korelasyon analizi oluşturulurken hata oluştu: {ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private double[] GetColumnValues(DataTable dataTable, string columnName)
        {
            try
            {
                return dataTable.AsEnumerable()
                    .Where(row => row[columnName] != DBNull.Value &&
                                 double.TryParse(row[columnName].ToString(), out _))
                    .Select(row => Convert.ToDouble(row[columnName]))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<double>();
            }
        }

        private double CalculateCorrelation(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length < 2)
                return 0;

            try
            {
                double meanX = x.Average();
                double meanY = y.Average();

                double sumXY = 0;
                double sumX2 = 0;
                double sumY2 = 0;

                for (int i = 0; i < x.Length; i++)
                {
                    double dX = x[i] - meanX;
                    double dY = y[i] - meanY;
                    sumXY += dX * dY;
                    sumX2 += dX * dX;
                    sumY2 += dY * dY;
                }

                if (Math.Abs(sumX2) < double.Epsilon || Math.Abs(sumY2) < double.Epsilon)
                    return 0;

                var correlation = sumXY / Math.Sqrt(sumX2 * sumY2);
                return Math.Round(correlation, 4);
            }
            catch
            {
                return 0;
            }
        }
        public void DeğerDağılımıGörüntüle(PanelControl panel, DataTable dataTable)
        {
            try
            {
                panel.Controls.Clear();

                // Yeni bir DataGridView oluştur
                DataGridView dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    ReadOnly = true
                };

                // Değer dağılımı tablosu için DataTable oluştur
                DataTable distributionTable = new DataTable();
                distributionTable.Columns.Add("Sütun Adı");
                distributionTable.Columns.Add("Değer");
                distributionTable.Columns.Add("Yüzde");

                // Her sütun için değer dağılımını hesapla
                foreach (DataColumn column in dataTable.Columns)
                {
                    var valueCountDict = new Dictionary<string, int>();
                    int totalCount = dataTable.Rows.Count;

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string value = row[column].ToString();
                        if (valueCountDict.ContainsKey(value))
                        {
                            valueCountDict[value]++;
                        }
                        else
                        {
                            valueCountDict[value] = 1;
                        }
                    }

                    // Değer dağılımını tabloya ekle
                    foreach (var kvp in valueCountDict)
                    {
                        double percentage = (double)kvp.Value / totalCount * 100;
                        distributionTable.Rows.Add(column.ColumnName, kvp.Key, percentage.ToString("F2") + "%");
                    }
                }

                // DataGridView'e değer dağılımı tablosunu ata
                dataGridView.DataSource = distributionTable;
                panel.Controls.Add(dataGridView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Değer dağılımı görüntülenirken hata: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void BenzersizDeğerSayısıGörüntüle(PanelControl panel, DataTable dataTable)
        {
            try
            {
                // Paneli temizle
                panel.Controls.Clear();

                // Yeni bir DataGridView oluştur ve özelliklerini ayarla
                DataGridView dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill, // Paneli tamamen doldur
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false, // Satır başlıklarını gizle
                    BorderStyle = BorderStyle.None, // Kenarlıkları kaldır
                    BackgroundColor = panel.BackColor // Panel ile aynı arka plan rengini kullan
                };

                // Benzersiz değer sayısı tablosu için DataTable oluştur
                DataTable uniqueCountTable = new DataTable();
                uniqueCountTable.Columns.Add("Sütun Adı");
                uniqueCountTable.Columns.Add("Benzersiz Değer Sayısı");

                // Her sütun için benzersiz değer sayısını hesapla
                foreach (DataColumn column in dataTable.Columns)
                {
                    var uniqueValues = new HashSet<string>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string value = row[column].ToString();
                        uniqueValues.Add(value);
                    }

                    // Benzersiz değer sayısını tabloya ekle
                    uniqueCountTable.Rows.Add(column.ColumnName, uniqueValues.Count);
                }

                // DataGridView'e benzersiz değer sayısı tablosunu ata
                dataGridView.DataSource = uniqueCountTable;

                // Panel'e DataGridView'i ekle
                panel.Controls.Add(dataGridView);

                // Görünümü yenile
                panel.PerformLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Benzersiz değer sayısı görüntülenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void EksikDeğerTablosuGörüntüle(PanelControl panel, DataTable dataTable)
        {
            try
            {
                panel.Controls.Clear();

                // Yeni bir DataGridView oluştur
                DataGridView dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    ReadOnly = true
                };

                // Eksik değer tablosu için DataTable oluştur
                DataTable missingValuesTable = new DataTable();
                missingValuesTable.Columns.Add("Sütun Adı");
                missingValuesTable.Columns.Add("Eksik Değer Sayısı");
                missingValuesTable.Columns.Add("Eksik Değer Oranı (%)");

                // Her sütun için eksik değer sayısını ve oranını hesapla
                foreach (DataColumn column in dataTable.Columns)
                {
                    int missingCount = 0;
                    int totalCount = dataTable.Rows.Count;

                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (row[column] == DBNull.Value || string.IsNullOrWhiteSpace(row[column].ToString()))
                        {
                            missingCount++;
                        }
                    }

                    double missingPercentage = (double)missingCount / totalCount * 100;
                    missingValuesTable.Rows.Add(column.ColumnName, missingCount, missingPercentage.ToString("F2") + "%");
                }

                // DataGridView'e eksik değer tablosunu ata
                dataGridView.DataSource = missingValuesTable;
                panel.Controls.Add(dataGridView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eksik değer tablosu görüntülenirken hata: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void NullOranıGörüntüle(PanelControl panel, DataTable dataTable)
        {
            try
            {
                panel.Controls.Clear();

                // Yeni bir DataGridView oluştur
                DataGridView dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    ReadOnly = true
                };

                // Null oranı tablosu için DataTable oluştur
                DataTable nullPercentageTable = new DataTable();
                nullPercentageTable.Columns.Add("Sütun Adı");
                nullPercentageTable.Columns.Add("Null Oranı (%)");

                // Her sütun için null oranını hesapla
                foreach (DataColumn column in dataTable.Columns)
                {
                    int nullCount = 0;
                    int totalCount = dataTable.Rows.Count;

                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (row[column] == DBNull.Value || string.IsNullOrWhiteSpace(row[column].ToString()))
                        {
                            nullCount++;
                        }
                    }

                    double nullPercentage = (double)nullCount / totalCount * 100;
                    nullPercentageTable.Rows.Add(column.ColumnName, nullPercentage.ToString("F2") + "%");
                }

                // DataGridView'e null oranı tablosunu ata
                dataGridView.DataSource = nullPercentageTable;
                panel.Controls.Add(dataGridView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Null oranı tablosu görüntülenirken hata: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Verisetiİşlemleri.cs
        public void TemaDegistir(Form form, string temaName, System.Drawing.Color backColor, System.Drawing.Color foreColor)
        {
            try
            {
                // Ana form renklerini ayarla
                form.BackColor = backColor;
                form.ForeColor = foreColor;

                // Kontrollere tema uygula
                ApplyThemeToControls(form.Controls, temaName, backColor, foreColor);

                // Kullanıcı tercihini kaydet
                Properties.Settings.Default["LastTheme"] = temaName;
                Properties.Settings.Default.Save();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tema değiştirilirken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ApplyThemeToControls(Control.ControlCollection controls, string themeName, System.Drawing.Color backColor, System.Drawing.Color foreColor)
        {
            foreach (Control control in controls)
            {
                // TextBox kontrolüne tema uygula
                if (control is TextBox textBox)
                {
                    textBox.BackColor = backColor;
                    textBox.ForeColor = foreColor;
                }
                // DataGridView kontrolüne tema uygula
                else if (control is DataGridView dataGridView)
                {
                    dataGridView.BackgroundColor = backColor;
                    dataGridView.DefaultCellStyle.BackColor = backColor;
                    dataGridView.DefaultCellStyle.ForeColor = foreColor;
                    dataGridView.ColumnHeadersDefaultCellStyle.BackColor = CalculateHeaderColor(backColor);
                    dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = foreColor;
                }
                // Panel veya GroupBox kontrollerine tema uygula
                else if (control is Panel || control is GroupBox)
                {
                    control.BackColor = backColor;
                    control.ForeColor = foreColor;
                }
                // Button kontrolüne tema uygula
                else if (control is Button btn)
                {
                    if (themeName.Contains("Koyu") || themeName.Contains("Gece"))
                    {
                        btn.BackColor = CalculateButtonColor(backColor);
                        btn.ForeColor = System.Drawing.Color.White;
                        if (btn.FlatStyle == FlatStyle.Flat)
                            btn.FlatAppearance.BorderColor = CalculateButtonBorderColor(backColor);
                    }
                    else
                    {
                        btn.BackColor = SystemColors.Control;
                        btn.ForeColor = System.Drawing.Color.Black;
                    }
                }

                // Alt kontrollere tema uygula
                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, themeName, backColor, foreColor);
                }
            }
        }

        private System.Drawing.Color CalculateHighlightColor(System.Drawing.Color baseColor)
        {
            return System.Drawing.Color.FromArgb(
                Math.Min(baseColor.R + 20, 255),
                Math.Min(baseColor.G + 20, 255),
                Math.Min(baseColor.B + 20, 255)
            );
        }

        private System.Drawing.Color CalculateHeaderColor(System.Drawing.Color baseColor)
        {
            return System.Drawing.Color.FromArgb(
                Math.Max(baseColor.R - 10, 0),
                Math.Max(baseColor.G - 10, 0),
                Math.Max(baseColor.B - 10, 0)
            );
        }

        private System.Drawing.Color CalculateButtonColor(System.Drawing.Color baseColor)
        {
            return System.Drawing.Color.FromArgb(
                Math.Min(baseColor.R + 30, 255),
                Math.Min(baseColor.G + 30, 255),
                Math.Min(baseColor.B + 30, 255)
            );
        }

        private System.Drawing.Color CalculateButtonBorderColor(System.Drawing.Color baseColor)
        {
            return System.Drawing.Color.FromArgb(
                Math.Min(baseColor.R + 40, 255),
                Math.Min(baseColor.G + 40, 255),
                Math.Min(baseColor.B + 40, 255)
            );
        }


    }
}
