# Veri Analiz Otomasyonu

Bu proje, veri bilimi süreçlerini basitleştirmek için tasarlanmış kullanıcı dostu bir masaüstü uygulamasıdır. Kullanıcıların CSV formatındaki veri setlerini yükleyerek analiz yapmalarını, verileri görselleştirmelerini ve analiz sonuçlarını PDF raporlar halinde kaydetmelerini sağlar.

## 🔍 Projenin Genel Tanıtımı
Veri Analiz Otomasyonu, kullanıcıların temel ve ileri düzey veri analizlerini hızlı ve kolay bir şekilde gerçekleştirebileceği bir masaüstü uygulamasıdır. Proje, kullanıcıların analiz süreçlerini grafikler ve özet tablolarla desteklerken, güvenliği ön planda tutar.

### 🎯 Proje Özellikleri
- Kullanıcı dostu arayüz
- CSV dosyası yükleme ve saklama
- Veri görselleştirme (grafikler ve tablolar)
- Temel istatistiksel analizler
- PDF rapor oluşturma
- Kullanıcı bilgileri ve dosyalar için güvenlik (SHA-256 şifreleme)
- Tema özelleştirme seçenekleri

## 📋 Proje Özellikleri Detaylı Açıklama

### 1️⃣ Kullanıcı Yönetimi
- Kullanıcılar sisteme giriş yapabilir veya yeni hesap oluşturabilir.
- Şifreler SHA-256 algoritmasıyla şifrelenir.
- Şifrelerin en az 8 karakter uzunluğunda olması sağlanır.

### 2️⃣ CSV Dosya Yükleme ve Saklama
- Sistem yalnızca CSV formatındaki dosyaları destekler.
- Kullanıcılar yükledikleri verileri veri tabanında saklayabilir ve istedikleri zaman bu verilere erişebilir.

### 3️⃣ Veri Görselleştirme
- Kullanıcılar çizgi grafiği, pasta grafiği, dağılım grafiği, alan grafiği, radar grafiği ve histogram gibi grafik türlerini kullanabilir.
- Sayısal ve kategorik veriler üzerinde özet tablolar hazırlanabilir.

### 4️⃣ Veri Analizi
- Ortalamalar, medyan, mod, standart sapma, min-max değerleri, varyans ve korelasyon analizleri gerçekleştirilir.
- Aykırı değer tespiti ve IQR hesaplamaları yapılır.

### 5️⃣ Raporlama
- Analiz sonuçları PDF formatında rapor olarak kullanıcıya sunulur.
- Raporlar, kullanıcıların seçtikleri analizleri ve grafik türlerini içerir.

### 6️⃣ Tema Seçimi
- Koyu tema, mavi tema, gece mavisi ve varsayılan tema seçenekleri sunulur.
- Kullanıcı uygulamadan çıkış yapsa bile son seçilen tema korunur.

## 🚀 Kullanılan Teknolojiler
- **Programlama Dili:** C#
- **Geliştirme Çerçevesi:** .NET Framework
- **Veritabanı:** Microsoft SQL Server
- **Kullanıcı Arayüzü Bileşenleri:** DevExpress, Windows Forms
- **PDF İşlemleri:** iTextSharp
- **Veri Görselleştirme:** LiveCharts, Windows Forms Chart Control
- **Güvenlik:** SHA-256 algoritması

## 📦 Kurulum Talimatları
1. Bu projeyi bilgisayarınıza klonlayın:
   ```bash
   git clone https://github.com/kullanici/veri-analiz-otomasyonu.git
   ```
2. Proje dizinine gidin:
   ```bash
   cd veri-analiz-otomasyonu
   ```
3. Projeyi Visual Studio ile açın ve gerekli bağımlılıkları yükleyin.
4. Veritabanı bağlantısını yapılandırın ve uygulamayı çalıştırın.

## 📚 Kullanım Rehberi
1. **Giriş Yap veya Kayıt Ol:** Uygulamayı kullanmak için kullanıcı hesabı oluşturun veya mevcut hesabınızla giriş yapın.
2. **Veri Yükleme:** "Veri Seti Yükle" butonuna tıklayarak CSV dosyanızı seçin.
3. **Veri Görüntüleme ve Analiz:** Yüklenen veri üzerinde temel ve ileri düzey analizleri gerçekleştirin.
4. **Grafikler ve Tablolar:** Verinizi görselleştirin ve grafik oluşturun.
5. **Rapor Oluşturma:** Analiz sonuçlarını PDF rapor olarak indirin.
6. **Tema Seçimi:** Uygulama temasını kişiselleştirin.

## 📊 Örnek Grafik ve Tablolar
| Grafik Türü     | Örnek                          |
|-----------------|--------------------------------|
| Çizgi Grafiği   | ![Line Chart](docs/line_chart.png) |
| Pasta Grafiği   | ![Pie Chart](docs/pie_chart.png)   |
| Histogram       | ![Histogram](docs/histogram.png)   |

## 🛠️ Projeye Katkıda Bulunma
Katkılarınızı memnuniyetle karşılıyoruz! Lütfen aşağıdaki adımları izleyin:
1. Projeyi fork edin.
2. Yeni bir branch oluşturun:
   ```bash
   git checkout -b yeni-ozellik
   ```
3. Değişikliklerinizi yapın ve commit edin:
   ```bash
   git commit -m 'Yeni özellik eklendi'
   ```
4. Branch'inizi push edin:
   ```bash
   git push origin yeni-ozellik
   ```
5. Pull request gönderin.

## 🔐 Güvenlik
Bu proje kullanıcı verilerini ve dosyalarını güvenli bir şekilde saklamak için SHA-256 algoritmasını kullanır. Şifreler geri döndürülemez şekilde şifrelenir.

## 📄 Lisans
Bu proje [MIT Lisansı](LICENSE) kapsamında lisanslanmıştır.

## 📧 İletişim
Herhangi bir sorunuz varsa, lütfen bizimle iletişime geçin:
- **E-posta:** alaalsaid@example.com
- **GitHub:** [GitHub Profiliniz](https://github.com/kullanici)

---
**Proje Geliştirici:** Ala Alsaid Ali
**Danışmanlar:** Dr. Öğr. Üyesi V. Cem BAYDOĞAN, Arş. Gör. Hüseyin Alperen DAĞDÖGEN, Arş. Gör. Semra ÇELEBİ

Bu proje, Fırat Üniversitesi Teknoloji Fakültesi Yazılım Mühendisliği Bölümü'nde YMH219 Nesne Tabanlı Programlama dersi için geliştirilmiştir.

