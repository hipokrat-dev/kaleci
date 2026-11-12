# Doğrulama — 12 Kasım 2026

## Çalıştırıldı
- Unity 6000.6.8f1 / URP 17.6.0 / NGO 2.14.0 / MPS 2.4.0 ile C# derlemesi.
- Editor sahne üretimi: Arena.unity.
- MatchRules: 25 assertion geçti (Docs/rule-test-results.txt).
- macOS development build başarılı.
- İki ayrı macOS süreç / gerçek UDP loopback testi: host ve client aynı sonucu raporladı:
  `score=5:0 history=GGGGG/DDDDD winner=0 shots=10`.
- Testte her atışta tekrar şut komutu gönderildi; atış sayısı/skor çoğalmadı.
- Blender 5.1.2 ile Goal.blend ve Goal.fbx üretimi başarılı.
- Android IL2CPP ARM64 development APK derlemesi başarılı: Builds/PenaltiArena-dev.apk (yaklaşık 44 MB).
- macOS menüsü görsel olarak açıldı; Türkçe metinler ve geçici 3D sahne görüntülendi. Unity Services proje bağlantısı olmadığı durumda oda açma hatası gösterildi.
- Kullanıcı Unity hesabına giriş yaptı; Asset Store'da iki paketin sahipliği doğrulandı: Super Goalie (Basic), KCISA Olympic Park Handball Stadium. İkisinde de Open in Unity / satın alınmış bilgisi.

## Henüz test edilmedi
- UGS proje bağlantısı + Relay üzerinden ayrı internet ağlarından bağlantı.
- Fiziksel Android cihazda APK açılışı, dokunma, performans ve ısınma.
- Gerçek üç istemci ile oda kapasitesi reddi (kod mevcut; rol kuralında üçüncü aktör testi geçti).
- Ağ kaybı/gecikmesi, arka plana alma, bağlantı kopması ve yeniden oda açma.
- Rövanşın iki ayrı cihazdan onayı (uygulandı; uçtan uca test henüz yok).
- Tüm ekran oranları, animasyonlar ve premium görsel kalite.

Sonuç: çalışan teknik temel; yayınlanabilir/final oyun değil. Yerel ağ testi internet testi yerine geçmez.

## Paket aktarımı engeli
Tarayıcı güvenlik denetimi `Open in Unity` protokol bağlantısını engelledi. Başka yolla tetiklenmedi. Kullanıcının bu düğmeye basıp Unity Package Manager indirmesini başlatması gerekiyor. Paketlerin sahipliği doğrulandı, indirme/Import tamamlandı diye sayılmıyor.

## Kaleci entegrasyonu — 12 Kasım 2026, 18:02 (İstanbul)

- Kullanıcının indirdiği Super Goalie paketinin model/doku/animasyon dosyaları seçilerek aktarıldı. Eski proje ayarları aktarılmadı.
- Humanoid avatar ve 10 Animator durumu (bekleme, üç yükseklikte merkez/sol/sağ) doğrulandı. Sağ pozlar sol kliplerden aynalanır.
- Idle / LeftMiddle / RightMiddle Unity render çıktıları görüntülenerek kontrol edildi; pose raporu Docs/goalie-pose-check.txt.
- Model URP materyalleriyle yeni ArenaWithGoalie.unity sahnesine eklendi. Teknik Arena.unity korunuyor.
- Kaleci animasyon kodu ve ilerleme değeri host snapshot'ına eklendi. Sonuç kararı animasyona verilmedi.
- 25 kural kontrolü yeniden geçti. Kalecili Android IL2CPP ARM64 APK derlemesi başarılı: 61.587.327 byte (yaklaşık 59 MiB).
- Önceki iki uygulamalı ağ testi kaleci görsel entegrasyonundan önceki sürüme aittir. Yeni görsel entegrasyonu iki cihazda yeniden test edilmedi.
- Animasyon ile fizik collider'larının tam hizalanması, iniş/ayağa kalkma, stadyum entegrasyonu, Relay ve fiziksel tablet testleri tamamlanmadı.
- Tarayıcı protokol engelinden sonra kullanıcı Download işlemini yaptı; artık kaleci paketi için indirme engeli yok. Stadyum paketi hâlâ yerelde yok.

## Repo hazırlığı
GitHub deposu hipokrat-dev/kaleci: başlangıçta boş, herkese açık. Üçüncü taraf paket kaynakları ve bunlardan türetilen ham animasyonlar Git dışında bırakıldı. KCISA stadyumunun yerel indirmesi doğrulandı; entegrasyon tamamlanmadı.
