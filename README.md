# 📊 BigDataOrdersDashboard

500.000+ siparişlik bir e-ticaret veri seti üzerinde çalışan; **veri analizi**, **ML.NET ile tahminleme ve kümeleme** ve **OpenAI ile yapay zeka destekli müşteri analizi** yapan bir ASP.NET Core MVC yönetim paneli.

Projenin amacı, büyük bir veri setiyle çalışırken SQL ve LINQ sorgularını optimize etmek, veriden anlamlı içgörüler çıkarmak ve bu içgörüleri makine öğrenmesi ile yapay zeka kullanarak bir adım öteye taşımaktır. Sorgu optimizasyonu sayesinde 500 bin siparişte dashboard yükleme süresi ~55 saniyeden 1 saniyenin altına indirildi.

---

## 🚀 Özellikler

### 🗂️ Veri Yönetimi
- **Kategoriler, ürünler, müşteriler, siparişler, değerlendirmeler ve mesajlar** için listeleme, ekleme, güncelleme ve silme işlemleri
- Büyük listelerde sayfalama ve sayfa içi arama
- Silme işlemlerinde onay penceresi

### 📈 Analiz ve İstatistik
- **Dashboard:** ciro, sipariş, kategori ve ülke dağılımları, son siparişler, değerlendirme özetleri
- **Sayısal ve metinsel istatistikler**
- **Ürün performansı:** en çok satan ve en çok ciro getiren ürünler, kritik stok, hiç satılmayan ürünler, kategorilere göre iptal oranı

### 👥 Müşteri Analizi
- **RFM tabanlı sadakat skoru:** müşteriler son sipariş, sipariş sıklığı ve harcamaya göre 0–100 arası puanlanır
- **ML.NET KMeans ile müşteri segmentasyonu:** benzer davranışlı müşterilerin otomatik gruplanması
- **Elde tutma (retention) ve kayıp (churn) analizi**
- **Müşteri detay sayfası:** müşteriye özel KPI'lar, sadakat skoru, aylık harcama grafiği, sipariş geçmişi

### 🔮 ML.NET ile Tahminleme (SSA Zaman Serisi)
3 yıllık geçmiş veriden 12 aylık tahmin:
- Ödeme yöntemi tahmini
- Şehir bazlı sipariş tahmini
- Ülke bazlı sipariş tahmini
- Kategori bazlı sipariş tahmini
- Toplam satış ve ciro tahmini

### 🤖 Yapay Zeka (OpenAI)
- **AI müşteri analizi:** müşterinin sipariş verilerinden özet, gitme riski ve önerilen aksiyonlar
- **AI değerlendirme analizi:** müşteri yorumlarından memnuniyet, şikayetler ve tutarsızlık tespiti
- **Otomatik duygu analizi:** gelen mesajların duygusu (olumlu / nötr / olumsuz) otomatik belirlenir
- **Toksik içerik engelleme:** hakaret, tehdit veya nefret söylemi içeren mesajlar OpenAI Moderation ile tespit edilip kaydedilmez

> Gizlilik için yapay zekaya müşterinin adı, e-postası, telefonu veya adresi gönderilmez; sadece özet sipariş verileri gönderilir.

---

## 🛠️ Kullanılan Teknolojiler

| Alan | Teknoloji |
|---|---|
| Backend | ASP.NET Core 8 MVC |
| Veritabanı | SQL Server, Entity Framework Core |
| Makine Öğrenmesi | ML.NET (SSA Forecasting, KMeans Clustering) |
| Yapay Zeka | OpenAI API (Chat Completions, Moderation) |
| Frontend | Razor, Bootstrap, Chart.js |
| Mimari | View Components, IHttpClientFactory, IMemoryCache |

---

## 📸 Ekran Görüntüleri

### Dashboard
![Dashboard](BigDataOrdersDashboard/wwwroot/images/Dashboard%201.png)
![Dashboard](BigDataOrdersDashboard/wwwroot/images/Dashboard%202.png)

