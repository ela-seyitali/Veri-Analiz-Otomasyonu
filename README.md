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

## 📋 Extended Description

### Projenin Amacı

Veri Analiz Otomasyonu Projesi, teknik bilgiye sahip olmayan kullanıcıların bile karmaşık veri analizlerini kolayca yapabilmesini sağlamak amacıyla geliştirilmiştir. Kullanıcıların veri setlerini yükleyerek analiz yapmalarını, bu analizleri görselleştirmelerini ve sonuçları PDF rapor olarak indirmelerini sağlar. Bu sistem, veri bilimi alanındaki temel ve ileri analiz yöntemlerini bir araya getirerek kullanıcıların verilerini daha verimli bir şekilde işlemelerine yardımcı olur.

### Projenin Kullanıcı Kitlesi

Bu proje, bireysel kullanıcılar, küçük ve orta büyüklükteki işletmeler (KOBİ'ler), öğrenciler, akademisyenler ve veri analistleri gibi geniş bir kitleye hitap etmektedir. Özellikle veri analizi ve görselleştirme süreçlerini hızlı ve etkili bir şekilde gerçekleştirmek isteyen kullanıcılar için ideal bir çözümdür.

### Projenin Sağladığı Faydalar

- **Hızlı ve Kolay Veri Analizi:** Kullanıcılar, birkaç tıklamayla veri setlerini yükleyip analiz yapabilir.
- **Güvenlik:** SHA-256 şifreleme algoritması ile kullanıcı bilgileri ve veriler güvence altına alınır.
- **Görselleştirme:** Farklı grafik türleri ve özet tablolar sayesinde veriler daha anlaşılır hale getirilir.
- **Raporlama:** Kullanıcılar analiz sonuçlarını PDF formatında indirip rapor olarak saklayabilir.
- **Kişiselleştirme:** Kullanıcılar, uygulamanın görünümünü farklı temalarla özelleştirebilir.

### Geliştirme Süreci ve Kullanılan Teknolojiler

Proje, C# programlama dili ve .NET Framework kullanılarak geliştirilmiştir. Veri görselleştirme için LiveCharts ve Windows Forms Chart Control bileşenleri kullanılmıştır. Kullanıcı arayüzü tasarımı için DevExpress araçları tercih edilmiştir. Veritabanı yönetimi Microsoft SQL Server ile gerçekleştirilmiş ve PDF raporlamalar için iTextSharp kütüphanesi entegre edilmiştir.

### Gelecekteki Geliştirmeler

Proje, modüler yapısı sayesinde gelecekte eklenebilecek birçok yeni özellik için uygun bir altyapıya sahiptir. Önerilen geliştirmeler şunlardır:

- **Makine Öğrenimi Entegrasyonu:** Otomatik modelleme ve aykırı değer tespiti
- **Veri Manipülasyonu:** Otomatik veri temizleme ve birleştirme
- **API Entegrasyonları:** Harici veri kaynaklarıyla bağlantı ve entegrasyon
- **Daha Fazla Veri Formatı Desteği:** Farklı veri formatlarını destekleme

Bu proje, veri analiz süreçlerini optimize etmek ve kullanıcıların karar alma süreçlerinde daha etkin olmalarına yardımcı olmak için tasarlanmıştır.

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

## 🖥️ Setup Dosyası
Oluşturduğunuz setup dosyasını indirerek uygulamayı kolayca yükleyebilirsiniz.

### Setup Dosyasını İndirme ve Kurulum
1. **Setup dosyasını indirin:** [Veri Analiz Otomasyonu Setup](https://drive.google.com/file/d/1U2VL7Xt5IAOBRTd73rKHN2iCN8ozpc_U/view?usp=drive_link)
2. İndirilen setup dosyasını çalıştırın.
3. Kurulum talimatlarını takip ederek uygulamayı bilgisayarınıza yükleyin.
4. Kurulum tamamlandıktan sonra masaüstünüzdeki kısayolu kullanarak uygulamayı başlatın.

> **Not:** Setup dosyasını oluştururken Visual Studio Installer veya WiX Toolset gibi araçlar kullanılmıştır.

## 📚 Kullanım Rehberi
1. **Giriş Yap veya Kayıt Ol:** Uygulamayı kullanmak için kullanıcı hesabı oluşturun veya mevcut hesabınızla giriş yapın.
2. **Veri Yükleme:** "Veri Seti Yükle" butonuna tıklayarak CSV dosyanızı seçin.
3. **Veri Görüntüleme ve Analiz:** Yüklenen veri üzerinde temel ve ileri düzey analizleri gerçekleştirin.
4. **Grafikler ve Tablolar:** Verinizi görselleştirin ve grafik oluşturun.
5. **Rapor Oluşturma:** Analiz sonuçlarını PDF rapor olarak indirin.
6. **Tema Seçimi:** Uygulama temasını kişiselleştirin.

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


## 📧 İletişim
Herhangi bir sorunuz varsa, lütfen bizimle iletişime geçin:
- **E-posta:** elaseyitali2@gmail.com
- **GitHub:** [Ela Seyitali](https://github.com/ela-seyitali)

---
**Proje Geliştirici:** Ela Seyitali
**Danışmanlar:** Dr. Öğr. Üyesi V. Cem BAYDOĞAN, Arş. Gör. Hüseyin Alperen DAĞDÖGEN 

Bu proje, Fırat Üniversitesi Teknoloji Fakültesi Yazılım Mühendisliği Bölümü'nde YMH219 Nesne Tabanlı Programlama dersi için geliştirilmiştir.

