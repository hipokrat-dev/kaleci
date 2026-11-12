using UnityEngine;
namespace PenaltyArena
{
    public sealed class ArenaUI : MonoBehaviour
    {
        public PenaltyGame game;
        string code="",address="127.0.0.1";bool settings;ArenaTheme theme;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [System.NonSerialized] public MatchSnapshot captureView;
        [System.NonSerialized] public bool captureShooting,captureAiming,captureSettings;
#endif
        public static Rect SettingsRect(float width)=>new Rect(width-340,88,320,282);
        public bool SettingsVisible=>settings;
        public static Rect MenuRect(float width)=>new Rect(width-68,width<940?112:16,48,48);
        public static Vector2 ToCanvas(Vector2 point,Rect safe){float scale=Mathf.Max(1,safe.height)/720;return new Vector2((point.x-safe.x)/scale,(safe.yMax-point.y)/scale);}
        public bool BlocksInput(Vector2 point){var safe=Screen.safeArea;float width=safe.width/(Mathf.Max(1,safe.height)/720);return settings||MenuRect(width).Contains(ToCanvas(point,safe));}
        void OnDestroy(){theme?.Dispose();}
        void OnGUI()
        {
            if(theme==null)theme=new ArenaTheme();var safe=Screen.safeArea;float scale=Mathf.Max(1,safe.height)/720,width=safe.width/scale;
            var matrix=GUI.matrix;var color=GUI.color;bool enabledBefore=GUI.enabled;
            try {
                GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax,0),Quaternion.identity,Vector3.one*scale);
                var room=game.room;var v=game.View;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(captureView!=null){DrawMatch(width,captureView,captureShooting,captureAiming,.68f,true,safe);if(captureSettings)DrawSettings(width);return;}
#endif
                if(!room.Connected)DrawHome(width);
                else if(game.Phase==MatchPhase.Lobby||game.Phase==MatchPhase.Disconnected)DrawRoom(width,v);
                else DrawMatch(width,v,game.Shooting,game.Aiming,game.Power,false,safe);
                if(settings)DrawSettings(width);
            }finally{GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabledBefore;}
        }
        void DrawHome(float width)
        {
            float x=width*.5f-232;theme.Panel(new Rect(x-34,55,532,600));
            theme.Label(new Rect(x,79,464,24),"GECE SENİN. KALE SENİN.",theme.caption);
            theme.Label(new Rect(x,114,464,56),"PENALTI ARENA",theme.heading);
            theme.Label(new Rect(x,176,464,28),"İki oyuncu. Beş penaltı. Tek kazanan.",theme.muted);Divider(x,219,464);
            theme.Label(new Rect(x,239,464,25),"OYUNCU ADIN",theme.fieldLabel);
            GUI.enabled=!settings;game.PlayerName=GUI.TextField(new Rect(x,270,464,48),game.PlayerName,16,theme.input);
            GUI.enabled=!game.room.Busy&&!settings;
            if(theme.Button(new Rect(x,341,464,50),"ODA OLUŞTUR",true))game.room.Open(true,"");
            theme.Label(new Rect(x,418,464,24),"ARKADAŞININ ODA KODU",theme.fieldLabel);
            code=GUI.TextField(new Rect(x,449,280,48),code,12,theme.input);
            if(theme.Button(new Rect(x+294,449,170,48),"KATIL"))game.room.Open(false,code.Trim());GUI.enabled=true;
            theme.Label(new Rect(x,510,464,54),game.room.Status,theme.status);Divider(x,581,464);
            if(theme.Button(new Rect(x+102,598,260,35),"Ses ve görüntü"))settings=!settings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GUI.enabled=!settings;address=GUI.TextField(new Rect(16,672,190,32),address,theme.input);
            if(theme.Button(new Rect(216,672,136,32),"Yerel host"))game.room.Local(true);
            if(theme.Button(new Rect(362,672,136,32),"Yerel katıl"))game.room.Local(false,address);GUI.enabled=true;
#endif
        }
        void DrawRoom(float width,MatchSnapshot v)
        {
            var room=game.room;bool disconnected=game.Phase==MatchPhase.Disconnected;float x=width*.5f-250;
            theme.Panel(new Rect(x-32,118,564,490));theme.Label(new Rect(x,139,500,28),disconnected?"BAĞLANTI KESİLDİ":"ÖZEL ODA",theme.caption);
            theme.Label(new Rect(x,178,500,57),disconnected?"YENİDEN BULUŞALIM":room.Code,disconnected?theme.name:theme.heading);
            theme.Label(new Rect(x,239,500,24),disconnected?"Maç sonucu belirlenmedi.":"Bu kodu birlikte oynayacağın kişiye söyle.",theme.muted);
            Divider(x,281,500);PlayerRow(x,300,v.name0,v.ready0,true);PlayerRow(x,353,v.count<2?"Rakip bekleniyor…":v.name1,v.ready1,v.count==2);
            GUI.enabled=!settings;if(room.Host&&!disconnected){if(theme.Button(new Rect(x,413,500,38),game.Easy?"KOLAY MOD  ·  AÇIK":"KOLAY MOD  ·  KAPALI"))game.Easy=!game.Easy;}
            else theme.Label(new Rect(x,413,500,38),disconnected?"Odadan çıkıp tekrar bağlanabilirsin.":v.easy?"Kolay mod açık":"Standart hız",theme.muted);
            bool ready=room.LocalSlot==0?v.ready0:v.ready1;GUI.enabled=!disconnected&&!ready&&!settings;
            if(theme.Button(new Rect(x,476,240,50),ready?"HAZIRSIN":"HAZIRIM",true))game.Ready();GUI.enabled=!settings;
            if(theme.Button(new Rect(x+260,476,240,50),"ODADAN ÇIK"))room.Leave();GUI.enabled=true;theme.Label(new Rect(x,543,500,42),room.Status,theme.status);
        }
        void PlayerRow(float x,float y,string player,bool ready,bool present)
        {
            theme.Disc(new Vector2(x+12,y+19),5,present?(ready?new Color(.25f,.75f,.38f):ArenaTheme.Gold):ArenaTheme.Muted);
            theme.Label(new Rect(x+33,y,310,38),player);theme.Label(new Rect(x+350,y,150,38),ready?"HAZIR":present?"BEKLİYOR":"1 / 2",theme.muted);
        }
        void DrawMatch(float width,MatchSnapshot v,bool shooting,bool aiming,float power,bool preview,Rect safe)
        {
            float boardWidth=width>=940?Mathf.Min(568,width-440):Mathf.Min(568,width-32),center=width*.5f,side=boardWidth*.30f;theme.Panel(new Rect(center-boardWidth*.5f,12,boardWidth,73),true);
            theme.Name(new Rect(center-boardWidth*.5f+22,17,side,31),v.name0);theme.Name(new Rect(center+boardWidth*.5f-side-22,17,side,31),v.name1);
            theme.Label(new Rect(center-70,8,140,66),v.score0+" : "+v.score1,theme.score);
            DrawHistory(new Vector2(center-boardWidth*.5f+22+side*.5f,62),v.history0);DrawHistory(new Vector2(center+boardWidth*.5f-22-side*.5f,62),v.history1);
            theme.Panel(new Rect(center-79,74,158,29),true);theme.Label(new Rect(center-76,75,152,26),v.shotId<10?(v.shotId/2+1)+". PENALTI":"EK ATIŞ "+(1+(v.shotId-10)/2),theme.caption);
            float badgeY=width<940?118:19;theme.Panel(new Rect(22,badgeY,176,35),true);
            for(int i=0;i<4;i++)ArenaTheme.Line(new Vector2(38+i*5,badgeY+25),new Vector2(38+i*5,badgeY+21-i*4),ArenaTheme.Gold,3);
            theme.Label(new Rect(61,badgeY+2,126,31),preview?"ÖNİZLEME":"ONLINE · "+v.count+"/2",theme.caption);
            var menu=MenuRect(width);theme.Panel(menu,true);if(GUI.Button(menu,GUIContent.none,GUIStyle.none))settings=!settings;Gear(menu.center);
            if(game.Stale&&!preview){theme.Panel(new Rect(center-174,118,348,35),true);theme.Label(new Rect(center-170,119,340,32),"Bağlantı bekleniyor…",theme.muted);}
            var phase=(MatchPhase)v.phase;if(phase==MatchPhase.Finished){DrawFinished(width,v,preview);return;}
            if(phase==MatchPhase.Result){theme.Panel(new Rect(center-160,199,320,77),true);theme.Label(new Rect(center-150,205,300,61),v.message,theme.heading);}
            string instruction=phase==MatchPhase.Windup||phase==MatchPhase.Flying?(shooting?"VURUŞ YAPILIYOR":"KURTARMAK İÇİN KAYDIR"):(shooting?"ŞUT İÇİN KAYDIR":"KURTARMAK İÇİN KAYDIR");
            theme.Panel(new Rect(center-177,628,354,40),true);theme.Label(new Rect(center-170,632,340,31),instruction,theme.caption);Gesture(new Vector2(center,698),shooting);
            if(shooting){
                theme.Panel(new Rect(width-78,302,64,28),true);theme.Label(new Rect(width-78,302,64,26),"GÜÇ",theme.caption);
                bool show=aiming||phase==MatchPhase.Windup||phase==MatchPhase.Flying;theme.Power(new Rect(width-64,337,36,190),show?power:0);
                if(aiming){var aim=preview?new Vector2(-3,2.1f):game.Aim;var p=ToCanvas(game.gameCamera.WorldToScreenPoint(new Vector3(aim.x,aim.y,0)),safe);theme.Ring(p,17,ArenaTheme.Gold,2);theme.Disc(p,2,ArenaTheme.Ivory);}
            }
        }
        void DrawHistory(Vector2 center,string history)
        {
            history=history??"";int start=Mathf.Max(0,history.Length-5);
            for(int i=0;i<5;i++){
                var p=center+new Vector2((i-2)*25,0);int index=start+i;
                if(index>=history.Length){theme.Disc(p,7,new Color(.11f,.14f,.15f));theme.Ring(p,7,new Color(.38f,.41f,.42f),1.2f);continue;}
                char result=history[index];theme.Disc(p,7.5f,result=='G'?new Color(.20f,.68f,.28f):new Color(.72f,.12f,.15f));
                if(result=='G'){ArenaTheme.Line(p+new Vector2(-3,0),p+new Vector2(-1,2),ArenaTheme.Ivory,1);ArenaTheme.Line(p+new Vector2(-1,2),p+new Vector2(3,-2),ArenaTheme.Ivory,1);}
                else if(result=='K')ArenaTheme.Line(p+Vector2.left*3,p+Vector2.right*3,ArenaTheme.Ivory,1);
                else{ArenaTheme.Line(p-new Vector2(2,2),p+new Vector2(2,2),ArenaTheme.Ivory,1);ArenaTheme.Line(p+new Vector2(-2,2),p+new Vector2(2,-2),ArenaTheme.Ivory,1);}
            }
        }
        void DrawFinished(float width,MatchSnapshot v,bool preview)
        {
            float x=width*.5f-230;theme.Panel(new Rect(x-28,210,516,280));theme.Label(new Rect(x,232,460,31),"PENALTI SERİSİ TAMAMLANDI",theme.caption);
            theme.Label(new Rect(x,280,460,58),v.winner==game.room.LocalSlot?"KAZANDIN":"MAÇ TAMAMLANDI",theme.heading);
            theme.Label(new Rect(x,346,460,28),"Bir maç daha? İki oyuncunun da onayı gerekir.",theme.muted);GUI.enabled=!preview&&!settings;
            if(theme.Button(new Rect(x,414,220,48),"RÖVANŞ",true))game.Rematch();if(theme.Button(new Rect(x+240,414,220,48),"ANA MENÜ"))game.room.Leave();GUI.enabled=true;
        }
        void DrawSettings(float width)
        {
            var r=SettingsRect(width);theme.Panel(r);theme.Label(new Rect(r.x+20,r.y+15,280,30),"SES VE GÖRÜNTÜ",theme.caption);
            if(theme.Button(new Rect(r.x+20,r.y+62,132,42),"30 FPS",Application.targetFrameRate==30))Application.targetFrameRate=30;
            if(theme.Button(new Rect(r.x+168,r.y+62,132,42),"60 FPS",Application.targetFrameRate==60))Application.targetFrameRate=60;
            theme.Label(new Rect(r.x+20,r.y+123,280,25),"SES DÜZEYİ",theme.fieldLabel);
            AudioListener.volume=GUI.HorizontalSlider(new Rect(r.x+20,r.y+162,280,22),AudioListener.volume,0,1);
            theme.Label(new Rect(r.x+20,r.y+191,280,28),game.room.Connected?"Maç devam eder.":"Ses düzeyi",theme.muted);
            if(theme.Button(new Rect(r.x+20,r.y+232,280,34),"KAPAT"))settings=false;
        }
        void Divider(float x,float y,float width)=>ArenaTheme.Line(new Vector2(x,y),new Vector2(x+width,y),new Color(.30f,.28f,.20f,.65f));
        void Gear(Vector2 c){theme.Ring(c,8,ArenaTheme.Gold,3);for(int i=0;i<8;i++){var d=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4));ArenaTheme.Line(c+d*8,c+d*12,ArenaTheme.Gold,4);}}
        static readonly Vector2[] HandOutline={new Vector2(-5,8),new Vector2(-9,-2),new Vector2(-4,-5),new Vector2(-1,0),new Vector2(-1,-15),new Vector2(3,-15),new Vector2(3,-2),new Vector2(10,1),new Vector2(8,10),new Vector2(-5,8)};
        void Gesture(Vector2 c,bool shooting)
        {
            for(int i=1;i<HandOutline.Length;i++)ArenaTheme.Line(c+HandOutline[i-1],c+HandOutline[i],ArenaTheme.Gold,1.7f);
            if(shooting)Arrow(c+new Vector2(0,-27),Vector2.down,14);else{Arrow(c+Vector2.left*57,Vector2.left,25);Arrow(c+Vector2.right*57,Vector2.right,25);}
        }
        static void Arrow(Vector2 tip,Vector2 direction,float length){var side=new Vector2(-direction.y,direction.x);ArenaTheme.Line(tip-direction*length,tip,ArenaTheme.Gold,2);ArenaTheme.Line(tip-direction*7+side*5,tip,ArenaTheme.Gold,2);ArenaTheme.Line(tip-direction*7-side*5,tip,ArenaTheme.Gold,2);}
    }
}
