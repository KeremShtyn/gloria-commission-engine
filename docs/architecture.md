# Mimari

Bu doküman hedef mimariyi anlatır. Repodaki kod bunun çalışan bir dilimidir:
tek servis, SQLite, üç kural tipi. Ölçeklenme notları en sonda.

## Genel akış

```
Fidelio (PMS) ──┐   otomatik    ┌──────────────┐    ┌──────────────┐   ┌──────────────┐
Flyby   (POS) ──┼──  ekstre  ──→│ staging_rows │──→ │ sale_records │──→│ Kural motoru │──→ Prim
Oracle  (ERP) ──┘   (dosya)     │  (ham satır) │    └──────────────┘   └──────────────┘
                                └──────┬───────┘           │
                                       │                   └──→ ERP mutabakatı
                                       └──→ import_errors
```

Ekstreyi kaynak sistem otomatik üretir; insan hiçbir adımda yok. Bugünkü manuel Excel
süreci ortadan kalkar — zaten çözülmesi istenen problem oydu.

Üç kaynak da aynı `sale_records` tablosuna normalize edilir: kaynak sistem, belge no, tarih,
personel, ürün, adet, tutar, para birimi, durum. Kural motoru kaynak sistemi bilmez —
sadece bir alan olarak görür, kural yazarken filtre olarak kullanılabilir.

## Veri entegrasyonu

**Gecelik batch, gün içi tetiklemeye açık.** Prim ay sonunda kesinleşiyor; anlık doğruluk
gereksinimi yok. Gerçek zamanlı entegrasyon üç kaynak sistemin de webhook/CDC yeteneği olmasını
ve her satırın anında mutabakatını gerektirirdi — maliyeti karşılığını vermiyor.

Uygulanmış hali: `ImportWatcherService` belirlenen klasörü tarar, bulduğu ekstreyi dosya adının
ön ekinden (`pms_`, `pos_`, `erp_`) tanır ve manuel yüklemeyle **aynı** import servisine verir.
İşlenen dosya arşive, işlenemeyen ayrı klasöre taşınır. Muhasebe gün içinde kontrol etmek isterse
aynı servisi HTTP üzerinden de çağırabilir — aktarım mantığı tek yerde.

Arka planda HTTP isteği olmadığı için iş, denetim kayıtlarında `system` aktörüyle ve Muhasebe
yetkisiyle çalışır: zamanlanmış aktarım, muhasebenin elle yaptığı işin gözetimsiz halidir,
kural değiştirme yetkisine ihtiyacı yoktur.

**Kaynak sistemlerin içine uzanılmaz.** Fidelio'nun ya da JDE'nin veritabanına doğrudan
bağlanmak, onların iç şemasına bağımlı olmak demektir; satıcı bir güncellemede kolon adını
değiştirdiğinde prim sistemi çöker. Bunun yerine kaynak sistemin ürettiği dosya ya da açtığı
API tüketilir. Bağımlılık iç yapıya değil sözleşmeye kurulur; ayrıca canlı bir otel sistemine
dışarıdan sorgu atma riski ve o sistemlerin veritabanı şifresini taşıma yükü ortadan kalkar.

**Ham veri saklanır.** Gelen her satır, hiçbir dönüşüm uygulanmadan `staging_rows` tablosuna
yazılır; ayrıştırma ondan sonra çalışır. Sebebi: ayrıştırıcıda bir hata olursa satır sessizce
yanlış kaydedilir ve karşılaştırılacak orijinal kalmaz. Maaş etkileyen bir sistemde "kaynak
tam olarak bunu gönderdi" diyebilmek gerekir. Ayrıca ayrıştırıcı düzeltildiğinde aynı parti
kaynağa dönmeden yeniden işlenebilir.