### İstatistikler
![Sayısal İstatistikler](BigDataOrdersDashboard/wwwroot/images/Say%C4%B1sal%20%C4%B0statistikler.png)
![Metinsel İstatistikler](BigDataOrdersDashboard/wwwroot/images/Metinsel%20%C4%B0statistikler.png)

### Müşteri Analizi
**Ülkeye Göre Müşteri Sadakati**
![Ülkeye Göre Müşteri Sadakati](BigDataOrdersDashboard/wwwroot/images/%C3%9Clkeye%20G%C3%B6re%20M%C3%BC%C5%9Fteri%20Sadakati.png)

**Müşteri Elde Tutma ve Kayıp Oranları**
![Müşteri Elde Tutma ve Kayıp Oranları](BigDataOrdersDashboard/wwwroot/images/M%C3%BC%C5%9Fteri%20Elde%20Tutma%20Ve%20Kay%C4%B1p%20Oranlar%C4%B1.png)

**Müşteri Detay Sayfası**
![Müşteri Detay Sayfası](BigDataOrdersDashboard/wwwroot/images/M%C3%BC%C5%9Fteri%20Detay%20Sayfas%C4%B1.png)
![Müşteri Detay Sayfası](BigDataOrdersDashboard/wwwroot/images/M%C3%BC%C5%9Fteri%20Detay%20Sayfas%C4%B1%202.png)

### ML.NET Tahminleme
**Satış Tahmini**
![Satış Tahmini](BigDataOrdersDashboard/wwwroot/images/Sat%C4%B1%C5%9F%20Tahmini.png)

**Ödeme Yöntemi Tahmini**
![Ödeme Yöntemi Tahmini](BigDataOrdersDashboard/wwwroot/images/%C3%96deme%20Y%C3%B6ntemi%20Tahmini.png)

**Şehir Sipariş Tahmini**
![Şehir Sipariş Tahmini](BigDataOrdersDashboard/wwwroot/images/%C5%9Eehir%20Sipari%C5%9F%20Tahmini.png)

**Ülke Sipariş Tahmini**
![Ülke Sipariş Tahmini](BigDataOrdersDashboard/wwwroot/images/%C3%9Clke%20Sipari%C5%9F%20Tahmini.png)

**Kategori Sipariş Tahmini**
![Kategori Sipariş Tahmini](BigDataOrdersDashboard/wwwroot/images/Kategori%20Sipari%C5%9F%20Tahmini.png)

### Yapay Zeka (OpenAI)
**AI Müşteri Sipariş Analizi**
![AI Müşteri Sipariş Analizi](BigDataOrdersDashboard/wwwroot/images/Open%20AI%20M%C3%BC%C5%9Fteri%20Sipari%C5%9F%20Analizi.png)

**AI Müşteri Yorum Analizi**
![AI Müşteri Yorum Analizi](BigDataOrdersDashboard/wwwroot/images/Open%20AI%20M%C3%BC%C5%9Fteri%20Yorum%20Analizi.png)

**Toksik Mesaj Engelleme**
![Toksik Mesaj Engelleme](BigDataOrdersDashboard/wwwroot/images/Toksik%20Mesaj%20Engelleme.png)

---

## ⚙️ Kurulum

1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/EmreHaci03/BigDataOrdersDashboard.git
   ```
2. `appsettings.json` içindeki `DefaultConnection` bağlantı cümlesini kendi SQL Server'ınıza göre düzenleyin.
3. Veritabanını oluşturun:
   ```bash
   dotnet ef database update
   ```
   > Not: Sipariş verileri repoda bulunmamaktadır; tablolar boş olarak oluşturulur.
4. OpenAI API anahtarınızı User Secrets ile ekleyin:
   ```bash
   dotnet user-secrets set "OpenAI:Key" "sk-..."
   ```
5. Projeyi çalıştırın:
   ```bash
   dotnet run
   ```
