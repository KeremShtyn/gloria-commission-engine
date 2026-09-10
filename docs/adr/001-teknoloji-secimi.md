# ADR-001: Teknoloji seçimi

## Bağlam

Case .NET 8+ Web API, EF Core, SQL Server LocalDB veya SQLite, frontend için React (Vite) istiyor.
Serbest bırakılan kısımlar: katman yapısı, veritabanı, API stili.

## Karar

.NET 8 (SDK `global.json` ile 8.0.405'e sabitlendi), EF Core 8, SQLite, React 19 + TypeScript.

Birincil anahtarlar `Guid`. Sıralı tamsayı kimlik URL'de tahmin edilebilir olur
(`/commission-rules/7` görünce 8'i denersin); maaş verisi taşıyan bir sistemde bu istenmez.
Rastgele GUID'in indeks parçalaması sorununu önlemek için zaman damgası önekli
(UUIDv7 düzeninde) üretiliyor — `SequentialGuid`.

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
- **`Guid` de TEXT olarak yatıyor** (36 karakter). SQL Server'da `uniqueidentifier` 16 bayt.
  SQL Server'a geçerken birincil anahtarın kümelenmiş indeks olmaması tercih edilmeli;
  UUIDv7 düzeni string sıralamasında artan, SQL Server'ın kendi GUID sıralaması farklı çalışır.

Üretimde SQL Server'a geçilir. Entity'ler, EF konfigürasyonları ve indeksler olduğu gibi taşınır;
migration'lar sağlayıcıya özgü olduğu için yeniden üretilmesi gerekir.

SDK'yı sabitlemek, makinesinde .NET 9 olan birinde de aynı derlemenin çıkmasını sağlıyor.

## Elenen alternatif

**SQL Server LocalDB:** Yalnızca Windows'ta çalışıyor, Docker Compose'da ek konteyner ve
bekleme süresi demek. Tek komutla ayağa kalkma hedefini bozuyordu.
