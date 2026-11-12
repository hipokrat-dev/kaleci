using UnityEngine;
namespace PenaltyArena
{
    public sealed class ArenaUI : MonoBehaviour
    {
        public PenaltyGame game;
        string code = "", address = "127.0.0.1"; bool settings;
        GUIStyle title, small, button, panel, score;
        public bool BlocksInput(Vector2 point)
        {
            var safe = Screen.safeArea; float scale = safe.height / 720f;
            Vector2 p = new Vector2((point.x-safe.x)/scale,(safe.yMax-point.y)/scale);
            float width=safe.width/scale;
            return (settings && new Rect(width-330,80,310,260).Contains(p)) || new Rect(width-135,24,115,40).Contains(p);
        }
        void Styles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(.94f,.91f,.82f);
            small = new GUIStyle(title) { fontSize = 18, wordWrap = true };
            score = new GUIStyle(title) { fontSize = 44 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 19, padding = new RectOffset(18,18,12,12) };
            panel = new GUIStyle(GUI.skin.box); panel.normal.background = Texture2D.whiteTexture;
            GUI.skin.textField.fontSize = 21;
        }
        void Box(Rect r) { var old = GUI.backgroundColor; GUI.backgroundColor = new Color(.035f,.05f,.055f,.94f); GUI.Box(r,"",panel); GUI.backgroundColor = old; }
        void Label(Rect r, string text, GUIStyle style = null) => GUI.Label(r, text, style ?? small);
        bool Button(Rect r, string text) => GUI.Button(r, text, button);
        void OnGUI()
        {
            if (title == null) Styles();
            var safe = Screen.safeArea; float scale = safe.height / 720f; float width = safe.width / scale;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * scale);
            var room = game.room; var v = game.View;
            if (!room.Connected) {
                float x = width/2-230; Box(new Rect(x-32,90,524,535));
                Label(new Rect(x,112,460,50),"PENALTI / ARENA",title);
                Label(new Rect(x,164,460,40),"İKİ OYUNCU · TEK KALE");
                game.PlayerName = GUI.TextField(new Rect(x,224,460,44),game.PlayerName,16);
                GUI.enabled = !room.Busy;
                if (Button(new Rect(x,286,460,48),"Oda oluştur")) room.Open(true, "");
                code = GUI.TextField(new Rect(x,354,270,44),code,12);
                if (Button(new Rect(x+285,354,175,44),"Katıl")) room.Open(false,code);
                GUI.enabled = true;
                Label(new Rect(x,420,460,70),room.Status);
                if (Button(new Rect(x,518,460,44),"Ses ve görüntü")) settings = !settings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                address = GUI.TextField(new Rect(20,620,230,32),address);
                if (Button(new Rect(20,660,175,36),"Yerel host")) room.Local(true);
                if (Button(new Rect(205,660,175,36),"Yerel katıl")) room.Local(false,address);
#endif
            } else if (game.Phase == MatchPhase.Lobby || game.Phase == MatchPhase.Disconnected) {
                float x=width/2-250; Box(new Rect(x-30,170,560,375));
                Label(new Rect(x,190,500,50),"ODA  " + room.Code,title);
                Label(new Rect(x,260,500,42),v.name0 + (v.ready0 ? "  ✓ Hazır" : "  • Bekliyor"));
                Label(new Rect(x,307,500,42),v.count < 2 ? "Rakip bekleniyor…" : v.name1 + (v.ready1 ? "  ✓ Hazır" : "  • Bekliyor"));
                if (room.Host) game.Easy = GUI.Toggle(new Rect(x,365,460,30),game.Easy,"Kolay mod (iki oyuncu için)");
                if (Button(new Rect(x,414,240,48),"Hazırım")) game.Ready();
                if (Button(new Rect(x+260,414,240,48),"Odadan çık")) room.Leave();
                Label(new Rect(x,480,500,42),room.Status);
            } else {
                Box(new Rect(width/2-370,18,740,108));
                Label(new Rect(width/2-350,26,250,35),v.name0);
                Label(new Rect(width/2+100,26,250,35),v.name1);
                Label(new Rect(width/2-90,24,180,62),$"{v.score0} : {v.score1}",score);
                Label(new Rect(width/2-350,64,250,35),History(v.history0)); Label(new Rect(width/2+100,64,250,35),History(v.history1));
                string round = v.shotId < 10 ? $"{v.shotId/2+1}. TUR" : $"EK ATIŞ {1+(v.shotId-10)/2}";
                Label(new Rect(width/2-100,94,200,25),round);
                Label(new Rect(16,24,200,32),"ONLINE • 2/2");
                if (Button(new Rect(width-135,24,115,40),"Ayarlar")) settings = !settings;
                if (game.Stale) Label(new Rect(width/2-220,138,440,35),"Bağlantı bekleniyor…");
                if (game.Phase == MatchPhase.Finished) {
                    Box(new Rect(width/2-260,235,520,250));
                    Label(new Rect(width/2-240,260,480,50),v.winner == room.LocalSlot ? "KAZANDIN" : "MAÇ TAMAMLANDI",title);
                    Label(new Rect(width/2-240,315,480,40),"Rövanş için iki oyuncunun onayı gerekir.");
                    if (Button(new Rect(width/2-230,390,220,48),"Rövanş")) game.Rematch();
                    if (Button(new Rect(width/2+10,390,220,48),"Ana menü")) room.Leave();
                } else {
                    if (game.Phase == MatchPhase.Result) Label(new Rect(width/2-220,235,440,70),v.message,title);
                    Box(new Rect(width/2-280,642,560,55));
                    Label(new Rect(width/2-270,648,540,42),game.Shooting ? "ŞUTÇU  /  Toptan başlayarak kaydır" : "KALECİ  /  Atlamak için kaydır · Merkez için dokun");
                    if (game.Aiming) {
                        Vector3 p = game.gameCamera.WorldToScreenPoint(new Vector3(game.Aim.x,game.Aim.y,0));
                        Label(new Rect((p.x-safe.x)/scale-20,(Screen.height-p.y-(Screen.height-safe.yMax))/scale-20,40,40),"◎",title);
                        Label(new Rect(width-150,320,135,50),$"GÜÇ %{game.Power*100:0}");
                    }
                }
            }
            if (settings) {
                Box(new Rect(width-330,80,310,260)); Label(new Rect(width-315,95,280,40),"GÖRÜNTÜ / SES");
                if (Button(new Rect(width-310,145,125,40),"30 FPS")) Application.targetFrameRate=30;
                if (Button(new Rect(width-175,145,125,40),"60 FPS")) Application.targetFrameRate=60;
                AudioListener.volume=GUI.HorizontalSlider(new Rect(width-310,210,260,24),AudioListener.volume,0,1);
                Label(new Rect(width-310,245,270,45),room.Connected ? "Maç devam eder." : "Ses düzeyi");
                if (Button(new Rect(width-310,292,260,35),"Kapat")) settings=false;
            }
        }
        static string History(string h)
        {
            string text=""; int start=Mathf.Max(0,h.Length-6);
            for(int i=start;i<Mathf.Max(5,h.Length);i++) text += i>=h.Length ? "○  " : h[i]=='G' ? "●  " : h[i]=='K' ? "K  " : "×  ";
            return text;
        }
    }
}