**Mükerrer kayıt.** Kaynak sistemdeki belge numarası doğal anahtar. `SHA256(kaynak|belgeNo)`
üzerinde unique index var; aynı dosya iki kez yüklense de satır tekrar yazılmaz. Aynı dosyada
aynı belge birden fazla kez geçerse muhasebeleşmiş (POSTED) satır tercih edilir.

**Hatalı satır.** Ayrıştırılamayan satır dosyayı reddettirmez; ham hâliyle `import_errors`
tablosuna, hata kodu ve satır numarasıyla yazılır. Böylece hem mutabakat açığı görünür kalır
hem de düzeltilip yeniden yüklenebilir.

**İadeler.** PMS'te ters kaydın orijinaline bağlanacak bir alanı yok. Aktarım sırasında
(personel + ürün + mutlak tutar + tarih önceliği) ile eşleştirilir; ERP'de `Reference` alanı
kullanılır. Eşleşen çift prim tabanından birlikte çıkar. Eşleşemeyen iade yine de negatif
tutarla düşülür — orijinali önceki döneme ait iadelerin cari aya mahsup edilme yolu budur.

## Kural motoru

Kural bir veri satırıdır, kod değil:

```
commission_rules
  eşleştirme:  kaynak sistem, departman*, ürün grubu*, otel*, ürün kodu   (null = hepsi)
  hesaplama:   tip + oran / sabit tutar / kademeler
  geçerlilik:  öncelik, yürürlük başlangıcı, bitişi, aktif mi
commission_rule_tiers
  kademe:      alt sınır, üst sınır, oran

* yıldızlı alanlar yabancı anahtar — bkz. ADR-007
```

Departman, ürün grubu ve otel serbest metin değil, referans tablolara bağlı. Serbest metin
olsalardı bir yazım hatası kuralın hiçbir satışla eşleşmemesine ve sessizce sıfır prim
üretmesine yol açardı. Otel eşleştirmesi personelin oteline bakar: kaynak dosyalarda otel
bilgisi yalnızca PMS'te bulunuyor, satış üzerinden eşleştirilseydi POS ve ERP satışları
hiçbir otel kuralıyla eşleşmezdi.

Bir satışa birden fazla kural uyarsa önce **önceliğe**, eşitlikte **daha spesifik olana** bakılır.
Böylece "tüm SPA satışlarına %6" kuralının üzerine "SPA_MSJ90'a %9" kuralı eklemek için
öncekine dokunmak gerekmez.

Kural **tipleri** koddadır — üç strateji sınıfı: sabit yüzde, kademeli barem, sabit tutar.
Yeni bir kalem eklemek veri işi; dördüncü bir hesaplama *davranışı* gerekirse
`ICommissionRuleStrategy`'nin yeni bir implementasyonu yazılır. Buradaki sınır bilinçli:
veritabanında script çalıştıran bir DSL, denetlenmesi ve test edilmesi çok daha zor bir sistem olurdu.

Motor saf bir fonksiyondur: personel, satışlar ve kurallar girer; sonuç ve **her adımın gerekçesi**
çıkar. Prim itirazı geldiğinde hesap yeniden koşturulmadan cevaplanabilsin diye adımlar
`commission_result_lines` olarak saklanır.

**Okuma ile yazma ayrıdır.** Prim ekranları hesabı canlı gösterir ve hiçbir şey yazmaz;
kalıcı kayıt `POST /commissions/{yıl}/{ay}/calculate` ile oluşur. Okuma ucu yazsaydı iki
kullanıcının aynı anda sayfayı açması aynı satırı yazmaya çalışır ve istek hata verirdi.
Kayıt işlemi de çakışabilir — bu durumda 409 döner, kullanıcıya tekrar denemesi söylenir.

Hiçbir kurala uymayan satış sessizce yutulmaz; sonuçta gerekçesiyle listelenir.
(Verilen veri setinde golf dersi satışı bu durumda — kuralı henüz tanımlı değil.)

## Güvenlik ve denetim

