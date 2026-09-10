# ADR-007: Kural kapsamı serbest metin değil, yabancı anahtar

## Bağlam

Prim kuralı hangi satışlara uygulanacağını beş alanla belirliyor: kaynak sistem, departman,
ürün grubu, ürün kodu, otel. Bunlar başlangıçta serbest metindi.

Serbest metnin sorunu şu: departman alanına `SPA ` (sonda boşluk) yazan bir kural kaydedilir,
hata vermez, hiçbir satışla eşleşmez ve **sessizce sıfır prim** üretir. Maaş etkileyen bir
sistemde fark edilmesi aylar sürebilir.

Kod incelenirken ikinci bir sorun çıktı: otel alanı satış kaydının oteliyle karşılaştırılıyordu.
Kaynak dosyalarda otel bilgisi yalnızca PMS'te var; POS ve ERP'de yok. Yani otel kapsamlı bir
kural POS satışlarıyla hiçbir zaman eşleşmiyordu. Canlı doğrulandı: aynı kural PMS'te 11 satırı
eşleştirirken POS'ta 0 satır üretti.

## Karar

Departman, otel ve ürün grubu birer tablo; kural bunlara yabancı anahtarla bağlanıyor.
Otel eşleştirmesi satışın değil **personelin** oteline bakıyor.

Ürün kodu serbest metin kaldı: üç kaynak sistem farklı şekillendiriyor (`SPA_MSJ60`,
`SPA01-5001`, ERP açıklamasından türetilen) ve gelen satış verisi bizim kontrolümüzde değil.
Oraya bütünlük dayatmak, kaynak yeni bir kod gönderdiğinde aktarımı durdururdu.

## Gerekçe

Otelin personele ait olduğu veriyle doğrulandı: 183 PMS satırının hiçbirinde personelin oteli
ile kaydın oteli farklı değil ve her personel tek otelde çalışıyor.

Veritabanı yabancı anahtarı geçersiz kimliği zaten reddediyor, ama oradan gelen hata istemciye
"beklenmeyen hata" olarak yansıyor. Bu yüzden servis katmanı da kontrol ediyor ve anlaşılır
bir mesaj dönüyor.

## Sonuçlar

Arayüzde bu alanlar artık serbest metin kutusu değil, açılır liste. Yazım hatası mümkün değil.

Ayrıştırıcı veritabanına erişemediği için ürün grubu kodunu kimliğe çevirme işi
`ImportService`'e taşındı. Katalogda olmayan bir grup gelirse satır reddedilmiyor,
`DIGER` grubuna düşüyor.

Kullanılmayan `products` tablosu kaldırıldı: tanımlıydı ama hiçbir yerde okunmuyor,
yazılmıyordu; ürün grubu eşlemesini `ProductCatalog` yapıyor.
