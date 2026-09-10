# ADR-005: Tarihte katı, tutarda toleranslı ayrıştırma

**Durum:** Kabul edildi · 2026-09-10

## Bağlam

Kaynak dosyalar kirli. Verilen ay içinde karşılaşılanlar:

| Sorun | Örnek |
|---|---|
| Bozuk tarih | `32/08/2026`, `08.15.2026` |
| Farklı sayı formatı | `2.500,00` — diğer satırlar `2500.00` |
| Sayı olmayan tutar | `abc` |
| Kayıtsız personel | `P9999` |
| Desteklenmeyen belge tipi | `DocType = XX` |
| Mükerrer satır | Aynı belge numarası iki kez |

Her biri için iki seçenek var: reddedip hata tablosuna yazmak, ya da tahmin edip kurtarmak.

## Karar

Ayrımı belirsizlik belirliyor.

**Tarih:** yalnızca `yyyy-MM-dd`. `32/08/2026` zaten geçersiz. `08.15.2026` teknik olarak
MM.dd.yyyy'den kurtarılabilir ama bunu kabul etmek `05.03.2026`'nın 5 Mart mı 3 Mayıs mı
olduğu sorusunu açar. Yanlış tahmin primi yanlış aya yazar ve fark ancak dönem kapandıktan
sonra ortaya çıkar. Reddediliyor.

**Tutar:** hem `2500.00` hem `2.500,00` okunuyor. Burada belirsizlik yok — son ayraçtan sonra
iki hane varsa ondalık, üç hane varsa binlik. Bu satırı reddetmek gerçek ciroyu kaybettirir
ve personelin primini eksiltir.

Reddedilen satırlar dosyayı geri çevirmiyor; ham hâliyle, hata kodu ve satır numarasıyla
`import_errors` tablosuna yazılıyor.

## Sonuçlar

Verilen üç dosyadaki 501 satırdan 469'u aktarılıyor, 23'ü mükerrer olduğu için yazılmıyor,
9'u hata tablosuna düşüyor. Hatalı satırlar kaynakta düzeltilip yeniden yüklenebiliyor; mükerrer kayıt
kontrolü ikinci yüklemede aynı satırların tekrar yazılmasını engelliyor.

Mutabakat raporu hata kodlarını sayı olarak gösteriyor — kirliliğin hangi kaynakta biriktiği
görünür kalıyor.