Sistem maaşı etkiliyor; tehdit modeli "dışarıdan saldırgan"dan çok **yetkili kullanıcının
geriye dönük düzeltmesi**.

**Denetim kaydı veri erişim katmanında üretilir.** `SaveChanges` içindeki bir interceptor
kural, kademe, satış ve dönem değişikliklerini yakalar; kim, ne zaman, hangi alanı hangi
değerden hangi değere çevirdi bilgisini JSON olarak yazar. Servis katmanında olsaydı yeni bir
kod yolu eklendiğinde loglama unutulabilirdi.

**Dönem kilidi.** Hesabı biten dönem kapatılır; kapalı dönemin satış ve prim kayıtları
değiştirilemez. Kontrol yine interceptor'da — hangi servisten gelinirse gelinsin geçerli.
Dönemi yeniden açmak Admin yetkisi ister ve denetim kaydına düşer.

**Kural silinmez, pasife alınır.** Geçmiş hesap adımları kurala referans veriyor;
fiziksel silme izlenebilirliği bozar.

**Kimlik ve yetki ayrı tutulur.** Kimlik nereden geldiği bir detaydır: şu an HTTP header'ından
okunuyor, üretimde JWT'den okunacak. Yetkilendirme ise buna bakmaksızın ASP.NET'in kendi
altyapısında — `[Authorize]` nitelikleri ve policy'ler. Kimlik kaynağını değiştirmek tek bir
satırı (`AddAuthentication`) değiştirmek demek; controller'lar, policy'ler ve servisler aynı kalır.

Admin kuralları yönetir, Muhasebe tüm personeli görür, Personel yalnızca kendisini. Sonuncusu
bir rol kontrolü değil, kaynak farkındalığı olan bir policy: hangi personelin sorgulandığı
rota değerinde. Aynı kontrol servis katmanında da tekrarlanır — controller'a güvenilmez.

Kimlik yoksa 401, kimlik var ama yetki yetersizse 403 döner. Bu ayrım istemcinin
"tekrar giriş yap" ile "bu sana kapalı" arasında karar verebilmesi için gerekli.

**Para birimi.** Yabancı para satışlar TRY'ye çevrilerek saklanır, kullanılan kur satırda tutulur.
Kuru bulunamayan satır sessizce 1 kabul edilmez; hatalı satır olarak loglanır.

## Loglama

İki ayrı şey var ve karıştırılmamalı:

| | Audit log | Uygulama logu |
|---|---|---|
| Ne | İş kaydı: kim primi değiştirdi | Teknik iz: hangi istek ne kadar sürdü |
| Nerede | `audit_logs` tablosu, **aynı transaction** | stdout → log toplayıcı |
| Neden orada | Kayıt başarılıysa log da var. Arada kuyruk olsaydı "değişiklik oldu ama logu kayboldu" mümkün olurdu | Kaybolması tolere edilebilir |
| Ömür | Kalıcı; itiraz geldiğinde delil | Günlerle sınırlı |

Audit log'u Elastic'e taşımak bu yüzden bir iyileştirme değil, hata olurdu: prim itirazında
dayanak, silinebilir bir index değil FK bütünlüğü olan bir tablo olmalı.

Uygulama logu Serilog ile yazılır, üretimde satır başına bir compact JSON nesnesi olarak
**stdout'a** düşer. Uygulama hangi backend'e gittiğini bilmez; Filebeat ya da Fluent Bit
stdout'u okuyup Elastic'e taşır. Doğrudan Elastic sink kullanılmamasının sebebi bu:
uygulamanın log altyapısına açılışta bağımlı olmaması, log yazamadığında istek işleyememesi gerekmiyor.

**Korelasyon kimliği.** Her istek `X-Correlation-Id` taşır — istemci gönderirse o kullanılır,
yoksa üretilir. Kimlik o istek boyunca yazılan her log satırına, cevap başlığına ve hata
gövdesine gider. Kullanıcı "hata aldım" dediğinde tek kimlikle bütün iz bulunur.
Log satırlarındaki `UserId` alanı audit log'daki `ChangedBy` ile aynı değerdir; teknik izden
iş kaydına geçilebilir.

