# ADR-001: Teknoloji seçimi

## Bağlam

Case .NET 8+ Web API, EF Core, SQL Server LocalDB veya SQLite, frontend için React (Vite) istiyor.
Serbest bırakılan kısımlar: katman yapısı, veritabanı, API stili.

## Karar

.NET 8 (SDK `global.json` ile 8.0.405'e sabitlendi), EF Core 8, SQLite, React 19 + TypeScript.

Dört proje: Domain, Application, Infrastructure, Api. Bağımlılık içeri doğru akar —
kural motoru Application'da ve EF Core'a bağımlı değil.

Minimal API kullanıldı; endpoint sayısı az ve hepsi ince.

## Sonuçlar

SQLite dosya tabanlı olduğu için `docker compose up` dışında kurulum gerektirmiyor,
değerlendirici tek komutla çalıştırabiliyor.

Bunun bedeli var ve üretime taşırken bilinmeli:

- **Eşzamanlı yazma sınırlı.** SQLite yazarken veritabanını kilitliyor. Gecelik ETL koşarken
  personel ekranı açıksa çakışır.
- **`decimal` kolonlar TEXT olarak yatıyor.** EF Core'un SQLite sağlayıcısında `HasPrecision`
  etkisiz. Eşitlik ve bellekte toplama sorunsuz ama `ORDER BY tutar` sözlük sırası verir —
  para üzerinde SQL tarafında sıralama yapan bir sorgu sessizce yanlış sonuç döndürür.

Üretimde SQL Server'a geçilir. Entity'ler, EF konfigürasyonları ve indeksler olduğu gibi taşınır;
migration'lar sağlayıcıya özgü olduğu için yeniden üretilmesi gerekir.

SDK'yı sabitlemek, makinesinde .NET 9 olan birinde de aynı derlemenin çıkmasını sağlıyor.

## Elenen alternatif

**SQL Server LocalDB:** Yalnızca Windows'ta çalışıyor, Docker Compose'da ek konteyner ve
bekleme süresi demek. Tek komutla ayağa kalkma hedefini bozuyordu.
