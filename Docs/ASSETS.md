# Kaynak ve varlık kaydı

- Unity URP Blank template: yerel Unity 6000.6.8f1 kurulumundaki resmi şablon, URP 17.6.0.
- Netcode for GameObjects 2.14.0, Multiplayer Services 2.4.0: Unity Package Manager. Lisansları ilgili paketlerde.
- Goal.blend / Goal.fbx ve build_goal.py: bu proje için üretilmiş özgün metre ölçekli kale/file geometrisi. Blender 5.1.2 ile üretildi. FBX eksen dönüşümü: -Z forward / Y up.
- Geçici saha, tribün, karakterler: ArenaBuilder tarafından üretilen temel geometriler. Final karakter ve stadyum kalitesi değildir.
- Kullanıcının referans görselleri sahne arka planı olarak kullanılmadı.

## Kullanıcı paketleri

1. Super Goalie (Basic), Wasu Studio — https://assetstore.unity.com/packages/templates/packs/super-goalie-basic-144535
2. kcisa - Olympic Park Handball Stadium — https://assetstore.unity.com/packages/3d/props/kcisa-olympic-park-handball-stadium-288707

Her iki paketin yerel içeriği incelendi. Ayrı lisans metni bulunduğu iddia edilmiyor; kaynak paketler yerel önbellekte korunuyor. Hesap sahipliği 12 Kasım 2026 tarihinde kullanıcının giriş yaptığı Asset Store ekranında her iki paket için doğrulandı. KCISA Built-in materyalleri URP'ye dönüştürülmeli; orijinal paket 5.6 GB, tüm içeriği mobil oyuna taşınmamalı. Paket içindeki atıf/lisans dosyaları içe aktarma sırasında korunmalı.

## Resmi API kaynakları

- https://docs.unity.com/en-us/mps-sdk/create-session
- https://docs.unity.com/en-us/mps-sdk/join-session
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/message-system/custom-messages.html
- https://packages.unity.com/com.unity.netcode.gameobjects (sürüm 2.14.0 resmi latest etiketi; 2.7.0 Unity 6.6 EntityId değişimiyle derlenmedi)

## Super Goalie entegrasyonu — 12 Kasım 2026

Kullanıcının indirdiği `Super GoalieBasic.unitypackage` yerel önbellekte bulundu. Character.fbx, karakter/top dokuları ve kaleci animasyonları seçilerek GUID'leri korunmuş şekilde Assets/SuperGoalie altına aktarıldı. Kaynak paketin ProjectSettings, Packages/manifest.json, AI scriptleri, eklentileri, sesleri ve eski demo sahnesi mevcut projeye taşınmadı. Orijinal paket önbellekte değişmeden duruyor. Yayıncı kılavuzu Docs/SuperGoalie içinde korundu.

Bu paket Standard Unity Asset Store EULA kapsamındaki üçüncü taraf varlıktır; bağımsız varlık paketi olarak yeniden dağıtım amacı taşımaz. URP materyalleri ve oyuna özel AnimatorController Assets/Penalty/Art/Goalie altında oluşturuldu. Temel `ArenaWithGoalie` sahnesindeki eski collider’lar korunur; `ArenaAnimated` sahnesinde bunlar kemikleri izleyen collider’larla değiştirilir.

## KCISA stadyum entegrasyonu — 12 Kasım 2026

Yerel paketten 478 kaynak model/materyal/renk dokusu seçildi (yaklaşık 697 MiB). Tam demo sahnesi, ProjectSettings, eklenti veya script taşınmadı. Yerleşimler paket içindeki Demo_SKS sahnesinden okunur. Unity'nin materyal biçim güncellemeleri korunur; orijinal `.unitypackage` değişmeden durur. Paket dizininde ayrıca lisans metni bulunmadı; sahiplik önceki Asset Store oturumunda doğrulanmıştı.

3.442 ayrıntılı koltuk, kaynak yerleşimleri korunarak özgün düşük poligonlu koltuklarla değiştirildi. Çatı kaplaması çıkarıldı; tribün, duvar ve çelik yapı seçimi açık gece arenasına uyarlandı. Kaynak renk dokularını kullanan URP materyalleri türetildi. Kaynak materyal/doku/mesh, türetilmiş birleşik mesh, yerleşim JSON'u ve ArenaStadium sahnesi public repoya gönderilmez.

