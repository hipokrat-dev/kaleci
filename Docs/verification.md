# Doğrulama — 12 Kasım 2026

## Geçen kontroller

- Unity 6000.6.8f1 / URP 17.6.0 / NGO 2.14.0 / Multiplayer Services 2.4.0 ile C# derlemesi.
- MatchRules: 25 kontrol geçti (`Docs/rule-test-results.txt`): beşer atış, karşılıklı ek atış, rol/sıra/evre doğrulaması ve tek sonuç.
- KCISA paketinden 478 kaynak model/materyal/renk dokusu seçildi (yaklaşık 697 MiB). Tam 5.6 GB paket taşınmadı. İçe aktarma aracının tekrar çalışması 0 dosya ekledi; mevcut materyaller ve metadata korundu.
- `ArenaStadium.unity`: KCISA tribün/iç yapı, URP materyalleri, futbol çimi/çizgileri, Super Goalie kaleci ve açık formalı geçici şutçu, Blender kale.
- 3.442 koltuk sadeleştirilip 45 mekânsal mesh grubuna birleştirildi. Dokuların import üst sınırı 1024 px.
- Şutçu, kaleci ve genel görünüm gerçek Unity render çıktıları görüntülenerek incelendi. Kaleci için file gizlenir; fizik collider'ları korunur. Şutçunun ayak hizası ve topun görünürlüğü düzeltildi.
- İki kamera açısından sağ/sol kaydırmanın gerçek ekran projeksiyonu test edildi; önceki ters yön hatası düzeltildi. File konumu/görünürlüğü, değişmeyen collider sayısı, 11 m penaltı noktası, karakter ayak hizası ve materyal referansları kontrolleri geçti.
- Super Goalie humanoid avatar ve 10 kaleci Animator durumu doğrulandı. Sağ kurtarış pozları sol kliplerden aynalanır. Idle/LeftMiddle/RightMiddle render kontrolleri yapıldı.
- Android IL2CPP ARM64 ve macOS development build başarılı. Güncel APK: `Builds/PenaltiArena-dev.apk`, 102.824.793 byte (yaklaşık 98 MiB).
- Blender 5.1.2 ile özgün Goal.blend ve Goal.fbx üretimi başarılı; stadyum sahnesinde kullanılıyor.

## Gerçek Relay testi

Kullanıcının hesabında **Kaleci** Unity Cloud projesi oluşturuldu ve yerel projeye bağlandı. Kullanıcı hedef kitleyi **çocuklara yönelik** seçti. Reklam, Analytics, ek kimlik sağlayıcısı veya ücretli abonelik etkinleştirilmedi.

`python3 Tools/run_network_smoke.py --relay` ile aynı Mac üzerindeki iki ayrı development uygulaması, farklı anonim oyuncu profilleriyle gerçek Unity Relay üzerinden özel odaya bağlandı. Her atışta tekrar şut komutu da gönderildi. İki süreç aynı sonucu üretti:

```
score=5:0 history=GGGGG/DDDDD winner=0 shots=10
```

İki süreç de exit code 0 ile kapandı. Son sonuç yayımlandıktan sonra test oturumundan ayrıldı. Yerel günlükler `Logs/relay-host.log` ve `Logs/relay-client.log` içindedir ve Git dışındadır.

Bu güncel test loopback bağlantısı kullanmadı. Aynı bilgisayardan Relay'e bağlanan iki süreç, ayrı fiziksel tabletlerin ve ayrı internet hatlarının testi yerine geçmez. Önceki temel sürüm ayrıca UDP loopback testini geçmişti.

## Henüz doğrulanmayanlar ve kalan işler

- `adb devices` bağlı cihaz göstermedi. Android'de APK açılışı, dokunma, farklı ekran oranları, FPS ve ısınma ölçülmedi. 30/60 FPS hedefi cihaz performansı garantisi değildir.
- Ayrı internet ağlarındaki iki Android tablet.
- Gerçek üçüncü istemcinin reddi; ağ gecikmesi/paket kaybı, arka plana alma ve kopma senaryoları.
- İki istemciden rövanş onayı uçtan uca testi.
- Gerçek şut animasyonu; kaleci animasyon/collider hizası ve iniş/ayağa kalkış. Şutçu geçici olarak aynı humanoid modelin bekleme klibini kullanır.
- Nihai arayüz/sesler, file tepkisi, seyirci, LOD/baked lighting, sinematik açılış.

Bu sürüm teknik prototiptir; referanslardaki premium kaliteye ulaşılmış veya yayınlanabilir sayılmaz. Üçüncü taraf paket kaynakları, türetilmiş ham varlıklar ve stadyum sahnesi public Git dışında tutulur; yeniden oluşturma araçları repodadır.
