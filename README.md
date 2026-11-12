# Kaleci — Penaltı Arena

İki Android tablet için internet üzerinden özel odalı 3D penaltı oyunu. Tek kişilik mod yoktur. Bu teslim **teknik prototiptir; premium görsel/animasyon teslimi değildir**.

## Açılış

1. Unity Hub → Add → bu repoyu klonladığın klasörü seç.
2. Unity **6000.6.8f1** ile aç. Paketlerin çözülmesini bekle.
3. İlk klonlamada `Assets/Penalty/Scenes/Arena.unity` sahnesini aç. Stadyum ve kaleci paketleri entegre edilmiş yerel kurulumda animasyon üretiminden sonra `ArenaAnimated.unity` kullanılır; yalnız kaleci paketi varsa `ArenaWithGoalie.unity` kullanılır.
4. Sahneyi yeniden üretmek için `Penalty Arena → 01 — Sahneyi yeniden oluştur`. Bu işlem yalnızca üretilen Arena sahnesini yeniler; sahnede elle yaptığın değişiklikleri önce farklı adla kaydet.

## İnternet üzerinden iki tablet

Yerel proje, kullanıcının hesabında oluşturulan **Kaleci** Unity Cloud projesine bağlandı. Kullanıcının seçimiyle hedef kitle **çocuklara yönelik** olarak işaretlendi. Reklam, Analytics, ek kimlik sağlayıcısı veya ücretli abonelik etkinleştirilmedi.

1. Bu kurulumda Cloud bağlantısı hazır. Repoyu başka bir hesapla kullanıyorsan Unity Editor → Edit → Project Settings → Services üzerinden kendi Cloud projenle bağla.
2. Unity Dashboard'da Authentication (Anonymous) ve Multiplayer/Relay erişimini kontrol et. Bu çalışma ücretli servis veya abonelik etkinleştirmedi.
3. `Penalty Arena → 02 — Android APK oluştur` komutunu kullan. Çıktı: `Builds/PenaltiArena-dev.apk`.
4. APK'yı iki tablete kur. İlk cihazda oyuncu adını yazıp **Oda oluştur**; ikinci cihazda kodu yazıp **Katıl**.
5. İki oyuncu da **Hazırım** demeden maç başlamaz. Host kolay modu oda ekranında belirler; iki tarafa aynı top hızı uygulanır.
6. Her şutta roller değişir. Herkes beş şut çeker, eşitlikte karşılıklı ek atış yapılır. Rövanş iki onay ister.

Port yönlendirme gerekmez; internet yolu Multiplayer Services Session + Relay kullanır. Kimlik doğrulama anonimdir. Gizli anahtar kaynak koda yazılmadı. Bağlantı testinin kapsamı ve sonucu `Docs/verification.md` içinde kayıtlıdır.

## Kontroller

- Şutçu: Topun ekran üzerindeki yakınından başlayıp yukarı/çapraza kaydır ve bırak. Uzunluk güç, yön hedef; yüksek güç/kenara hedef dışarı çıkabilir. Hedef yalnızca kendi ekranındadır.
- Kaleci: Sağa/sola/çapraza kaydır; kısa dokunuş merkezde kalır. Editor'de ok tuşları; yukarı oku ile yüksek uzanma, boşluk merkez.
- Fare sürüklemesi dokunmayla aynı işi yapar.
- Ayarlar maçı durdurmaz. 30/60 FPS seçimi bulunur; cihaz performans garantisi değildir.

## Mimari

`RoomConnection`: NGO custom messages, MPS özel Session, max 2 oyuncu, Relay, anonim Authentication. Host kaynak kimliğini bağlantıdan çıkarır; komutta aktör kimliği gönderilmez.

