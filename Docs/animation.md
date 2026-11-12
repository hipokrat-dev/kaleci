# Şut ve kurtarış hareketleri

`ArenaAnimated` iki karakterin hareketini ortak NGO sunucu saatinden üretir. Şut komutu kabul edilince `Windup` evresi başlar; tekrar komut atışı yenileyemez. Şutçu yaklaşır, ayağını geri alır ve 0,58 saniyede topa temas eder. Host o fizik adımında topu serbest bırakır. Topun yönünü animasyon olayı değil doğrulanmış hedef ve güç belirler. Vuruş sonrası hareket 2,4 saniyede tamamlanır.

Kalecide orta, sol ve sağ için alçak/orta/yüksek dokuz kurtarış vardır. Kaynak paketin tam klipleri iniş ve kalkış için kullanılır; yalnız başlangıç pozunda bekletilmez. Uzanma süresi `diveDuration`, elin yana erişimi `diveReach` üzerinden ayarlanır. Merkeze dönüş adımlarıyla toplam süre varsayılan ayarda 3,25 saniyedir. Yeni atış, sonuç gösterimi ve devam eden kurtarış tamamlanmadan başlamaz.

Host aynı FixedUpdate içinde kemik eğrilerini örnekler, el/kafa kürelerini ve kol/bacak/gövde kapsüllerini günceller, ardından fiziği ilerletir. Eski sabit karakter collider’ları animasyonlu sahneden kaldırılır. Gerçek el teması topu durdurabilir veya saptırabilir; temas eden top yine çizgiyi geçerse gol sayılır. Diğer oyuncu görsel eğrileri aynı saatten örnekler. Kendi kalecisinin hareketi, onay beklerken kısa süre yerel olarak gösterilir; sonuç yetkisi hostta kalır. Fizikte gecikme için geriye sarma uygulanmaz.

`MotionAuthoring.Bake` kaynak humanoid klipleri mevcut avatar üstünde örnekleyerek 30 Hz generic kemik eğrileri oluşturur. Eğriler pozisyon ve quaternion içerir; quaternion işaret sürekliliği korunur. Runtime Animator kapatılır, böylece iki sistem aynı kemikleri sürmez. Ayağın temas karesi ayrıca örneklenir. Kaynak paket ve önceki sahneler korunur. Üretilen rig bağımlı klipler ve sahne yereldir.

`MotionChecks.Run` pozların sonlu kalmasını, zemine girmemesini, el erişimini, collider hizasını, ayak temasını ve iki karakterin dönüşünü denetler. `MotionPhysicsCheck` yalnız geliştirme testiyle çalışır; dokuz el pozu ve ters tarafa atlayış gerçek PhysX sahnesinde sınanır. `NetworkSmokeCheck` canlı maçta sağ/sol kurtarışları, tekrar şut reddini, Türkçe isimleri ve iki süreçte aynı skoru denetler. Test kapsamı ve sonuçları `verification.md` içindedir.

Görsel önizleme, Unity kamerasından kaydedilmiş hareket örnekleridir. Kareler aynı Editor döngüsünde üretildiği için skin mesh CPU üzerinde her karede yeniden örneklenir. Önizlemedeki şut topu sunum amaçlı yörüngedir; fizik doğrulaması ayrı oyuncu testinde yapılır. Bu çalışma nihai karakter/mocap, ses, seyirci veya cihaz performansı teslimi değildir.
