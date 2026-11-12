using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PenaltyArena.Editor
{
    public static class InterfaceChecks
    {
        public static void Run()
        {
            int checks=0;void Require(bool valid,string message){if(!valid)throw new Exception("INTERFACE: "+message);checks++;}
            foreach(string name in new[]{"Cinzel","Manrope"}) {
                var font=Resources.Load<Font>("Fonts/"+name);Require(font!=null,"Missing "+name);
                string characters="ÇçĞğİıÖöŞşÜü0123456789";font.RequestCharactersInTexture(characters,24);
                foreach(char c in characters)Require(font.HasCharacter(c),name+" missing "+c);
            }
            foreach(var safe in new[]{new Rect(0,0,1600,900),new Rect(0,0,1200,900),new Rect(0,0,1600,720),new Rect(64,20,1472,860)}) {
                float scale=safe.height/720,width=safe.width/scale;var menu=ArenaUI.MenuRect(width);var settings=ArenaUI.SettingsRect(width);
                Require(menu.xMin>=0&&menu.xMax<=width&&menu.yMax<720,"Menu inside safe area");
                Require(settings.xMin>=0&&settings.xMax<=width&&settings.yMax<720,"Settings inside safe area");
                var screenPoint=new Vector2(safe.x+menu.center.x*scale,safe.yMax-menu.center.y*scale);
                Require(Vector2.Distance(ArenaUI.ToCanvas(screenPoint,safe),menu.center)<.01f,"Touch and GUI coordinates agree");
                Require(Vector2.Distance(ArenaUI.ToCanvas(safe.center,safe),new Vector2(width*.5f,360))<.01f,"Inset safe-area centre");
            }
            float standard=Mathf.Tan(38*Mathf.Deg2Rad*.5f)*(16f/9);
            float tablet=Mathf.Tan(PenaltyGame.FitVerticalFieldOfView(38,4f/3)*Mathf.Deg2Rad*.5f)*(4f/3);
            Require(Mathf.Abs(standard-tablet)<.0001f,"4:3 keeps the authored horizontal framing");
            Require(Mathf.Abs(PenaltyGame.FitVerticalFieldOfView(38,16f/9)-38)<.001f,"16:9 framing unchanged");
            Require(Mathf.Abs(PenaltyGame.FitVerticalFieldOfView(38,20f/9)-38)<.001f,"Ultrawide keeps vertical framing");
            File.WriteAllText("Docs/presentation-interface-checks.txt",$"INTERFACE_CHECKS_OK {checks}\nTurkish glyph coverage and 16:9, 4:3, ultrawide, inset safe-area layout mappings passed. Visual layouts are checked in the development player.\n");
            Debug.Log("INTERFACE_CHECKS_OK "+checks);
        }
    }
}
