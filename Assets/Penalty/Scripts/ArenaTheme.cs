using System;
using UnityEngine;
namespace PenaltyArena
{
    // Reusable textures are generated once, rather than allocating every OnGUI event.
    public sealed class ArenaTheme : IDisposable
    {
        public static readonly Color Gold=new Color(.82f,.70f,.44f),Ivory=new Color(.96f,.92f,.81f),Muted=new Color(.59f,.64f,.65f);
        public readonly Font displayFont,bodyFont;
        public readonly GUIStyle panel,pill,button,primary,input,heading,caption,name,score,label,muted,fieldLabel,status;
        readonly Texture2D panelTexture,pillTexture,buttonTexture,hoverTexture,primaryTexture,fieldTexture,circle,ring,boldRing,gradient;
        public ArenaTheme()
        {
            displayFont=Resources.Load<Font>("Fonts/Cinzel");bodyFont=Resources.Load<Font>("Fonts/Manrope");
            panelTexture=Surface(18,new Color(.025f,.035f,.042f,.96f),new Color(.30f,.26f,.17f,.9f));
            pillTexture=Surface(31,new Color(.018f,.026f,.03f,.94f),new Color(.57f,.47f,.28f,.9f));
            buttonTexture=Surface(14,new Color(.055f,.069f,.075f,.98f),new Color(.37f,.34f,.24f,1));
            hoverTexture=Surface(14,new Color(.13f,.15f,.15f,1),Gold);
            primaryTexture=Surface(14,new Color(.61f,.48f,.25f,1),new Color(.94f,.83f,.57f,1));
            fieldTexture=Surface(12,new Color(.012f,.02f,.025f,.96f),new Color(.19f,.24f,.26f,1));
            panel=Background(panelTexture,18);pill=Background(pillTexture,31);
            button=Background(buttonTexture,14);button.font=bodyFont;button.fontSize=18;button.alignment=TextAnchor.MiddleCenter;button.normal.textColor=Ivory;
            button.hover.background=hoverTexture;button.hover.textColor=Ivory;button.active.background=primaryTexture;button.active.textColor=Color.white;
            button.focused=button.hover;button.padding=new RectOffset(14,14,8,8);button.richText=false;
            primary=new GUIStyle(button);primary.normal.background=primaryTexture;primary.normal.textColor=new Color(.05f,.06f,.065f);primary.fontStyle=FontStyle.Bold;
            input=Background(fieldTexture,12);input.font=bodyFont;input.fontSize=21;input.normal.textColor=Ivory;input.focused.background=buttonTexture;input.focused.textColor=Ivory;input.alignment=TextAnchor.MiddleLeft;input.padding=new RectOffset(16,16,8,8);input.richText=false;
            heading=Text(displayFont,36,Ivory);caption=Text(displayFont,17,Gold);name=Text(displayFont,20,Ivory);score=Text(displayFont,48,Ivory);
            label=Text(bodyFont,17,Ivory);muted=Text(bodyFont,14,Muted);fieldLabel=new GUIStyle(muted){alignment=TextAnchor.MiddleLeft,fontSize=12};status=new GUIStyle(muted){wordWrap=true};
            circle=new Texture2D(64,64,TextureFormat.RGBA32,false){name="ArenaCircle",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))));circle.SetPixels(pixels);circle.Apply();
            ring=RingTexture(4);boldRing=RingTexture(10);
            gradient=new Texture2D(1,128,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<128;y++)gradient.SetPixel(0,y,Color.Lerp(new Color(.39f,.28f,.10f),new Color(1,.9f,.57f),y/127f).linear);gradient.Apply();
        }
        static GUIStyle Text(Font font,int size,Color color){var s=new GUIStyle{font=font,fontSize=size,alignment=TextAnchor.MiddleCenter,richText=false,wordWrap=false,clipping=TextClipping.Clip};s.normal.textColor=color;return s;}
        static GUIStyle Background(Texture2D texture,int border){var s=new GUIStyle{border=new RectOffset(border,border,border,border)};s.normal.background=texture;return s;}
        static Texture2D Surface(float radius,Color fill,Color stroke)
        {
            const int n=64;var t=new Texture2D(n,n,TextureFormat.RGBA32,false){name="ArenaSurface",wrapMode=TextureWrapMode.Clamp};var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                var q=new Vector2(Mathf.Abs(x+.5f-32)-(31-radius),Mathf.Abs(y+.5f-32)-(31-radius));
                float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                Color c=Color.Lerp(fill,stroke,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-2,-.6f,d)));c.a*=Mathf.Clamp01(.5f-d);colors[y*n+x]=c.linear;
            }
            t.SetPixels(colors);t.Apply();return t;
        }
        static Texture2D RingTexture(float width)
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-d)*Mathf.Clamp01(d-(31.5f-width)));}
            t.SetPixels(pixels);t.Apply();return t;
        }
        public void Panel(Rect r,bool round=false)
        {
            float radius=round?Mathf.Min(r.width,r.height)*.5f:16;
            GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.018f,.025f,.03f,.96f).linear,0,radius);
            GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.57f,.47f,.28f,.9f).linear,1,radius);
        }
        public void Label(Rect r,string text,GUIStyle style=null)=>GUI.Label(r,text,style??label);
        public void Name(Rect r,string text){int size=name.fontSize;float width=name.CalcSize(new GUIContent(text)).x;name.fontSize=Mathf.Clamp(Mathf.FloorToInt(size*r.width/Mathf.Max(1,width)),12,size);GUI.Label(r,text,name);name.fontSize=size;}
        public bool Button(Rect r,string text,bool emphasized=false)=>GUI.Button(r,text,emphasized?primary:button);
        public void Disc(Vector2 c,float radius,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(c.x-radius,c.y-radius,2*radius,2*radius),circle);GUI.color=old;}
        public static void Line(Vector2 a,Vector2 b,Color color,float thickness=1)
        {
            var matrix=GUI.matrix;var old=GUI.color;Vector2 d=b-a;GUI.color=color;GUI.matrix=matrix*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
            GUI.DrawTexture(new Rect(0,-thickness*.5f,d.magnitude,thickness),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;
        }
        public void Ring(Vector2 c,float radius,Color color,float thickness=1.5f){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(c.x-radius,c.y-radius,radius*2,radius*2),thickness/radius>.2f?boldRing:ring);GUI.color=old;}
        public void Power(Rect r,float amount)
        {
            Panel(r,true);var inner=new Rect(r.x+6,r.y+7,r.width-12,r.height-14);float h=inner.height*Mathf.Clamp01(amount);
            if(h>0)GUI.DrawTextureWithTexCoords(new Rect(inner.x,inner.yMax-h,inner.width,h),gradient,new Rect(0,0,1,Mathf.Clamp01(amount)));
            for(int i=1;i<5;i++)Line(new Vector2(inner.x,inner.yMax-inner.height*i/5),new Vector2(inner.xMax,inner.yMax-inner.height*i/5),new Color(.06f,.07f,.07f,.35f));
        }
        public void Dispose(){foreach(var t in new[]{panelTexture,pillTexture,buttonTexture,hoverTexture,primaryTexture,fieldTexture,circle,ring,boldRing,gradient})if(t!=null)UnityEngine.Object.Destroy(t);}
    }
}
