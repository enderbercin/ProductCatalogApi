# Product Catalog API

Bu proje, .NET 8 ve Angular ile geliştirilmiş bir ürün katalog yönetim sistemidir. Fake Store API entegrasyonu ile dış kaynaklardan ürün verilerini çeker ve iç sistem kataloğu ile entegre eder.

## 🚀 Özellikler

### Backend (.NET 8)
- **Katmanlı Mimari**: Controller/Service/Repository/ExternalClient katmanları
- **Fake Store API Entegrasyonu**: Dış API'den ürün verilerini çekme
- **Ürün Yönetimi**: Ürün ekleme, listeleme, stok takibi
- **Sipariş Otomasyonu**: Kritik stoklu ürünler için otomatik sipariş oluşturma
- **Güvenlik**: Rate limiting, CSRF koruması
- **Roma Rakamı Dönüşümü**: Stok miktarlarını Roma rakamı ile gösterme

### Frontend (Angular)
- Modern ve kullanıcı dostu arayüz
- Ürün listeleme ve yönetimi
- Sipariş takibi
- Roma rakamı görüntüleme

## 🏗️ Proje Yapısı

```
ProductCatalogApi/
├── Controllers/          # API Controller'ları
├── Services/            # İş mantığı katmanı
├── Repositories/        # Veri erişim katmanı
├── Models/              # Veri modelleri
├── ExternalClients/     # Dış API entegrasyonu
├── Middleware/          # Güvenlik middleware'leri
└── Utils/              # Yardımcı sınıflar

ProductCatalogFrontend/
├── src/
│   ├── app/            # Angular bileşenleri
│   ├── services/       # API servisleri
│   └── models/         # TypeScript modelleri
```

## 🛠️ Teknolojiler

### Backend
- **.NET 8**
- **ASP.NET Core Web API**
- **Dependency Injection**
- **SOLID Prensipleri**

### Frontend
- **Angular 17**
- **TypeScript**
- **CSS3**

### Güvenlik
- **Rate Limiting**
- **CSRF Koruması**
- **Exception Handling**

## 📋 API Endpoint'leri

### Ürün Yönetimi
```
GET    /api/products                    # Tüm ürünleri listele
GET    /api/products/{productCode}      # Ürün detayı
POST   /api/products                    # Yeni ürün ekle
GET    /api/products/external           # Dış API ürünleri
GET    /api/products/low-stock          # Kritik stoklu ürünler
POST   /api/products/from-external/{id} # Dış üründen iç ürün oluştur
```

### Sipariş Yönetimi
```
GET    /api/orders                      # Tüm siparişleri listele
GET    /api/orders/{orderId}            # Sipariş detayı
POST   /api/orders/check-and-place      # Otomatik sipariş oluştur
```

### Roma Rakamı Dönüşümü
```
GET    /api/products/{code}/stock-roman # Ürün stoğunu Roma rakamı ile
GET    /api/products/utils/roman/{num}  # Sayıyı Roma rakamına çevir
```

### Ürün Eşleştirme
```
GET    /api/products/mapping/fake-store/{id}     # Fake Store ID → Product Code
GET    /api/products/mapping/product-code/{code} # Product Code → Fake Store ID
```

## 🚀 Kurulum ve Çalıştırma

### Backend (.NET)

1. **Gereksinimler**
   - .NET 8 SDK
   - Visual Studio 2022 veya VS Code

2. **Kurulum**
   ```bash
   cd ProductCatalogApi
   dotnet restore
   dotnet build
   ```

3. **Çalıştırma**
   ```bash
   dotnet run
   ```
   
   API http://localhost:5164 adresinde çalışacaktır.

### Frontend (Angular)

1. **Gereksinimler**
   - Node.js 18+
   - Angular CLI

2. **Kurulum**
   ```bash
   cd ProductCatalogFrontend
   npm install
   ```

3. **Çalıştırma**
   ```bash
   ng serve
   ```
   
   Uygulama http://localhost:4200 adresinde çalışacaktır.

## 📝 Örnek API İstekleri

### Yeni Ürün Ekleme
```bash
curl -X POST "http://localhost:5164/api/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Ürün",
    "threshold": 10,
    "initialStock": 50
  }'
```

### Dış API'den Ürün Oluşturma
```bash
curl -X POST "http://localhost:5164/api/products/from-external/1"
```

### Kritik Stoklu Ürünleri Listeleme
```bash
curl -X GET "http://localhost:5164/api/products/low-stock"
```

### Otomatik Sipariş Oluşturma
```bash
curl -X POST "http://localhost:5164/api/orders/check-and-place"
```

### Roma Rakamı Dönüşümü
```bash
curl -X GET "http://localhost:5164/api/products/utils/roman/42"
```

## 🔒 Güvenlik Özellikleri

### Rate Limiting
- POST endpoint'leri için dakikada 100 istek sınırı
- HTTP 429 yanıtı ile limit aşımı bildirimi

### CSRF Koruması
- POST/PUT/DELETE istekleri için token doğrulaması
- Otomatik token üretimi ve doğrulama

### Exception Handling
- Global exception handler
- Kullanıcı dostu hata mesajları
- Detaylı loglama

## 🧮 Roma Rakamı Dönüşüm Algoritması

### Algoritma Adımları:
1. Sayıyı al
2. En büyük Roma rakamı değerinden başla
3. Sayıyı böl ve kalanla devam et
4. Her bölüm adımında sembol dizisini biriktir

### Dönüşüm Tablosu:
```
1 → I, 4 → IV, 5 → V, 9 → IX, 10 → X
40 → XL, 50 → L, 90 → XC, 100 → C
400 → CD, 500 → D, 900 → CM, 1000 → M
```

### Örnek Çıktılar:
- 42 → XLII
- 99 → XCIX
- 2024 → MMXXIV
- 0 veya negatif → N/A

## 🔄 Fake Store API Entegrasyonu

### Ürün Eşleştirme Mantığı:
1. **İki Yönlü Eşleştirme**: Fake Store ID ↔ Product Code
2. **Otomatik Ürün Oluşturma**: Dış üründen iç sistem ürünü
3. **Stok Takibi**: İç sistem stok yönetimi
4. **Fiyat Karşılaştırma**: En uygun fiyatlı satıcı seçimi

### Eşleştirme Endpoint'leri:
- `POST /api/products/from-external/{id}`: Dış üründen iç ürün oluştur
- `GET /api/products/mapping/fake-store/{id}`: ID → Code eşleştirmesi
- `GET /api/products/mapping/product-code/{code}`: Code → ID eşleştirmesi

## 📊 Veri Modelleri

### Product
```json
{
  "productCode": "ABC12345",
  "name": "Ürün Adı",
  "threshold": 10,
  "initialStock": 50,
  "currentStock": 45,
  "createdAt": "2024-01-01T00:00:00Z",
  "fakeStoreId": 1
}
```

### Order
```json
{
  "orderId": "ORD12345",
  "productCode": "ABC12345",
  "supplierName": "Fake Store API",
  "price": 29.99,
  "quantity": 20,
  "status": "Pending",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 👨‍💻 Geliştirici

Bu proje, task.txt dosyasındaki gereksinimlere göre geliştirilmiştir.

---

**Not**: Bu proje geliştirme amaçlıdır ve production ortamında kullanılmadan önce ek güvenlik önlemleri alınmalıdır. 