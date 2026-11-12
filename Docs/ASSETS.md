# Kaynak ve varlık kaydı

- Unity URP Blank template: yerel Unity 6000.6.8f1 kurulumundaki resmi şablon, URP 17.6.0.
- Netcode for GameObjects 2.14.0, Multiplayer Services 2.4.0: Unity Package Manager. Lisansları ilgili paketlerde.
- Goal.blend / Goal.fbx ve build_goal.py: bu proje için üretilmiş özgün metre ölçekli kale/file geometrisi. Blender 5.1.2 ile üretildi. FBX eksen dönüşümü: -Z forward / Y up.
- Geçici saha, tribün, karakterler: ArenaBuilder tarafından üretilen temel geometriler. Final karakter ve stadyum kalitesi değildir.
- Kullanıcının referans görselleri sahne arka planı olarak kullanılmadı.

## Kullanıcı paketleri

1. Super Goalie (Basic), Wasu Studio — https://assetstore.unity.com/packages/templates/packs/super-goalie-basic-144535
2. kcisa - Olympic Park Handball Stadium — https://assetstore.unity.com/packages/3d/props/kcisa-olympic-park-handball-stadium-288707

Bu iki paketin lisans dosyaları ve içerikleri henüz yerelde doğrulanmadı. Hesap sahipliği 12 Kasım 2026 tarihinde kullanıcının giriş yaptığı Asset Store ekranında her iki paket için doğrulandı. KCISA Built-in materyalleri URP'ye dönüştürülmeli; orijinal paket 5.6 GB, tüm içeriği mobil oyuna taşınmamalı. Paket içindeki atıf/lisans dosyaları içe aktarma sırasında korunmalı.

## Resmi API kaynakları

- https://docs.unity.com/en-us/mps-sdk/create-session
- https://docs.unity.com/en-us/mps-sdk/join-session
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/message-system/custom-messages.html
- https://packages.unity.com/com.unity.netcode.gameobjects (sürüm 2.14.0 resmi latest etiketi; 2.7.0 Unity 6.6 EntityId değişimiyle derlenmedi)

## Super Goalie entegrasyonu — 12 Kasım 2026

Kullanıcının indirdiği `Super GoalieBasic.unitypackage` yerel önbellekte bulundu. Character.fbx, karakter/top dokuları ve kaleci animasyonları seçilerek GUID'leri korunmuş şekilde Assets/SuperGoalie altına aktarıldı. Kaynak paketin ProjectSettings, Packages/manifest.json, AI scriptleri, eklentileri, sesleri ve eski demo sahnesi mevcut projeye taşınmadı. Orijinal paket önbellekte değişmeden duruyor. Yayıncı kılavuzu Docs/SuperGoalie içinde korundu.

Bu paket Standard Unity Asset Store EULA kapsamındaki üçüncü taraf varlıktır; bağımsız varlık paketi olarak yeniden dağıtım amacı taşımaz. URP materyalleri ve oyuna özel AnimatorController Assets/Penalty/Art/Goalie altında oluşturuldu. Önceki geometrik kaleci collider'ları korunuyor; bunların animasyondaki el/gövde pozlarıyla tam hizalanması sonraki oynanış düzenlemesidir.
