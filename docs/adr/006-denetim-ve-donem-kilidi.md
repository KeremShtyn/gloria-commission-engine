# ADR-006: Denetim kaydı ve dönem kilidi veri erişim katmanında

**Durum:** Kabul edildi · 2026-09-10

## Bağlam

Sistem maaş etkiliyor. Asıl risk dışarıdan saldırgan değil; yetkili bir kullanıcının geriye
dönük olarak bir satışın tutarını ya da bir kuralın oranını değiştirmesi.

## Karar

İkisi de EF Core interceptor'ı olarak `SaveChanges` içinde çalışıyor:

- **Denetim kaydı** — kural, kademe, satış ve dönem değişikliklerinde kim, ne zaman, hangi
  alanı hangi değerden hangi değere çevirdi bilgisi JSON olarak yazılıyor.
- **Dönem kilidi** — kapalı döneme ait satış ve prim kayıtlarına yapılan her yazma reddediliyor.

## Gerekçe

Servis katmanında olsaydı yeni bir kod yolu eklendiğinde loglamayı unutmak mümkün olurdu.
Veri erişim katmanında olduğunda hangi endpoint'ten, hangi servisten gelinirse gelinsin kural geçerli.

Dönem kilidini de aynı yere koymanın sebebi aynı: kilidi kontrol etmeyi unutan yeni bir servis
metodu yazılamıyor.

## Sonuçlar

Denetim kaydı işlemle aynı transaction'da yazılıyor — kayıt başarılıysa log da var, değilse ikisi de yok.

Yetki kontrolü ayrı bir katmanda (servis) duruyor; endpoint'e güvenilmiyor.
Rol şu an HTTP header'ından okunuyor, üretimde `ICurrentUser`'ın JWT okuyan bir implementasyonuyla
değiştirilecek. Header okunamazsa en dar yetki uygulanıyor.

Dönemi yeniden açmak mümkün ama Admin yetkisi istiyor ve kendisi de denetim kaydına düşüyor —
kapalı dönemin açılması denetlenmesi gereken bir olay.

## Elenen alternatif

**Veritabanı trigger'ı.** Uygulamadan bağımsız çalışır, daha güçlü bir garanti verirdi.
Ama SQLite ile SQL Server arasında taşınabilir değil ve testlerde kurulumu zor.
Uygulama tek yazar olduğu sürece interceptor yeterli.
