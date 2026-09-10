# ADR-004: Kural veri, kural tipi kod

## Bağlam

Yarın golf dersi satışı prim kapsamına girdiğinde ya da SPA oranı %6'dan %7'ye çıktığında
sistemin kod yazılmadan adapte olması isteniyor.

## Karar

Kural tanımı tamamen veritabanında:

- **Eşleştirme** — kaynak sistem, departman, ürün grubu, ürün kodu, otel. Null bırakılan alan
  "hepsi" demek.
- **Hesaplama** — tip artı parametreleri (oran / sabit tutar / kademe listesi).
- **Geçerlilik** — öncelik, yürürlük aralığı, aktiflik.

Kural *tipleri* koddadır: `ICommissionRuleStrategy`'nin üç implementasyonu.

Bir satışa birden fazla kural uyarsa önce önceliğe, eşitlikte daha spesifik olana bakılır.

## Gerekçe

Sınır burada: yeni bir prim **kalemi** veri, yeni bir hesaplama **davranışı** kod.
Veritabanında script çalıştıran bir kural dili her ikisini de veriye çevirirdi ama
maaş etkileyen bir sistemde denetlenmesi ve test edilmesi çok daha zor olurdu.
Üç tip, case'in istediği senaryoların tamamını karşılıyor.

Spesifiklik kuralı, genel kuralı bozmadan istisna eklemeyi mümkün kılıyor:
"tüm SPA satışlarına %6" dururken "SPA_MSJ90'a %9" eklenebiliyor.

## Sonuçlar

Kural motoru saf bir fonksiyon — veritabanına dokunmuyor, girdiden deterministik çıktı üretiyor.
Birim testleri veritabanı kurulumu gerektirmiyor, hesap yeniden üretilebilir.

Motor her adımın gerekçesini de üretiyor ve bu adımlar `commission_result_lines` olarak saklanıyor.
Prim itirazı geldiğinde hesabı yeniden koşturmadan cevap verilebiliyor.

Kural silinmiyor, pasife alınıyor: geçmiş hesap adımları kurala referans veriyor.