Çim dokusu, sade koltuklar ve ışık panelleri Editor koduyla üretilir. Blender Goal.fbx stadyum sahnesinde kullanılır. Super Goalie modeli şutçu için de geçici olarak kullanıldı; IdleNonAlert bekleme klibi retarget edildi. Paket içinde şut vuruşu klibi yoktur. Sonraki animasyon çalışmasında bu rig için özgün vuruş eğrileri yazılmıştır; `IdleNonAlert` yerine ayakları yere basan özel hazırlık pozu kullanılır.

## Hareketler — 12 Kasım 2026

`MotionBaker` tam PunchDownCenter, PunchMiddleCentre, PunchHighCentre, PunchLowLeft, PunchMiddleLeft ve PunchHighLeft kliplerini kaynak alır; sağ yönler aynalanır. Uzanma hedefleri, iniş/toparlanma zamanları ve merkeze dönüş oyuna uyarlanır. Şut 2,4 saniyelik özgün kemik hareketleriyle oluşturulur; ayak teması 0,58 saniyedir. Kaynak modele uyarlanmış çıktı klipleri, kontrolörleri ve `ArenaAnimated.unity` Git dışında kalır; üretim kodu repodadır.

Resmi API referansları:
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/animationclip/sampleanimation
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/animationutility/seteditorcurve
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/skinnedmeshrenderer/bakemesh

## Gece sahnesi — 12 Kasım 2026

Bağımsız çim ve şeffaf 4×4 seyirci atlası yerleşik image_gen ile bu proje için üretildi; `Assets/Penalty/Art/Lookdev` içinde repoya eklenir. Seyirciler tribün katlarında 24 mekânsal gruba ayrılmış kesit yüzeyleridir. Referans görselleri arka plan olarak kullanılmaz. Super Goalie yüz dokusu UV düzeni korunarak ayrıntılandırıldı; bu türev yerelde kalır. Forma/saç meshleri de kaynak iskelete uyarlandığı için Presentation dizini ve ArenaNight sahnesi public Git dışında tutulur. KCISA dış çatı halkası kullanılır; futbol tribünü, saçak, yeni kale/file, saha çizgileri ve projektör yerleşimi Editor koduyla üretilir. Yönergeler ve çıktı yolları: [art-direction.md](art-direction.md).


## Arayüz yazı tipleri — 12 Kasım 2026

- [Cinzel, Google Fonts kaynağı](https://github.com/google/fonts/tree/main/ofl/cinzel): Natanael Gama / Cinzel Project Authors. `Cinzel[wght].ttf` değişken dosyasından sabit 600 SemiBold ağırlığı.
- [Manrope, Google Fonts kaynağı](https://github.com/google/fonts/tree/main/ofl/manrope): Manrope Project Authors. `Manrope[wght].ttf` değişken dosyasından sabit 500 Medium ağırlığı.
- İkisi de SIL Open Font License 1.1. Kaynak OFL metinleri, telif bildirimleri ve kullanılan sabit TTF dosyaları `Assets/Penalty/Resources/Fonts` içinde birlikte dağıtılır. Bunlar Asset Store paketlerinden gelmez.
- Sabitleme: fontTools 4.68.0 `instantiateVariableFont(font, {"wght": weight}, inplace=False)`. Aile adları korunur; name tablosunda 2/17 (alt aile), 4 (tam ad), 6 (PostScript ad) sabit ağırlığa göre güncellenir. Türkçe `ÇçĞğİıÖöŞşÜü` karakter kapsamı Unity'de doğrulanır.


## Oyuncu ayrıntıları — 12 Kasım 2026

Kaynak karakterin yüz/forma/el meshleri UV ve iskelet korunarak yumuşatıldı. Yeni saç geometrisi ve uzun kaleci kolları kaynak iskelete bağlı türevlerdir; ham meshler yerelde kalır. `PlayerMeshRefinement`, `PremiumHair` ve `PlayerSurface.shader` üretim kodları repodadır. `KitRelief.png` ve `HeadDetail-v2.png`, yerleşik image_gen ile mevcut UV dokularından düzenlendi. Kaynak paketlerden türedikleri için bu dokular ve bunlardan çıkarılan normal map public Git dışında tutulur. Yeniden üretim ve yönerge kaydı: [art-direction.md](art-direction.md).
