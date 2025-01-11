using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using DevExpress.XtraEditors;
using System.Windows.Media;
using System.Drawing.Drawing2D;
using System.Drawing;


namespace VeriAnalizOtomasyonu
{
    public class Görselleştirme
    {
        private const int MAX_DATA_POINTS = 1000;
        private readonly System.Windows.Media.Color[] CHART_COLORS = new[]
 {
    System.Windows.Media.Color.FromRgb(65, 105, 225),  // Royal Blue
    System.Windows.Media.Color.FromRgb(46, 139, 87),   // Sea Green
    System.Windows.Media.Color.FromRgb(218, 112, 214), // Orchid
    System.Windows.Media.Color.FromRgb(210, 105, 30),  // Chocolate
    System.Windows.Media.Color.FromRgb(106, 90, 205),  // Slate Blue
    System.Windows.Media.Color.FromRgb(188, 143, 143)  // Rosy Brown
};


        private DataTable SampleLargeDataset(DataTable originalData, int maxPoints)
        {
            if (originalData.Rows.Count <= maxPoints)
                return originalData;

            DataTable sampledData = originalData.Clone();
            int step = originalData.Rows.Count / maxPoints;

            for (int i = 0; i < originalData.Rows.Count; i += step)
            {
                sampledData.ImportRow(originalData.Rows[i]);
            }

            return sampledData;
        }
        // Modern Çizgi Grafiği
        public void ModernÇizgiGrafiğiOlustur(PanelControl panel, DataTable dataTable,
     string categoryColumn, string valueColumn, bool smoothLine = true)
        {
            try
            {
                panel.Controls.Clear();
                panel.Size = new Size(800, 400);
                var sampledData = SampleLargeDataset(dataTable, MAX_DATA_POINTS);

                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LegendLocation.Right,
                    DisableAnimations = sampledData.Rows.Count > 500,
                    Background = new SolidColorBrush(Colors.White)
                };

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (DataRow row in sampledData.Rows)
                {
                    values.Add(Convert.ToDouble(row[valueColumn]));
                    labels.Add(row[categoryColumn].ToString());
                }

                chart.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = valueColumn,
                Values = values,
                LineSmoothness = smoothLine ? 0.5 : 0,
                PointGeometrySize = sampledData.Rows.Count > 100 ? 0 : 8,
                Stroke = new System.Windows.Media.SolidColorBrush (System.Windows.Media.Color.FromRgb(24, 144, 255)),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 144, 255)),
            }
        };

                chart.AxisX.Add(new Axis
                {
                    Title = categoryColumn,
                    Labels = labels,
                    Separator = new Separator { Step = Math.Max(1, labels.Count / 15) }
                });

                chart.AxisY.Add(new Axis
                {
                    Title = valueColumn,
                    LabelFormatter = value => value.ToString("N0")
                });

                // Zoom ve Pan özelliklerini ekle
                chart.Zoom = ZoomingOptions.X;
                chart.Pan = PanningOptions.X;

                panel.Controls.Add(chart);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafik oluşturulurken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Modern Pasta Grafiği
        public void ModernPastaGrafiğiOlustur(PanelControl panel, DataTable dataTable,
      string categoryColumn, string valueColumn, int maxCategories = 10)
        {
            try
            {
                panel.Controls.Clear();

                var pieChart = new LiveCharts.WinForms.PieChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LegendLocation.Right,
                    BackColor = System.Drawing.Color.White // Fixed line
                };

                var groupedData = dataTable.AsEnumerable()
                    .GroupBy(row => row[categoryColumn].ToString())
                    .Select(g => new
                    {
                        Category = g.Key,
                        Value = g.Sum(r => Convert.ToDouble(r[valueColumn]))
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(maxCategories)
                    .ToList();

                var series = new SeriesCollection();
                int colorIndex = 0;

                foreach (var item in groupedData)
                {
                    var color = CHART_COLORS[colorIndex % CHART_COLORS.Length];
                    series.Add(new PieSeries
                    {
                        Title = $"{item.Category} ({item.Value:N0})",
                        Values = new ChartValues<double> { item.Value },
                        DataLabels = true,
                        LabelPoint = point => $"{point.Participation:P1}",
                        Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 144, 255)),
                    });
                    colorIndex++;
                }

                pieChart.Series = series;

                panel.Controls.Add(pieChart);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafik oluşturulurken hata: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





        // Yığılmış Çubuk Grafiği düzgün çalışmıyor ****************************************************

        // Alan Grafiği
        public void AlanGrafiğiOlustur(PanelControl panel, DataTable dataTable,
            string categoryColumn, List<string> valueColumns)
        {
            try
            {
                panel.Controls.Clear();
                var sampledData = SampleLargeDataset(dataTable, MAX_DATA_POINTS);

                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LegendLocation.Right,
                    DisableAnimations = sampledData.Rows.Count > 500,
                    Zoom = ZoomingOptions.Xy,        // Hem X hem Y ekseninde zoom
                    Pan = PanningOptions.Xy          // Hem X hem Y ekseninde kaydırma
                };

                var labels = sampledData.AsEnumerable()
                    .Select(row => row[categoryColumn].ToString())
                    .ToList();

                for (int i = 0; i < valueColumns.Count; i++)
                {
                    var values = new ChartValues<double>();
                    foreach (DataRow row in sampledData.Rows)
                    {
                        if (double.TryParse(row[valueColumns[i]]?.ToString(), out double value))
                        {
                            values.Add(value);
                        }
                        else
                        {
                            values.Add(0);
                        }
                    }

                    var color = CHART_COLORS[i % CHART_COLORS.Length];
                    chart.Series.Add(new LineSeries
                    {
                        Title = valueColumns[i],
                        Values = values,
                        Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, color.R, color.G, color.B)),
                        LineSmoothness = 0,
                        AreaLimit = 0,
                        StrokeThickness = 2,         // Çizgi kalınlığı
                        PointGeometrySize = 8        // Nokta büyüklüğü
                    });
                }

                chart.AxisX.Add(new Axis
                {
                    Title = categoryColumn,
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new Separator        // Izgara çizgileri
                    {
                        Step = Math.Max(1, labels.Count / 20),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 }
                    }
                });

                chart.AxisY.Add(new Axis
                {
                    Title = "Değerler",
                    LabelFormatter = value => value.ToString("N0"),
                    Separator = new Separator        // Izgara çizgileri
                    {
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 }
                    }
                });

                // Tooltip özelleştirme
                chart.DefaultLegend.Foreground = System.Windows.Media.Brushes.DarkGray;
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedYValues
                };

                panel.Controls.Add(chart);

                // Zoom talimatlarını gösteren tooltip
                var zoomLabel = new Label
                {
                    Text = "Zoom: Fare tekerleği veya sağ tık + sürükle | Sıfırla: Çift tık",
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.Gray, // Fixed line
                    Dock = DockStyle.Top
                };

                panel.Controls.Add(zoomLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafik oluşturulurken hata: {ex.Message}",
                               "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Dağılım Grafiği
        public void DağılımGrafiğiOlustur(PanelControl panel, DataTable dataTable,
            string xColumn, string yColumn)
        {
            try
            {
                panel.Controls.Clear();
                var sampledData = SampleLargeDataset(dataTable, MAX_DATA_POINTS);
                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LegendLocation.Right,
                    DisableAnimations = sampledData.Rows.Count > 500,
                    Zoom = ZoomingOptions.Xy,        // Hem X hem Y ekseninde zoom
                    Pan = PanningOptions.Xy          // Hem X hem Y ekseninde kaydırma
                };

                var values = new ChartValues<ScatterPoint>();

                // Veri noktalarını ekle ve hatalı değerleri kontrol et
                foreach (DataRow row in sampledData.Rows)
                {
                    try
                    {
                        if (row[xColumn] != DBNull.Value && row[yColumn] != DBNull.Value)
                        {
                            if (double.TryParse(row[xColumn].ToString(), out double x) &&
                                double.TryParse(row[yColumn].ToString(), out double y))
                            {
                                values.Add(new ScatterPoint(x, y));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue; // Hatalı veriyi atla
                    }
                }

                if (values.Count == 0)
                {
                    MessageBox.Show("Geçerli veri noktası bulunamadı.",
                                  "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var series = new ScatterSeries
                {
                    Title = $"{xColumn} vs {yColumn}",
                    Values = values,
                    MinPointShapeDiameter = 8,
                    MaxPointShapeDiameter = 8,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(24, 144, 255)),
                    StrokeThickness = 2
                };

                chart.Series = new SeriesCollection { series };

                // X ekseni ayarları
                chart.AxisX.Add(new Axis
                {
                    Title = xColumn,
                    LabelFormatter = value => value.ToString("N2"),
                    Separator = new Separator
                    {
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 },
                        Step = CalculateAxisStep(values.Select(v => ((ScatterPoint)v).X))
                    }
                });

                // Y ekseni ayarları
                chart.AxisY.Add(new Axis
                {
                    Title = yColumn,
                    LabelFormatter = value => value.ToString("N2"),
                    Separator = new Separator
                    {
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 },
                        Step = CalculateAxisStep(values.Select(v => ((ScatterPoint)v).Y))
                    }
                });

                // Tooltip özelleştirme
                chart.DefaultLegend.Foreground = System.Windows.Media.Brushes.DarkGray;
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedYValues
                };

                panel.Controls.Add(chart);

                // Zoom talimatları
                var zoomLabel = new Label
                {
                    Text = "Zoom: Fare tekerleği veya sağ tık + sürükle | Sıfırla: Çift tık",
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.Gray, // Fixed line
                    Dock = DockStyle.Top
                };

                panel.Controls.Add(zoomLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafik oluşturulurken hata: {ex.Message}",
                               "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Eksen adımlarını hesaplamak için yardımcı metod
        private double CalculateAxisStep(IEnumerable<double> values)
        {
            try
            {
                var min = values.Min();
                var max = values.Max();
                var range = max - min;
                var step = range / 10.0; // 10 ana bölüm olsun

                // Adımı yuvarla
                var magnitude = Math.Pow(10, Math.Floor(Math.Log10(step)));
                step = Math.Ceiling(step / magnitude) * magnitude;

                return step;
            }
            catch
            {
                return 1.0; // Varsayılan adım
            }
        }
        // Radar Grafiği
        public void RadarGrafiğiOlustur(PanelControl panel, DataTable dataTable, List<string> valueColumns)
        {
            try
            {
                panel.Controls.Clear();

                // Veri doğrulama
                if (!valueColumns.Any())
                {
                    MessageBox.Show("Lütfen en az bir sütun seçin.",
                                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LiveCharts.LegendLocation.None,
                    DisableAnimations = dataTable.Rows.Count > 100,
                    Zoom = ZoomingOptions.Xy,  // Zoom özelliği eklendi
                    Pan = PanningOptions.Xy     // Pan özelliği eklendi
                };

                var seriesCollection = new SeriesCollection();
                var labels = valueColumns.ToArray();

                // Veri setini örnekle (en fazla 20 satır)
                int step = Math.Max(1, dataTable.Rows.Count / 20);
                var sampledRows = new List<DataRow>();

                for (int i = 0; i < dataTable.Rows.Count; i += step)
                {
                    sampledRows.Add(dataTable.Rows[i]);
                    if (sampledRows.Count >= 20) break;
                }

                // Her bir örneklenmiş satır için seri oluştur
                foreach (DataRow row in sampledRows)
                {
                    var values = new ChartValues<double>();
                    bool validRow = true;

                    // Değerleri normalize et
                    foreach (var column in valueColumns)
                    {
                        if (double.TryParse(row[column]?.ToString(), out double value))
                        {
                            values.Add(value);
                        }
                        else
                        {
                            validRow = false;
                            break;
                        }
                    }

                    if (validRow)
                    {
                        var color = CHART_COLORS[seriesCollection.Count % CHART_COLORS.Length];
                        seriesCollection.Add(new LineSeries
                        {
                            Title = null,
                            Values = values,
                            LineSmoothness = 0.5,
                            PointGeometry = null,
                            StrokeThickness = 2,
                            Stroke = new System.Windows.Media.SolidColorBrush(color),
                            Fill = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromArgb(50, color.R, color.G, color.B))
                        });
                    }
                }

                chart.Series = seriesCollection;

                // Eksen ayarları
                chart.AxisX.Add(new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new Separator { Step = 1 },
                    MinValue = 0,
                    MaxValue = valueColumns.Count - 1
                });

                chart.AxisY.Add(new Axis
                {
                    MinValue = 0,
                    MaxValue = 100,
                    LabelFormatter = value => value.ToString("N0") + "%"
                });

                // Grafik paneline ekle
                panel.Controls.Add(chart);

                // Zoom ve pan talimat etiketi
                var zoomLabel = new Label
                {
                    Text = "Zoom: Fare tekerleği | Kaydırma: Sağ tık + sürükle | Sıfırla: Çift tık",
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.Gray,
                    Dock = DockStyle.Top
                };
                panel.Controls.Add(zoomLabel);

                // Zoom sıfırlama için çift tıklama olayı ekle
                chart.MouseDoubleClick += (s, e) =>
                {
                    chart.AxisX[0].MinValue = double.NaN;
                    chart.AxisX[0].MaxValue = double.NaN;
                    chart.AxisY[0].MinValue = 0;
                    chart.AxisY[0].MaxValue = 100;
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Radar grafiği oluşturulurken hata: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Histogram
        public void HistogramOlustur(PanelControl panel, DataTable dataTable, string valueColumn, int binCount = 20)
        {
            try
            {
                panel.Controls.Clear();

                // Veri doğrulama
                if (!dataTable.Columns.Contains(valueColumn))
                {
                    MessageBox.Show($"'{valueColumn}' sütunu bulunamadı.",
                                  "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Geçerli sayısal değerleri topla
                var values = dataTable.AsEnumerable()
                    .Select(row => row[valueColumn])
                    .Where(value => value != DBNull.Value)
                    .Select(value =>
                    {
                        if (double.TryParse(value.ToString(), out double result))
                            return result;
                        return double.NaN;
                    })
                    .Where(x => !double.IsNaN(x))
                    .ToList();

                if (values.Count == 0)
                {
                    MessageBox.Show("Geçerli sayısal veri bulunamadı.",
                                  "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var min = values.Min();
                var max = values.Max();
                var binWidth = (max - min) / binCount;

                // Histogram verilerini hesapla
                var bins = new double[binCount];
                foreach (var value in values)
                {
                    var binIndex = (int)((value - min) / binWidth);
                    if (binIndex == binCount) binIndex--;
                    bins[binIndex]++;
                }

                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    LegendLocation = LegendLocation.Right,
                    DisableAnimations = values.Count > 1000,
                    Zoom = ZoomingOptions.Xy,        // Hem X hem Y ekseninde zoom
                    Pan = PanningOptions.Xy          // Hem X hem Y ekseninde kaydırma
                };

                var columnSeries = new ColumnSeries
                {
                    Title = "Frekans",
                    Values = new ChartValues<double>(bins),
                    Fill = new SolidColorBrush(CHART_COLORS[0]),
                    MaxColumnWidth = 50,
                    DataLabels = true,
                    LabelPoint = point => point.Y.ToString("N0")
                };

                chart.Series = new SeriesCollection { columnSeries };

                // Aralık etiketlerini oluştur
                var labels = new List<string>();
                for (int i = 0; i < binCount; i++)
                {
                    var start = min + (i * binWidth);
                    var end = start + binWidth;
                    labels.Add($"{start:N2}-{end:N2}");
                }

                // X ekseni ayarları
                chart.AxisX.Add(new Axis
                {
                    Title = valueColumn,
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new Separator
                    {
                        Step = Math.Max(1, labels.Count / 15),  // Etiket yoğunluğunu ayarla
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 }
                    }
                });

                // Y ekseni ayarları
                chart.AxisY.Add(new Axis
                {
                    Title = "Frekans",
                    LabelFormatter = value => value.ToString("N0"),
                    Separator = new Separator
                    {
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2 }
                    }
                });

                // Tooltip özelleştirme
                chart.DefaultLegend.Foreground = System.Windows.Media.Brushes.DarkGray;
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedYValues
                };

                panel.Controls.Add(chart);

                // Zoom talimatları
                var zoomLabel = new Label
                {
                    Text = "Zoom: Fare tekerleği veya sağ tık + sürükle | Sıfırla: Çift tık",
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.Gray,
                    Dock = DockStyle.Top
                };
                panel.Controls.Add(zoomLabel);

                // İstatistiksel bilgiler
                var statsLabel = new Label
                {
                    Text = $"Toplam Veri: {values.Count} | " +
                          $"Minimum: {min:N2} | " +
                          $"Maksimum: {max:N2} | " +
                          $"Ortalama: {values.Average():N2}",
                    AutoSize = true,
                    ForeColor = System.Drawing.Color.Gray,

                    Dock = DockStyle.Bottom
                };
                panel.Controls.Add(statsLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Histogram oluşturulurken hata: {ex.Message}",
                               "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void KorelasyonMatrisiOlustur(PanelControl panel, DataTable dataTable)
        {
            try
            {
                panel.Controls.Clear();

                // Sayısal sütunları al
                var numericColumns = dataTable.Columns.Cast<DataColumn>()
                    .Where(col => IsNumericType(col.DataType))
                    .Select(col => col.ColumnName)
                    .ToList();

                if (numericColumns.Count < 2)
                {
                    MessageBox.Show("Korelasyon matrisi için en az 2 sayısal sütun gereklidir.",
                                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcıya sütun seçimi yaptırmak için CheckedListBox oluştur
                var checkedListBox = new CheckedListBox
                {
                    SelectionMode = SelectionMode.MultiExtended,
                    Width = 300,
                    Height = 200,
                    CheckOnClick = true
                };

                foreach (var column in numericColumns)
                {
                    checkedListBox.Items.Add(column);
                }

                // Form oluştur
                var selectionForm = new Form
                {
                    Text = "Sütun Seçimi",
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    StartPosition = FormStartPosition.CenterScreen
                };

                // Formun içeriğini ayarla
                var okButton = new Button
                {
                    Text = "Tamam",
                    DialogResult = DialogResult.OK,
                    Dock = DockStyle.Bottom
                };

                selectionForm.Controls.Add(checkedListBox);
                selectionForm.Controls.Add(okButton);

                // Formu göster ve sonucu kontrol et
                var result = selectionForm.ShowDialog();

                if (result != DialogResult.OK || checkedListBox.CheckedItems.Count < 2)
                {
                    MessageBox.Show("Korelasyon matrisi için en az 2 sütun seçmelisiniz.",
                                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Seçilen sütunları al
                var selectedColumns = checkedListBox.CheckedItems.Cast<string>().ToList();

                // Korelasyon matrisi oluştur
                var correlationMatrix = new double[selectedColumns.Count, selectedColumns.Count];
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    for (int j = 0; j < selectedColumns.Count; j++)
                    {
                        var values1 = GetColumnValues(dataTable, selectedColumns[i]);
                        var values2 = GetColumnValues(dataTable, selectedColumns[j]);
                        correlationMatrix[i, j] = CalculateCorrelation(values1, values2);
                    }
                }

                // Grafik oluştur ve ekle
                var chart = new LiveCharts.WinForms.CartesianChart
                {
                    Dock = DockStyle.Fill,
                    DisableAnimations = true,
                    LegendLocation = LiveCharts.LegendLocation.None
                };

                var heatValues = new ChartValues<double>(correlationMatrix.Cast<double>());

                var heatSeries = new HeatSeries
                {
                    Values = heatValues,
                    DataLabels = true,
                    LabelPoint = point => point.Weight.ToString("N2")
                };

                chart.Series = new SeriesCollection { heatSeries };

                chart.AxisX.Add(new Axis
                {
                    Labels = selectedColumns,
                    LabelsRotation = 45,
                    Separator = new Separator { Step = 1 }
                });

                chart.AxisY.Add(new Axis
                {
                    Labels = selectedColumns,
                    Separator = new Separator { Step = 1 }
                });

                panel.Controls.Add(chart);

                // Renk skalası ekle
                var colorScale = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    Dock = DockStyle.Bottom,
                    Height = 30,
                    Padding = new Padding(10)
                };

                colorScale.Controls.AddRange(new Control[]
                {
            new Label { Text = "-1", AutoSize = true, Margin = new Padding(5) },
            new Panel
            {
                Width = 200,
                Height = 20,
                BackgroundImage = CreateGradientImage(),
                Margin = new Padding(5, 0, 5, 0)
            },
            new Label { Text = "1", AutoSize = true, Margin = new Padding(5) }
                });

                panel.Controls.Add(colorScale);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Korelasyon matrisi oluşturulurken hata oluştu: {ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Yeni yardımcı metod
        private bool HasValidNumericData(DataTable dataTable, string columnName)
        {
            return dataTable.AsEnumerable()
                .Any(row => row[columnName] != DBNull.Value &&
                           double.TryParse(row[columnName].ToString(), out _));
        }


        private double[] GetColumnValues(DataTable dataTable, string columnName)
        {
            return dataTable.AsEnumerable()
                .Where(row => row[columnName] != DBNull.Value)
                .Select(row => Convert.ToDouble(row[columnName]))
                .ToArray();
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


        private double CalculateCorrelation(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length == 0)
                return 0;

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

            if (sumX2 == 0 || sumY2 == 0)
                return 0;

            return sumXY / Math.Sqrt(sumX2 * sumY2);
        }

        private Bitmap CreateGradientImage()
        {
            var bitmap = new Bitmap(200, 20);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new PointF(rect.Left, rect.Top),
                    new PointF(rect.Right, rect.Top),
                    System.Drawing.Color.FromArgb(255, 0, 0),   // Kırmızı (-1)
                    System.Drawing.Color.FromArgb(0, 255, 0)))  // Yeşil (1)
                {
                    g.FillRectangle(brush, rect);
                }
            }
            return bitmap;
        }




    }
}