**Uygulama loguna ne yazılmaz.** Prim tutarı, ciro, personel adı. Sadece `employeeNo`,
kural kodu ve satır sayısı yazılır. Bu sistem maaş etkiliyor; hassas değerlerin log
toplayıcıda geniş bir kitlenin görebileceği yere düşmesi, audit log'un sağladığı erişim
kontrolünü anlamsız kılardı.

## Katmanlar

İstek tek yönde ilerler; bağımlılıklar içeri doğru akar.

```
Api            Controller      HTTP: bağlama, doğrulama, durum kodu. İş mantığı yok.
                   │
Application    Service         İş mantığı, yetki denetimi, işlem sınırı
                   │  ↕ Mapper (DTO ↔ Entity)
               Repository      arayüz — servis ORM'i bilmez
                   │
Infrastructure Repository      EF Core implementasyonu, interceptor'lar
                   │
Domain         Entity          iş nesneleri, davranışlarıyla
```

DTO'lar `Application/Dtos` altında `Requests` ve `Responses` olarak ayrı. Entity dışarı sızmaz:
controller ne entity görür ne de EF Core tipi. Bunun karşılığı elle yazılan mapper'lar —
kütüphane bağımlılığı getirmemek ve hangi alanın nereye gittiğinin okunur kalması için tercih edildi.

Kural motoru bu zincirin dışında durur: saf bir fonksiyondur, girdisini servisten alır.
Veritabanına dokunmadığı için testleri kurulum gerektirmez.

Yazma işlemleri `IUnitOfWork.SaveChangesAsync` ile kalıcılaşır. Denetim kaydı ve dönem kilidi
o çağrının içindeki interceptor'larda çalıştığı için, hangi servisten gelinirse gelinsin geçerli.

## Ölçeklenme

Bu case tek servis olarak teslim edildi. Gerçek kurulumda değişecekler:

- **Veritabanı:** SQLite yerine SQL Server. Entity'ler, EF konfigürasyonları ve indeksler
  olduğu gibi taşınır; migration'lar sağlayıcıya özgü olduğu için yeniden üretilir.
- **Aktarım:** Import endpoint'i senkron. Dosyalar büyüdüğünde kuyruğa alınıp arka planda
  işlenmesi, ilerleme bilgisinin `import_batches` üzerinden okunması gerekir.
- **Zamanlama:** Gecelik ETL'i tetikleyecek bir job (Hangfire veya harici scheduler).
- **Gözlemlenebilirlik:** Log toplayıcı (Filebeat → Elastic) ve aktarım/hesaplama sürelerinin
  metriğe dönmesi. Uygulama tarafı hazır; eksik olan altyapı.

Bunları şimdi eklemek teslim edilen dilimi karmaşıklaştırırdı; sınırı burada çizdim.

## Bilinen sınır

**Başarısız aktarım geçmişte hiç görünmez.** Aktarımın tamamı tek transaction: parti kaydı,
ham satırlar, satışlar ve iade eşleşmeleri birlikte yazılır, ortada bir hata çıkarsa hepsi
geri alınır. Yarım yazılmış kayıt kalmıyor — ama bu sefer de denemenin kendisi iz bırakmıyor.
Muhasebe "dosyayı yükledim, olmadı" dediğinde aktarım geçmişinde bakacak bir satır yok.

Kalıcı çözüm partiye `Status` (Running / Completed / Failed) ve hata mesajı alanı eklemek,
başarısızlık kaydını ayrı bir transaction'da yazmak: veri değişikliği geri alınır, denemenin
kaydı kalır. Aktarım geçmişi o zaman başarısız denemeleri de gerekçesiyle gösterir.
