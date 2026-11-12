# Doğrulama — 12 Kasım 2026

## Güncel oyuncu ve arayüz teslimi

- Siyah-altın HUD ve oda/maç sonu menüleri; Türkçe Cinzel/Manrope yazı tipleri, özel hedef halkası ve güç çubuğu tamamlandı. Şutçunun kolları aşağıda bekleme/nefes döngüsü, kalecinin önde hazır eldivenleri, forma ayrıntıları ve şutçunun bilek birleşimi güncellendi.
- Yeni paket öncesinde 28 oyun kuralı, 1.238 hareket, 70 sahne ve 65 arayüz kontrolü geçti. Arayüz kontrolleri Türkçe karakterleri, 16:9/4:3/geniş ekran/güvenli alan koordinatlarını ve kamera açısını kapsar.
- macOS development ve Android IL2CPP ARM64 derlemeleri başarılı. APK: `Builds/PenaltiArena-dev.apk`, 83.629.845 byte (79,8 MiB).
- APK SHA-256: `6e43570d14ab0df7dabdf71592c114cbbb21a06fb444a242313689e8b09ecc3d`.
- Derlenmiş uygulamadan GPU skinning ve post processing ile dokuz görüntü alındı: ana menü, şutçu, kaleci, kurtarış, sonuç, ayarlar, maç sonu, 4:3 ve geniş ekran. `PRESENTATION_UI_CAPTURE_OK`; `Docs/presentation-ui/`. Bu görüntülerdeki 2:1 skor örnek veridir ve **ÖNİZLEME** etiketi taşır; oynanmış maç sonucu değildir. Görüntülerde bilek birleşimi ve yukarı kaydırma oku son kez kontrol edildi. Çalışma günlüğünde exception yok.
- Son derlemede `python3 Tools/run_network_smoke.py --relay --motion` geçti. İki ayrı uygulama gerçek Relay üzerinden `score=1:0 history=GKKKK/DDDDD winner=0 shots=10` sonucunu aldı; iki süreç exit code 0. İkisinde de `MOTION_PHYSICS_OK` doğrulandı ve exception kaydı yok. Bu test iki fiziksel tablette yapılmadı.
- Çevre geometrisi 157.452 üçgen, 49 mesh renderer; karakterlerle toplam 65 renderer. Önceki KCISA uyarlamasında çevre yaklaşık 1,19 milyon üçgendi. Bunlar tablet FPS ölçümü değildir.
- Yeni sahnenin kaynakları ve yeniden oluşturma adımları: [art-direction.md](art-direction.md). Ham Asset Store varlıkları ve paketlere bağlı türevler public Git dışında tutulur.

## Geçen kontroller

- Unity 6000.6.8f1 / URP 17.6.0 / NGO 2.14.0 / Multiplayer Services 2.4.0 ile C# derlemesi.
- MatchRules: 28 kontrol geçti (`Docs/rule-test-results.txt`): beşer atış, karşılıklı ek atış, rol/sıra/evre doğrulaması ve tek sonuç.
- KCISA paketinden 478 kaynak model/materyal/renk dokusu seçildi (yaklaşık 697 MiB). Tam 5.6 GB paket taşınmadı. İçe aktarma aracının tekrar çalışması 0 dosya ekledi; mevcut materyaller ve metadata korundu.
- `ArenaStadium.unity`: KCISA tribün/iç yapı, URP materyalleri, futbol çimi/çizgileri, Super Goalie kaleci ve açık formalı geçici şutçu, Blender kale.
- 3.442 koltuk sadeleştirilip 45 mekânsal mesh grubuna birleştirildi. Dokuların import üst sınırı 1024 px.
- Şutçu, kaleci ve genel görünüm gerçek Unity render çıktıları görüntülenerek incelendi. Kaleci için file gizlenir; fizik collider'ları korunur. Şutçunun ayak hizası ve topun görünürlüğü düzeltildi.
- İki kamera açısından sağ/sol kaydırmanın gerçek ekran projeksiyonu test edildi; önceki ters yön hatası düzeltildi. File konumu/görünürlüğü, değişmeyen collider sayısı, 11 m penaltı noktası, karakter ayak hizası ve materyal referansları kontrolleri geçti.
- Super Goalie humanoid avatar ve 10 kaleci Animator durumu doğrulandı. Sağ kurtarış pozları sol kliplerden aynalanır. Idle/LeftMiddle/RightMiddle render kontrolleri yapıldı.
- Android IL2CPP ARM64 ve macOS development build başarılı. Önceki animasyon tesliminin APK boyutu: `Builds/PenaltiArena-dev.apk`, 103.720.733 byte (yaklaşık 99 MiB). APK içindeki IL2CPP metadata’da yeni CharacterMotion, ReadJson ve UTF-8 uzunluk doğrulaması ayrıca kontrol edildi.
- Blender 5.1.2 ile özgün Goal.blend ve Goal.fbx üretimi başarılı; stadyum sahnesinde kullanılıyor.

## Şut ve kurtarış doğrulaması

