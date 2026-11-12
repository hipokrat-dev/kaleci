# Kaleci — Penaltı Arena

İki Android tablet için internet üzerinden özel odalı 3D penaltı oyunu. Tek kişilik mod yoktur. Bu teslim **teknik prototiptir; premium görsel/animasyon teslimi değildir**.

## Açılış

1. Unity Hub → Add → bu repoyu klonladığın klasörü seç.
2. Unity **6000.6.8f1** ile aç. Paketlerin çözülmesini bekle.
3. İlk klonlamada `Assets/Penalty/Scenes/Arena.unity` sahnesini aç. Kaleci paketi entegre edilmiş yerel kurulumda `ArenaWithGoalie.unity` kullanılır.
4. Sahneyi yeniden üretmek için `Penalty Arena → 01 — Sahneyi yeniden oluştur`. Bu işlem yalnızca üretilen Arena sahnesini yeniler; sahnede elle yaptığın değişiklikleri önce farklı adla kaydet.

## İnternet üzerinden iki tablet

Unity Services proje bağlantısı henüz yapılmadı; boş `cloudProjectId` ile internet odası çalışmaz.

1. Unity Editor → Edit → Project Settings → Services altında kendi Unity Cloud projenle bağlantı kur. Uygun mevcut proje kullan veya hesabında bu oyun için proje oluştur.
2. Unity Dashboard'da Authentication (Anonymous) ve Multiplayer/Relay erişimini kontrol et. Bu çalışma ücretli servis veya abonelik etkinleştirmedi.
3. `Penalty Arena → 02 — Android APK oluştur` komutunu kullan. Çıktı: `Builds/PenaltiArena-dev.apk`.
4. APK'yı iki tablete kur. İlk cihazda oyuncu adını yazıp **Oda oluştur**; ikinci cihazda kodu yazıp **Katıl**.
5. İki oyuncu da **Hazırım** demeden maç başlamaz. Host kolay modu oda ekranında belirler; iki tarafa aynı top hızı uygulanır.
6. Her şutta roller değişir. Herkes beş şut çeker, eşitlikte karşılıklı ek atış yapılır. Rövanş iki onay ister.

Port yönlendirme gerekmez; internet yolu Multiplayer Services Session + Relay kullanır. Kimlik doğrulama anonimdir. Gizli anahtar kaynak koda yazılmadı. Relay bağlantısı ve hesap ayarları henüz uçtan uca doğrulanmadı.

## Kontroller

- Şutçu: Topun ekran üzerindeki yakınından başlayıp yukarı/çapraza kaydır ve bırak. Uzunluk güç, yön hedef; yüksek güç/kenara hedef dışarı çıkabilir. Hedef yalnızca kendi ekranındadır.
- Kaleci: Sağa/sola/çapraza kaydır; kısa dokunuş merkezde kalır. Editor'de ok tuşları; yukarı oku ile yüksek uzanma, boşluk merkez.
- Fare sürüklemesi dokunmayla aynı işi yapar.
- Ayarlar maçı durdurmaz. 30/60 FPS seçimi bulunur; cihaz performans garantisi değildir.

## Mimari

`RoomConnection`: NGO custom messages, MPS özel Session, max 2 oyuncu, Relay, anonim Authentication. Host kaynak kimliğini bağlantıdan çıkarır; komutta aktör kimliği gönderilmez.

`PenaltyGame`: host yetkili fizik ve skor, şut kimliği/evre/rol/sayısal girdi doğrulaması. Hedef yalnızca şut bırakıldığında gönderilir. İstemci 20 Hz snapshot alır ve görsel pozisyonları yumuşatır. Topun tamamının kale düzlemini geçmesi gol sayılır. Direk sonrası oyun süresi dolana dek sürer; ikinci vuruş reddedilir. Kaleci temasına rağmen içeri giren top gol olabilir.

`MatchRules`: beşer atış, eşit çift ek atış, tek sonuç, rol değişimi. `RuleChecks.Run` gerçek Unity Editor içinde 25 assertion çalıştırır.

`ArenaBuilder`: tekrar üretilebilir mobil URP sahnesi ve Android build komutu.

`Blender/build_goal.py`: metre ölçeğinde kale/file, UV, .blend ve .fbx üretimi.

## Yerel geliştirme testi

Editor ve development build menüsünde `Yerel host` / `Yerel katıl` bulunur. Aynı bilgisayarda IP `127.0.0.1`, aynı ağdaki cihazda host IP'si gerekir. Bu **internet odasının yerine geçmez**; bir kişilik mod veya kullanıcıya sunulan genel eşleştirme değildir.

Otomatik iki süreç testi: development macOS uygulamasına `-arena-smoke-host` ve ikinci örneğine `-arena-smoke-client` verilir. İki süreç hazır olur, 10 şut oynar, tekrar şut gönderimini dener ve aynı sonucu günlüğe yazar. Bu yapay zekâ oyun modu değildir.

## Mevcut eksikler

- Super Goalie karakter/top görselleri ve kaleci animasyonları içe aktarıldı. KCISA stadyumu hesapta var, ancak indirilmediği için henüz entegre değil.
- Kaleci artık Super Goalie humanoid modeli; bekleme ve merkez/sağ/sol kurtarış pozları bağlı. Şutçu hâlâ geçici geometri. Şut, yere iniş ve ayağa kalkış geçişleri ile premium malzemeler tamamlanmadı. Kaleci görsel pozu ile basit fizik collider’larının tam hizalanması henüz yapılmadı.
- Stadyum geçici; KCISA dönüşümü, model/doku optimizasyonu, LOD, baked lighting tamamlanmadı.
- Blender kale modeli hazır; sahne şu anda aynı ölçülerde Editor tarafından üretilen geçici kale kullanır.
- Sinematik açılış videosu, özel sesler, file deformasyonu, replay ve son arayüz tasarımı eklenmedi.
- Gerçek cihazda dokunma, farklı en-boy oranları, 30/60 FPS ölçümleri yapılmadı.
- İnternet gecikmesi telafisi/lag compensation yok; katılan oyuncu bir ağ turu kadar dezavantaj yaşayabilir. Sıfır gecikme iddiası yok.
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

KCISA stadyum paketi yerel bilgisayara indirildi; sahneye entegrasyonu henüz yapılmadı.
