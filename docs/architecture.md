# Mimari

Bu doküman hedef mimariyi anlatır. Repodaki kod bunun çalışan bir dilimidir:
tek servis, SQLite, üç kural tipi. Ölçeklenme notları en sonda.

## Genel akış

```
Fidelio (PMS) ──┐
Flyby   (POS) ──┼─→  Staging  ─→  Ayrıştırma  ─→  sale_records  ─→  Kural motoru  ─→  Prim
Oracle  (ERP) ──┘    (ham CSV)     + doğrulama        │                                  │
                                        │             └──→ ERP mutabakatı            audit_logs
                                        └──→ import_errors
```

Üç kaynak da aynı `sale_records` tablosuna normalize edilir: kaynak sistem, belge no, tarih,
personel, ürün, adet, tutar, para birimi, durum. Kural motoru kaynak sistemi bilmez —
sadece bir alan olarak görür, kural yazarken filtre olarak kullanılabilir.

## Veri entegrasyonu

**Gecelik batch, gün içi tetiklemeye açık.** Prim ay sonunda kesinleşiyor; anlık doğruluk
gereksinimi yok. Gerçek zamanlı entegrasyon üç kaynak sistemin de webhook/CDC yeteneği olmasını
ve her satırın anında mutabakatını gerektirirdi — maliyeti karşılığını vermiyor.
Muhasebe gün içinde kontrol etmek isterse aynı import endpoint'i elle çağrılabilir.

Fidelio ve JDE'ye doğrudan veritabanı bağlantısı açmak yerine dosya/servis sınırı korunur:
kaynak sistemlerin şeması bizim sorumluluğumuz değil, sözleşme dosya formatı.

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
  eşleştirme:  kaynak sistem, departman, ürün grubu, ürün kodu, otel   (null = hepsi)
  hesaplama:   tip + oran / sabit tutar / kademeler
  geçerlilik:  öncelik, yürürlük başlangıcı, bitişi, aktif mi
commission_rule_tiers
  kademe:      alt sınır, üst sınır, oran
```

Bir satışa birden fazla kural uyarsa önce **önceliğe**, eşitlikte **daha spesifik olana** bakılır.
Böylece "tüm SPA satışlarına %6" kuralının üzerine "SPA_MSJ90'a %9" kuralı eklemek için
öncekine dokunmak gerekmez.

Kural **tipleri** koddadır — üç strateji sınıfı: sabit yüzde, kademeli barem, sabit tutar.
Yeni bir kalem eklemek veri işi; dördüncü bir hesaplama *davranışı* gerekirse
`ICommissionRuleStrategy`'nin yeni bir implementasyonu yazılır. Buradaki sınır bilinçli:
veritabanında script çalıştıran bir DSL, denetlenmesi ve test edilmesi çok daha zor bir sistem olurdu.

Motor saf bir fonksiyondur: personel, satışlar ve kurallar girer; sonuç ve **her adımın gerekçesi**
çıkar. Hesap veritabanına `commission_result_lines` olarak yazılır — hangi satış, hangi kural,
hangi oran, ara toplam. Prim itirazı geldiğinde hesap yeniden koşturulmadan cevaplanabilir.

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
değiştirilemez. Kontrol yine interceptor'da — hangi endpoint'ten gelinirse gelinsin geçerli.
Dönemi yeniden açmak Admin yetkisi ister ve denetim kaydına düşer.

**Kural silinmez, pasife alınır.** Geçmiş hesap adımları kurala referans veriyor;
fiziksel silme izlenebilirliği bozar.

**Roller.** Admin kuralları yönetir, Muhasebe tüm personeli görür, Personel yalnızca kendisini.
Yetki kontrolü servis katmanında; endpoint'e güvenilmez. Şu an rol HTTP header'ından okunuyor,
üretimde bu sınıf JWT claim'lerini okuyan bir implementasyonla değiştirilir — çağıran kod aynı kalır.

**Para birimi.** Yabancı para satışlar TRY'ye çevrilerek saklanır, kullanılan kur satırda tutulur.
Kuru bulunamayan satır sessizce 1 kabul edilmez; hatalı satır olarak loglanır.

## Ölçeklenme

Bu case tek servis olarak teslim edildi. Gerçek kurulumda değişecekler:

- **Veritabanı:** SQLite yerine SQL Server. Şema ve indeksler aynı; EF Core sağlayıcısı değişir.
- **Aktarım:** Import endpoint'i senkron. Dosyalar büyüdüğünde kuyruğa alınıp arka planda
  işlenmesi, ilerleme bilgisinin `import_batches` üzerinden okunması gerekir.
- **Zamanlama:** Gecelik ETL'i tetikleyecek bir job (Hangfire veya harici scheduler).
- **Gözlemlenebilirlik:** Yapılandırılmış log + korelasyon kimliği; aktarım ve hesaplama
  sürelerinin metriğe dönmesi.

Bunları şimdi eklemek teslim edilen dilimi karmaşıklaştırırdı; sınırı burada çizdim.
