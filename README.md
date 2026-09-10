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

37 test var. Kademeli barem ve iade senaryoları `TieredRuleTests.cs` ve `RefundTests.cs`
altında; denetim kaydı ve dönem kilidi `PersistenceTests.cs` altında (SQLite in-memory).

## Yetkilendirme

Gerçek kimlik doğrulama yok; rol HTTP header'ından okunuyor.

| Header | Değer |
|---|---|
| `X-User-Role` | `Admin`, `Accounting` veya `Employee` |
| `X-User-Id` | Denetim kaydının aktörü |
| `X-Employee-No` | Rol `Employee` ise personelin kendi numarası |

Admin kuralları yönetir, Muhasebe tüm personelin primini görür, Personel yalnızca kendisininkini.
Header okunamazsa en dar yetki (`Employee`) uygulanır.

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
- **Denetim kaydı `SaveChanges` içinde.** Servis katmanında değil; hangi yoldan gelinirse gelinsin
  kural ve satış değişiklikleri loglanır.

## Yapı

```
src/
  Gloria.Commission.Domain           entity'ler, enum'lar
  Gloria.Commission.Application      kural motoru (veritabanından bağımsız), DTO'lar
  Gloria.Commission.Infrastructure   EF Core, CSV aktarımı, servisler
  Gloria.Commission.Api              endpoint'ler, rol okuma, hata formatı
tests/                               kural motoru birim testleri
web/                                 React (Vite)
sample-data/                         verilen CSV'ler
```

Kural motoru saf: veritabanına dokunmaz, verilen girdilerden deterministik çıktı üretir.
Testlerin hızlı ve hesabın yeniden üretilebilir olmasının sebebi bu.
