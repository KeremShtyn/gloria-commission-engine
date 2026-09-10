# Gloria Prim Motoru

Otel personelinin ek hizmet satışlarından (SPA, à la carte, buggy, pavillon) hak ettiği primi,
PMS / POS / ERP kayıtlarından otomatik hesaplayan sistem.

Prim kuralları veritabanında tutulur: yeni bir prim kalemi eklemek veya oran değiştirmek
kod değişikliği gerektirmez.

- Mimari: [docs/architecture.md](docs/architecture.md)
- Teknik kararların gerekçeleri: [docs/adr](docs/adr)

## Gereksinimler

.NET 8 SDK, Node 22. Docker ile çalıştıracaksanız ikisi de gerekmez.

## Çalıştırma

Docker ile:

```bash
docker compose up --build
```

Elle:

```bash
dotnet run --project src/Gloria.Commission.Api    # http://localhost:5199
npm --prefix web install && npm --prefix web run dev    # http://localhost:5173
```

İlk açılışta SQLite veritabanı oluşur, personel listesi ve altı örnek kural yüklenir.
Swagger: `http://localhost:5199/swagger`

## Örnek veriyi yükleme

`sample-data/` altındaki üç CSV'yi sırayla aktarın:

```bash
for s in pms pos erp; do
  case $s in
    pms) f=pms_fidelio_2026_08.csv ;;
    pos) f=pos_flyby_2026_08.csv ;;
    erp) f=erp_jde_2026_08.csv ;;
  esac
  curl -X POST "http://localhost:5199/api/v1/imports/$s" \
    -H "X-User-Role: Admin" -H "X-User-Id: admin" \
    -F "file=@sample-data/$f"
done
```

Cevapta kaç satırın yazıldığı, kaçının mükerrer olduğu ve hatalı satırların gerekçesi döner.
Verilen dosyalarda 9 hatalı satır var (bozuk tarih, `abc` tutar, kayıtsız personel, `XX` belge tipi…);
bunlar dosyayı reddettirmez, `import_errors` tablosuna düşer.

## Test

```bash
dotnet test
```

43 test var. Kademeli barem ve iade senaryoları `TieredRuleTests.cs` ve `RefundTests.cs`
altında; denetim kaydı ve dönem kilidi `PersistenceTests.cs`, yetki kuralı `AuthorizationTests.cs`
altında.

## Kimlik ve yetkilendirme

Case gerçek bir kimlik doğrulama sistemi istemiyor; kimlik HTTP header'ından okunuyor.
Yetkilendirme ise ASP.NET'in kendi altyapısıyla yapılıyor — elle yazılmış `if`'ler değil,
`[Authorize]` nitelikleri ve policy'ler.

| Header | Değer |
|---|---|
| `X-User-Role` | `Admin`, `Accounting` veya `Employee`. Yoksa istek 401 |
| `X-User-Id` | Denetim kaydının aktörü |
| `X-Employee-No` | Rol `Employee` ise personelin kendi numarası |
| `X-Correlation-Id` | Opsiyonel. Gönderilirse log ve hata cevabında bu kimlik kullanılır |

| Policy | Kim | Nerede |
|---|---|---|
| `AdminOnly` | Admin | Kural yazma, dönem kapatma/açma |
| `CanSeeAllEmployees` | Admin, Muhasebe | Dönem özeti, mutabakat, aktarım, denetim kayıtları |
| `SelfOrPrivileged` | Kendi kaydı ya da ayrıcalıklı rol | Personelin prim detayı |

`SelfOrPrivileged` bir rol kontrolü değil: hangi personelin sorgulandığı rota değerinde olduğu
için kaynak farkındalığı olan bir `IAuthorizationHandler`. Birim testleri
`AuthorizationTests.cs` altında.

Kimlik yoksa **401**, kimlik var ama yetki yetersizse **403** döner; ikisi de aynı hata biçiminde.
Yetki kontrolü ayrıca servis katmanında da tekrarlanır — controller'a güvenilmez.

**JWT'ye geçiş.** Değişecek tek yer `Program.cs`'teki şu blok:

```csharp
builder.Services
    .AddAuthentication(AuthenticationHeaders.Scheme)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(...);
```

Yerine `.AddJwtBearer(...)` gelir. Controller'lar, policy'ler, servisler ve testler değişmez;
`HttpContextCurrentUser` zaten header'ı değil claim'leri okuyor.

