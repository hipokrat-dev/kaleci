# Gece arenası — 12 Kasım 2026

`ArenaNight.unity`, iki referanstaki gece stadyumu ve forma renklerini izleyen oynanabilir sahnedir. Bu bir sanat iyileştirmesidir; ücretsiz kaynak karakterin yüz, el, vücut ve kıyafet geometrisi hâlâ referanstaki fotogerçekçilik düzeyinde değildir.

## Sahne

- KCISA paketinin dış çelik çatı halkası korunur. Kapalı salon duvarları ve iç tavan çubukları yerine açık gökyüzü, üç katlı futbol tribünü ve saçak geometrisi kullanılır.
- Seyirciler 24 bölgeye birleştirilmiş, fotoğraf dokulu kesit yüzeyleridir. Her sıranın konumu, yüksekliği ve yönü 3D tribüne göre hesaplanır; hafif hareket shader içinde verilir. Yakın planda kullanılacak tam 3D insan modelleri değildir.
- Özgün çim dokusu; biçme şeritleri, çizgiler, penaltı noktası ve kale önündeki aşınmayla birleştirilir. Kamera yakınında ayrı çim uçları vardır.
- Beyaz/lacivert 9 numara şutçu ve turkuaz 1 numara kaleci; ayrı kumaş, çorap, krampon, eldiven ve saç görünümü. Saç meshleri mevcut iskelete bağlıdır.
- Soğuk ana projektör ışığı, dolgu ve sıcak kenar ışığı; LED bankaları, sıcak tribün bantları, sis, bloom, ACES ve hafif vignette. Mobil URP: 4× MSAA, FXAA, 30 m gölge mesafesi, dört kemik ağırlığı.
- Yeni kale ve file görseli mevcut fizik geometrisini korur. Kaleci kamerasında yalnız file görseli gizlenir.

`PenaltyGame` kamera konumlarını sahneden okur. Yeni kadrajın topu dokunma alanında tuttuğu ve ekran sağının her iki rolde doğru dünya yönüne çevrildiği doğrulanır. Oyun kuralları ve ağ mesajları bu çalışma için değiştirilmedi.

## Maç arayüzü ve oyuncu duruşları

Siyah-altın skor levhası, beş atış göstergesi, tur sekmesi, bağlantı durumu, özel hedef halkası, güç çubuğu ve kaydırma simgeleri referans görsellerin düzenini izler. Sonuç noktalarında renk yanında işaret de vardır: gol onay işareti, kurtarış çizgi, dışarı çarpı. Özel oda, ana menü, maç sonu ve ayarlar aynı tipografi ve renkleri kullanır.

Arayüz `Screen.safeArea` içinde 720 birim yüksekliğe ölçeklenir. Uzun Türkçe adlar alanlarına sığdırılır; dar ekranlarda bağlantı ve ayar düğmeleri skor levhasının altına taşınır. 4:3 kamera, 16:9 için yazılmış yatay kadrajı koruyacak kadar genişler; şutçu ve top kadrajda kalır. Ayarlar sahadaki dokunma/fare ve klavye girdisini engeller, çevrimiçi maçı durdurmaz.

Şutçu beklerken kollarını indirir, göğsünde hafif ve kesintisiz 2,4 saniyelik nefes döngüsü vardır; vuruşa girerken kollar denge pozuna geçer. Kaleci eldivenlerini vücudunun önünde hazır tutar. Forma yanlarında hafif kırışıklık gölgesi, yaka şeridi ve küçük arma bulunur. Şutçunun el görünümü kaynak eldiven geometrisinin ten rengi ve avuç daraltmasıyla uyarlanmıştır; yeni anatomik el modeli değildir. Bilek birleşimi korunur.

Başlıklar Cinzel SemiBold 600, metinler Manrope Medium 500 kullanır. Google Fonts değişken kaynaklarından fontTools 4.68.0 ile sabit ağırlık çıkarılmıştır; TTF dosyaları ve OFL lisansları `Assets/Penalty/Resources/Fonts` içindedir. Kaynaklar ve lisans kaydı: [ASSETS.md](ASSETS.md).

## Yeniden oluşturma

Önce README'deki paket geri yükleme adımları ve `07 — Şut ve kurtarışları oluştur` tamamlanmalı. Ardından:

1. `Penalty Arena → 08 — Gece arenasını oluştur`.
2. `Penalty Arena → 09 — Gece sahnesini doğrula`.
3. `Penalty Arena → 02 — Android APK oluştur`.