`PenaltyGame`: host yetkili fizik ve skor, şut kimliği/evre/rol/sayısal girdi doğrulaması. Hedef yalnızca şut bırakıldığında gönderilir. İstemci 20 Hz snapshot alır ve görsel pozisyonları yumuşatır. Topun tamamının kale düzlemini geçmesi gol sayılır. Direk sonrası oyun süresi dolana dek sürer; ikinci vuruş reddedilir. Kaleci temasına rağmen içeri giren top gol olabilir.

`MatchRules`: beşer atış, eşit çift ek atış, tek sonuç, rol değişimi. `RuleChecks.Run` gerçek Unity Editor içinde 28 assertion çalıştırır.

`CharacterMotion` / `KeeperHitboxes`: ortak zaman çizelgesinde şut, dokuz kurtarış, toparlanma ve kemikleri izleyen çarpışma alanları. `MotionAuthoring` bu rig üzerinde 30 Hz yerel animasyon eğrileri üretir. Ayrıntılar: `Docs/animation.md`.

`ArenaBuilder`: tekrar üretilebilir mobil URP sahnesi ve Android build komutu. Animasyonlu sahne varsa onu tercih eder.

`Blender/build_goal.py`: metre ölçeğinde kale/file, UV, .blend ve .fbx üretimi.

## Yerel geliştirme testi

Editor ve development build menüsünde `Yerel host` / `Yerel katıl` bulunur. Aynı bilgisayarda IP `127.0.0.1`, aynı ağdaki cihazda host IP'si gerekir. Bu **internet odasının yerine geçmez**; bir kişilik mod veya kullanıcıya sunulan genel eşleştirme değildir.

Otomatik iki süreç testi: development macOS uygulamasına `-arena-smoke-host` ve ikinci örneğine `-arena-smoke-client` verilir. İki süreç hazır olur, 10 şut oynar, tekrar şut gönderimini dener ve aynı sonucu günlüğe yazar. Bu yapay zekâ oyun modu değildir.

## Mevcut eksikler

- Super Goalie karakter/top görselleri ve kaleci animasyonları ile KCISA stadyumunun seçilmiş parçaları entegre edildi. Sanat yönetimi hâlâ geliştirme aşamasında.
- Şutçu için yaklaşma, geriye salınım, ayak–top teması ve takip hareketi üretildi. Kalecinin dokuz kurtarışı iniş, ayağa kalkış ve merkeze dönüş içerir; el/gövde/uzuv collider’ları kemikleri izler. Karakter modelleri hâlâ aynı Super Goalie modelinin iki forma uyarlamasıdır; nihai karakter ve sanat kalitesi değildir.
- KCISA tribünleri ve iç yapısı URP materyalleriyle çim sahaya uyarlandı. 3.442 koltuk sadeleştirilip 45 bölgeye birleştirildi; dokular en fazla 1024 px. Son LOD, seyirci, baked lighting ve cihaz profili çalışmaları tamamlanmadı.
- Stadyum sahnesinde Blender kale modeli kullanılır. Birleşik file kaleci kamerasında gizlenir; fizik collider’ları korunur.
- Sinematik açılış videosu, özel sesler, file deformasyonu, replay ve son arayüz tasarımı eklenmedi.
- Gerçek cihazda dokunma, farklı en-boy oranları, 30/60 FPS ölçümleri yapılmadı.
- Animasyonlar ortak ağ saatiyle örneklenir; katılan kalecinin yerel hareketi hemen gösterilir. Host fizik doğrulamasında geriye sarma yoktur; gecikme hâlâ kurtarış zamanlamasını etkileyebilir.
- Host migration yok. Kopmada kazanan ilan edilmez; odadan çıkıp yeni bağlantı kurulur.
- Bu prototipte süreç arka plana alındığında bağlantı dayanıklılığı ve gelişmiş saldırı/rate-limit testleri tamamlanmadı.

Doğrulama kayıtları: `Docs/rule-test-results.txt`, `Logs/`. Asset kaynakları: `Docs/ASSETS.md`.

## GitHub ve yeniden kurulum

Ana kaynak deposu: https://github.com/hipokrat-dev/kaleci