## API

| Endpoint | Açıklama |
|---|---|
| `GET /api/v1/commissions/{yıl}/{ay}/employees/{personelNo}` | Primi hesaplama adımlarıyla döner |
| `GET /api/v1/commissions/{yıl}/{ay}` | Dönemin tüm personel özeti |
| `GET /api/v1/commissions/{yıl}/{ay}/reconciliation` | ERP mutabakat raporu |
| `GET/POST/PUT/DELETE /api/v1/commission-rules` | Kural yönetimi |
| `POST /api/v1/imports/{pms\|pos\|erp}` | CSV aktarımı |
| `POST /api/v1/periods/{yıl}/{ay}/close` | Dönem kapatma |
| `GET /api/v1/audit-logs` | Değişiklik geçmişi |

## Ekranlar

**Prim kuralları** — kural tanımlama ve düzenleme. Boş bırakılan eşleştirme alanı "hepsi" demektir.

**Prim detayı** — personelin dönem primi ve hesabın her adımı. Prim dışı kalan satışlar da
nedeniyle birlikte listelenir; hiçbir kayıt sessizce düşmez.

## Öne çıkan kararlar

Ayrıntısı [docs/adr](docs/adr) altında, özeti:

- **Gecelik batch ETL.** Prim ay sonunda kesinleşiyor; gerçek zamanlı entegrasyonun maliyeti
  karşılığını vermiyor. Gün içi tetikleme yine mümkün.
- **ERP prim tabanı değil, doğrulama katmanı.** ERP'nin `Reference` alanı PMS belgelerine işaret
  ediyor ama içerik tutmuyor (82 satırın 82'sinde tutar/personel farklı). Güvenilir bir join
  anahtarı olmadan ERP'yi tabana katmak çift sayım riski taşıyor.
- **Kural = veri, kural tipi = kod.** Üç tip var: sabit yüzde, kademeli barem, sabit tutar.
  Yeni kural eklemek bir veritabanı satırı; yeni bir *tip* eklemek yeni bir strateji sınıfı.
- **Tarihte katı, tutarda toleranslı ayrıştırma.** `32/08/2026` reddedilir — yanlış tahmin primi
  yanlış aya yazar. `2.500,00` kabul edilir — belirsizlik yok, reddetmek gerçek ciroyu kaybettirir.
- **Denetim kaydı ve dönem kilidi `SaveChanges` içinde.** Servis katmanında değil; hangi yoldan
  gelinirse gelinsin kural ve satış değişiklikleri loglanır, kapalı dönem korunur.
- **Audit log veritabanında, uygulama logu stdout'ta.** Audit log iş kaydı, aynı transaction'da
  yazılır; Elastic'e taşınmaz. Uygulama logu Serilog ile JSON olarak stdout'a düşer, log
  toplayıcı Elastic'e taşır. Her istek `X-Correlation-Id` taşır.

## Yapı

Katmanlı mimari. İstek şu sırayla ilerler:

```
Controller  →  Service  →  Repository  →  Entity
     ↑            ↓
    DTO         Domain
```

```
src/
  Gloria.Commission.Api               Controllers, rol okuma, hata formatı
  Gloria.Commission.Application       Services, Repositories (arayüz), Dtos, Mappers, Rules
  Gloria.Commission.Infrastructure    Repositories (EF Core), Persistence, Import
  Gloria.Commission.Domain            Entities, Enums
tests/                                birim testleri
web/                                  React (Vite)
sample-data/                          verilen CSV'ler
```

Kurallar:

- **Controller** yalnızca HTTP işi yapar: bağlama, doğrulama, durum kodu. İş mantığı yok.
- **Service** iş mantığını taşır, yetkiyi denetler, DTO ↔ Entity dönüşümünü `Mappers` üzerinden yapar.
- **Repository** veri erişimini kapsar. Arayüzü Application'da, EF Core implementasyonu
  Infrastructure'da — servis katmanı ORM'i bilmez.
- **Kural motoru** saf: veritabanına dokunmaz, verilen girdilerden deterministik çıktı üretir.
  Testlerin hızlı ve hesabın yeniden üretilebilir olmasının sebebi bu.

Yazma işlemleri `IUnitOfWork` ile kalıcılaşır; böylece bir servis metodu birden fazla tabloya
tek işlemde yazabilir (aktarım: parti + satışlar + hatalı satırlar).
