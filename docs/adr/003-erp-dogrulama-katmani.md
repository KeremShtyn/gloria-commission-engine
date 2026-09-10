# ADR-003: ERP prim tabanı değil, doğrulama katmanı

## Bağlam

Üç kaynak da satış verisi taşıyor. Soru şu: ERP kayıtları prim tabanına eklenmeli mi, yoksa
PMS ve POS'un muhasebe yansıması mı?

Verilen dosyada ERP'nin `Reference` alanı `PMS-180xxx` biçiminde ve bu belge numaraları PMS'te
gerçekten var. Ama içerik tutmuyor: 82 satırın 82'sinde referans verilen PMS kaydının tutarı,
personeli ve ürünü farklı.

Üç ölçüm daha:

- ERP'de tek bir POS ürünü yok. Yalnızca PMS ürün evrenini kapsıyor.
- Her ERP tutarı, ilgili PMS ürününün birim fiyatının tam katı (1x, 2x, 3x).
- 82 satırın 42'si (personel + ürün + tutar) olarak bir PMS satırıyla birebir aynı.

## Karar

Prim tabanı PMS + POS. ERP mutabakat ve doğrulama katmanı olarak kullanılıyor:

- `UNPOSTED` satırlar aktarılıyor ama prime esas sayılmıyor.
- `RM` (credit memo) satırları iade doğrulaması için `Reference` üzerinden eşleştiriliyor.
- Mutabakat raporu operasyonel ciro ile muhasebeleşmiş ciroyu ürün grubu bazında karşılaştırıyor.

## Gerekçe

ERP aynı ticari olayların yeniden kaydı; bağımsız bir gelir kaynağı değil. Tabana eklenirse
PMS'in 808.300 TL'sinin üzerine 533.200 TL daha binerdi. Bunu dedupe etmek de mümkün değil,
çünkü tek join anahtarı olan `Reference` güvenilmez. Güvenilir anahtar yokken en güvenli
davranış tabana katmamak.

## Sonuçlar

Karar koda gömülmedi. `SourceSystem` kural motorunda bir eşleştirme boyutu; ERP'yi tabana katmak
için bir kural satırı eklemek yeterli, derleme gerekmiyor. Değerlendirici veya muhasebe aksini
isterse karar tersine çevrilebilir.

ERP'ye kural tanımlı olmadığı için ERP satırları hesapta "kural tanımlı değil" gerekçesiyle
listeleniyor. Sessizce düşmüyorlar; ekranda görünüyorlar.

## Not

Veri sentetik. Referans uyumsuzluğu kasıtlı bir tuzak da olabilir, üretici hatası da.
Her iki durumda da güvenli tasarım aynı.