Bu repo herkese açıktır. Asset Store paketlerinin model/doku/animasyon dosyaları, bu dosyalardan kopyalanan `.anim` dosyaları, paket kılavuzları ve yerel APK'lar Git'e dahil edilmez. Kaynak kod, bağımsız teknik sahne ve bu proje için üretilen Blender kale modeli dahil edilir.

### Super Goalie paketini geri yükleme

1. Kendi Unity hesabınla Super Goalie (Basic) paketini My Assets üzerinden indir.
2. Proje kökünden `python3 Tools/import_super_goalie.py "/paketin/tam/yolu/Super GoalieBasic.unitypackage"` çalıştır.
3. Unity'de `Assets → Refresh` seç. Gereken model/doku ve animasyonlar içe aktarılır; `ArenaWithGoalie.unity` sahnesi oluşturulur.
4. Gerekirse `Penalty Arena → 03 — Kaleci modelini entegre et` komutunu çalıştır.

İçe aktarma aracı mevcut farklı dosyaları ezmez. Paket indirme, lisans edinme veya hesap girişi yapmaz. `Arena.unity` temel sahnesi korunur. Kalecili sahne ve türetilen paket dosyaları yerelde kalır. Android build komutu isteğe bağlı paketin varlığına göre uygun sahneyi seçer. Her iki cihazda aynı sürümü kullan.

### KCISA stadyumunu geri yükleme

1. Kendi Unity hesabınla KCISA Olympic Park Handball Stadium paketini indir.
2. `python3 Tools/import_kcisa.py "/paketin/tam/yolu/kcisa - Olympic Park Handball Stadium.unitypackage"` çalıştır.
3. Unity'de `Assets → Refresh`, ardından gerekirse `Penalty Arena → 05 — KCISA stadyumunu entegre et` seç.
4. `ArenaStadium.unity` oluşur. Önceki sahneler korunur; açık sahnede kaydedilmemiş değişiklik varsa araç durur.

Araç yalnız seçilmiş modelleri, materyalleri ve renk dokularını getirir. Kaynak paketi değiştirmez. Mevcut farklı model/dokuları ezmez; Unity'nin güncellediği materyalleri ve `.meta` dosyalarını korur. Paket kaynakları, yerleşim verisi, türetilmiş materyal/animasyon/mesh ve stadyum sahnesi Git dışındadır.

### Ağ testi

macOS development build sonrasında `python3 Tools/run_network_smoke.py` iki süreçle loopback testi yapar. `python3 Tools/run_network_smoke.py --relay` bağlı Cloud projesinde geçici özel oda açar; iki ayrı anonim profil kullanır. İki süreçte de 10 atışın `5:0` / `GGGGG/DDDDD` sonucunu doğrular ve test oturumundan ayrılır. Günlükler Git dışındaki `Logs` klasöründedir. Bu testler fiziksel Android cihaz performansını ölçmez.

### Şut ve kurtarışları oluşturma

Paketler ve `ArenaStadium.unity` hazırken `Penalty Arena → 07 — Şut ve kurtarışları oluştur` komutunu çalıştır. `ArenaAnimated.unity` ve `Assets/Penalty/Art/Motion` içindeki klipler üretilir. Önceki sahneler kaynak olarak korunur. Animasyonlu sahnenin açık, kaydedilmemiş sürümü yeniden üretim sırasında `Art/Motion/BeforeRebuild.unity` kopyasına alınır. İki cihazda aynı yeni APK kullanılmalıdır; ağ mesajı biçimi UTF-8 olarak güncellenmiştir.

`python3 Tools/run_network_smoke.py --relay --motion` gerçek Relay üzerinde bir gol, dört animasyonlu kurtarış ve beş dışarı atışı doğrular. Ayrıca her iki uygulamada dokuz yön için ayrı fizik sahnesinde el–top çarpışmasını ve ters tarafa atlayışta topun geçmesini denetler. Bu test bir oyun modu değildir.
