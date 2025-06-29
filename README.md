# Product Catalog API

Bu proje, .NET 8 ve Angular ile geliştirilmiş, tedarik zinciri ürün stok takibi ve sipariş otomasyonu sağlayan bir katalog yönetim sistemidir. Hem in-memory (iç sistem) hem de Fake Store API'den ürünleri birleştirerek sunar. Stoklar backend'de Roma rakamına çevrilir ve frontend'de sadece gösterilir.

##  Özellikler

### Backend (.NET 8)
- **Katmanlı Mimari**: Controller, Service, Repository, ExternalClient
- **Fake Store API Entegrasyonu**: Dış API'den ürün verisi çekme
- **İç Sistem Kataloğu**: In-memory ürün ekleme, güncelleme, eşleştirme
- **Birleşik Ürün Listesi**: Hem eklenen hem dış API ürünleri tek endpointte
- **Kritik Stok Tespiti**: Eşik altı ürünleri listeler
- **Sipariş Otomasyonu**: Kritik stoklu ürünler için en uygun fiyatlı sipariş
- **Güvenlik**: Rate limiting, CSRF koruması, XSS önlemleri
- **Roma Rakamı**: Stoklar backend'de Roma rakamına çevrilir

### Frontend (Angular)
- Modern, responsive arayüz
- Ürün ve sipariş yönetimi
- Her sayfa geçişinde API'den güncel veri çekme
- Stoklar backend'den gelen Roma rakamı ile gösterilir

##  Proje Yapısı

```
ProductCatalogApi/           # Backend (ASP.NET Core)
ProductCatalogFrontend/      # Frontend (Angular)
```

##  Teknolojiler
- .NET 8, ASP.NET Core Web API
- Angular 20.0.4, TypeScript
- SOLID, Dependency Injection
- Rate Limiting, CSRF, Exception Handling

##  API Endpoint'leri

### Ürün Yönetimi
```
GET    /api/products                    # Birleşik ürün listesi (in-memory + Fake Store)
POST   /api/products                    # Yeni ürün ekle (in-memory)
GET    /api/products/low-stock          # Kritik stoklu ürünler
POST   /api/products/from-external/{id} # Fake Store ürününden iç ürün oluştur
GET    /api/products/external           # Sadece Fake Store ürünleri
GET    /api/products/{productCode}      # Ürün detayı
```

### Sipariş Yönetimi
```
GET    /api/orders                      # Tüm siparişleri listele
POST   /api/orders/check-and-place      # Kritik stoklar için otomatik sipariş
GET    /api/orders/{orderId}            # Sipariş detayı
```

### Diğer
```
GET    /api/products/{code}/stock-roman # Ürün stoğunu Roma rakamı ile
GET    /api/products/utils/roman/{num}  # Sayıyı Roma rakamına çevir
GET    /api/products/mapping/fake-store/{id}     # Fake Store ID → Product Code
GET    /api/products/mapping/product-code/{code} # Product Code → Fake Store ID
```

##  Güvenlik
- **Rate Limiting**: POST endpoint'leri için dakikada 100 istek
- **CSRF**: Tüm değiştirici isteklerde token zorunlu
- **XSS**: Angular template binding ile otomatik koruma
- **Exception Handling**: Global hata yönetimi ve kullanıcı dostu mesajlar

##  Roma Rakamı
- Stok miktarları sadece backend'de Roma rakamına çevrilir (`stockInRoman`)
- Frontend'de hiçbir şekilde hesaplama yapılmaz, sadece gösterilir

##  Örnek API Kullanımı

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

##  Veri Modelleri

### Product
```json
{
  "productCode": "ABC12345",
  "name": "Ürün Adı",
  "threshold": 10,
  "initialStock": 50,
  "currentStock": 45,
  "createdAt": "2024-01-01T00:00:00Z",
  "fakeStoreId": 1,
  "stockInRoman": "XLV"
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

##  Fake Store API Entegrasyonu
- Dış API'den ürünler çekilir, ancak ekleme/güncelleme yapılamaz
- İç sistemde eklenen ürünler ile Fake Store ürünleri `productCode` ve `fakeStoreId` ile eşleştirilir
- Birleşik katalogda tekrar eden ürünler gösterilmez

##  Task ve Bonuslar
- Ürünler hem in-memory hem Fake Store API'den birleşik listelenir
- Kritik stok tespiti ve otomatik sipariş
- Rate limit, CSRF, XSS, exception handling
- **Bonus:** Stoklar sadece backend'de Roma rakamına çevrilir, frontend'de hesaplama yapılmaz