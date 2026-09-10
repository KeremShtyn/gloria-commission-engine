# ADR-002: Gerçek zamanlı entegrasyon yerine gecelik batch

**Durum:** Kabul edildi · 2026-09-10

## Bağlam

Satış verisi Fidelio (PMS), Flyby (POS) ve Oracle JDE (ERP) üzerinde ayrı ayrı oluşuyor.
Case ikisini de tartışmamızı istiyor: anlık API entegrasyonu mu, gecelik batch mi.

## Karar

Gecelik batch. Aktarım idempotent bir endpoint üzerinden yapılıyor, gün içinde elle de tetiklenebiliyor.

## Gerekçe

Prim ay sonunda kesinleşiyor. Bir satışın prime yansımasının 12 saat gecikmesinin operasyonel
karşılığı yok. Buna karşılık gerçek zamanlı entegrasyon üç kaynak sistemin de webhook ya da CDC
yeteneği olmasını, her satırın anında mutabakatını ve kaynak sistem kesintilerinde kuyruk
yönetimini gerektirirdi.

Batch'in ikinci faydası: hesabın girdisi sabit bir küme. Ay sonu hesabı yeniden koşturulduğunda
aynı sonuç çıkıyor. Sürekli akan veride bunu garanti etmek için ayrıca sürüm/kesit yönetimi gerekirdi.

## Sonuçlar

Personel primini gün içinde anlık göremiyor; en son aktarımın kestiği yere kadar görüyor.
Bu kabul edilebilir çünkü ekranda son hesaplama zamanı gösteriliyor.

Kaynak sistemlerin şemasına bağımlı olmuyoruz; sözleşme dosya formatı. Fidelio sürüm
yükseltirse bizim tarafta ayrıştırıcı değişir, veritabanı bağlantısı kopmaz.