Açık gece sahnesi kaydedilmemişse yeniden üretimden önce `Art/Presentation/BeforeRebuild.unity` kopyasına alınır. `ArenaAnimated.unity` kaynak sahne olarak korunur. Üretim araçları, shaderlar ve bağımsız üretilen çim/seyirci dokuları Git'tedir. Kaynak paketlerden türetilen mesh, forma dokuları, yüz dokusu ve gece sahnesi yereldedir.

Canlı Editor'de tekrar üretim, meshlerin vertex/index GPU verisini açıkça yeniler. Yalnız `CopySerialized` kullanmak CPU mesh verisi güncelken eski geometriyi ekranda bırakabildiği için mesh güncellemesi native Mesh API üzerinden yapılır.

## Görsel üretim kaydı

Araç: yerleşik `image_gen`, imagegen becerisi. Referans ekran görüntüleri sahne arka planı olarak kullanılmadı. Üretim çıktıları gerçek Unity sahnesinde materyal olarak kullanıldı.

| Dosya | İşlem | Üretim yönergesi |
|---|---|---|
| `Assets/Penalty/Art/Lookdev/TurfColor.png` | Yeni görsel; opak, 1254×1254 çıktı | Gerçek zamanlı futbol sahası için dört kenardan tekrar edebilen taban rengi. Tam tepeden ortografik görünüş; 20 mm biçilmiş sık doğal ryegrass, küçük gerçekçi çim uçları, dengeli yeşiller. Düz dağınık ışık. Çizgi, biçme bandı, nesne, yazı, ufuk veya sert gölge bulunmamalı. Kare 2048 istenmişti; aracın döndürdüğü çözünürlük 1254'tür. |
| `Assets/Penalty/Art/Lookdev/CrowdAtlas.png` | Yeni görsel; şeffaf, 1254×1254 | Tam 4×4 düzen; her hücrede tek, önden görülen, bel üstü yetişkin seyirci. Farklı görünüşler; lacivert, krem, gri, mavi ve bordo gündelik kıyafet. Bazıları alkışlıyor veya tezahürat ediyor. Düz dağınık ışık, bağımsız saydam kesitler; stadyum, koltuk, yazı veya çizilmiş hücre sınırı yok. |
| `Assets/Penalty/Art/Presentation/HeadDetail.png` | Kaynak `head.jpg` düzenleme; yerel türev | UV adaları, sınırlar, yüz özelliklerinin konumu, açılım bozulması ve beyaz boş alanlar aynı kalmalı. Orta kahverengi ten korunmalı; gözenek, koyu kaş, kısa sakal ve dudak ayrıntısı iyileştirilmeli. Düz renk dokusu; yeni portre veya yönlü pişirilmiş gölge olmamalı. |

Yüz dosyası bulunmazsa araç orijinal paket dokusunu kullanır. Bağımsız çim ve seyirci dosyaları yeniden kurulum için repoda tutulur. Kaynak üretim dosyaları kullanıcının `.codex/generated_images` dizininde korunur.

## İnceleme

`PremiumArena.Preview` iki rol ve yakın planlar için URP [SingleCameraRequest](https://docs.unity3d.com/6000.0/Manual/urp/User-Render-Requests.html) kullanır. Poz incelemesinde CPU `BakeMesh` kopyaları ile tutarlı kareler alınır. Development player'daki `-arena-look-capture <klasör>` isteğe bağlı doğrulama aracı ise gerçek GPU skinning ve ekran çıktısını alır; kullanıcıya sunulan oyun modu değildir.

Yerel sonuçlar `Docs/presentation-*.png`, `Docs/presentation-runtime/`, `Docs/presentation-checks.txt` ve `Docs/presentation-build.txt` içinde bulunur. Cihaz performansı ancak Android tablet üzerinde ölçülebilir; masaüstü render veya bağlantı testi FPS garantisi vermez.

`-arena-ui-capture <klasör>` development seçeneği, gerçek GPU skinning ve oyun arayüzünü birlikte kaydeder. Ana menü, iki rol, kurtarış, sonuç, ayarlar, maç sonu, 4:3 ve geniş ekran örneklerini üretir. Maç görsellerindeki 2:1 skor örnek veridir ve **ÖNİZLEME** olarak işaretlenir; oynanmış maç sonucu değildir. Yerel görüntüler: `Docs/presentation-ui/`. `InterfaceChecks.Run` Türkçe karakterleri, güvenli alan koordinatlarını ve ekran oranlarına göre kamera açısını denetler.