- `ArenaAnimated.unity`: 2,4 saniyelik şut; temas 0,58 saniye. Gerçek kemik ölçümünde ayak ucu ile topun arka yüzeyi arasında 1 cm, ayakkabı tabanı ile zemin arasında yaklaşık 1 mm var.
- Dokuz kurtarış; uzanma, iniş, kalkış ve merkeze dönüş. Sağ/sol el erişimi 2,9 m. Eski sabit collider’lar kaldırıldı; 9 kapsül ve 3 küre kemiklerden güncellenir.
- `MotionChecks`: güncel duruşlarla 1.238 kontrol geçti. Tüm kliplerde 50 ms aralıklarla geçerli poz ve zemin kontrolü; el erişimi, collider hizası, şut teması ve başlangıca dönüş kontrolü.
- İki development player içinde dokuz el pozu gerçek PhysX çarpışmasıyla test edildi; ters taraftaki şutun geçmesi de doğrulandı (`MOTION_PHYSICS_OK`).
- Şut, alçak/orta/yüksek uzanmalar ve toparlanma Unity kamera karelerinde görsel olarak incelendi. Yerel video: `Docs/motion-preview.mp4` (7 saniye). Bu video hareket önizlemesidir; önizleme topunun yörüngesi sunum amaçlıdır. Fizik sonuçları oyuncu uygulamalarındaki ayrı testlerden gelir.
- Şut tekrar komutu Windup sırasında reddedilir; kaleci bu sırada hareket edebilir. İstemci hareket başlangıcını ortak ağ saatinden okur ve kendi kaydırmasını kısa süre yerel tahminle gösterir.
- Yeni zaman verileriyle ilk Relay denemesinde NGO’nun varsayılan UTF-16 mesajı MTU sınırını aştı. Mesajlar UTF-8 oldu, uzunluk denetimi eklendi; çok uzun ek-atış geçmişinde parçalı güvenilir gönderim kullanılır. İki cihaz aynı güncel sürümü kullanmalıdır.

## Gerçek Relay testi

Kullanıcının hesabında **Kaleci** Unity Cloud projesi oluşturuldu ve yerel projeye bağlandı. Kullanıcı hedef kitleyi **çocuklara yönelik** seçti. Reklam, Analytics, ek kimlik sağlayıcısı veya ücretli abonelik etkinleştirilmedi.

`python3 Tools/run_network_smoke.py --relay` ile aynı Mac üzerindeki iki ayrı development uygulaması, farklı anonim oyuncu profilleriyle gerçek Unity Relay üzerinden özel odaya bağlandı. Her atışta tekrar şut komutu da gönderildi. İki süreç aynı sonucu üretti:

```
score=5:0 history=GGGGG/DDDDD winner=0 shots=10
```

İki süreç de exit code 0 ile kapandı. Son sonuç yayımlandıktan sonra test oturumundan ayrıldı. Yerel günlükler `Logs/relay-host.log` ve `Logs/relay-client.log` içindedir ve Git dışındadır.

Animasyonlu son sürümde `python3 Tools/run_network_smoke.py --relay --motion` geçti. Kaleci şutun hazırlık hareketine tepki verir; test iki sağ ve iki sol kurtarış içerir. Host ve client aynı sonucu aldı:

```
score=1:0 history=GKKKK/DDDDD winner=0 shots=10
```

İki süreç exit code 0 ile kapandı. Her ikisinde dokuz el çarpışması ve ters taraf kontrolü geçti; Türkçe oyuncu adları bozulmadı. Son test günlüklerinde exception yok. Günlükler: `Logs/relay-motion-host.log`, `Logs/relay-motion-client.log`. İlk zamanlama denemesinde yalnız Flying mesajını bekleyen otomasyon bir köşe şutuna geç kaldı; test girdisi görünür hazırlık evresine taşındı, oyun fiziği değiştirilmedi.

Önceki animasyon tesliminin APK SHA-256 değeri: `3d5c214b633e1ace97052ae7aa5ba37f628c322eee938db81c60b68918eb2ee6`.

Bu güncel test loopback bağlantısı kullanmadı. Aynı bilgisayardan Relay'e bağlanan iki süreç, ayrı fiziksel tabletlerin ve ayrı internet hatlarının testi yerine geçmez. Önceki temel sürüm ayrıca UDP loopback testini geçmişti.

## Henüz doğrulanmayanlar ve kalan işler

- `adb devices` bağlı cihaz göstermedi. Android'de APK açılışı, dokunma, farklı ekran oranları, FPS ve ısınma ölçülmedi. 30/60 FPS hedefi cihaz performansı garantisi değildir.
- Ayrı internet ağlarındaki iki Android tablet.
- Gerçek üçüncü istemcinin reddi; ağ gecikmesi/paket kaybı, arka plana alma ve kopma senaryoları.
- İki istemciden rövanş onayı uçtan uca testi.
- Hareketlerin gerçek tablet ekranında ve yüksek ağ gecikmesinde hissi; nihai karakter/mocap ve sanat iyileştirmesi.
- Arayüzün gerçek cihaz dokunma/okunabilirlik testi, özel sesler, file tepkisi, seyirciler için gelişmiş LOD, baked lighting ve sinematik açılış.

Bu sürüm teknik prototiptir; referanslardaki premium kaliteye ulaşılmış veya yayınlanabilir sayılmaz. Üçüncü taraf paket kaynakları, türetilmiş ham varlıklar ve stadyum sahnesi public Git dışında tutulur; yeniden oluşturma araçları repodadır.
