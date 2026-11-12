#!/usr/bin/env python3
"""Prepare licensed Gregor Quendel recordings. Usage: prepare_crowd_audio.py downloaded.zip
Source: https://opengameart.org/content/free-crowd-cheering-sounds (CC BY 4.0).
"""
from pathlib import Path
import argparse
import subprocess
import tempfile
import zipfile


def prepare(archive):
    destination=Path(__file__).resolve().parents[1]/'Assets/Penalty/Resources/Audio'
    destination.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(archive) as z,tempfile.TemporaryDirectory(prefix='kaleci-audio-') as temp:
        for number,name,duration in [('10','CrowdBed',34),('04','CrowdGoal',6),('05','CrowdSave',4)]:
            entry=next(n for n in z.namelist() if f' - {number} - ' in n and n.endswith('.mp3') and not n.startswith('__MACOSX'))
            source=Path(temp)/(name+'.mp3');source.write_bytes(z.read(entry))
            cmd=['/opt/homebrew/bin/ffmpeg','-v','error','-y','-i',str(source)]
            if name=='CrowdBed':
                # 30-second loop: the final four seconds dissolve into the original beginning.
                filters='[0:a]asplit=3[a][b][c];[a]atrim=4:30,asetpts=PTS-STARTPTS[mid];[b]atrim=30:34,asetpts=PTS-STARTPTS,afade=t=out:st=0:d=4[tail];[c]atrim=0:4,asetpts=PTS-STARTPTS,afade=t=in:st=0:d=4[head];[tail][head]amix=inputs=2:normalize=0[seam];[mid][seam]concat=n=2:v=0:a=1,highpass=f=100,lowpass=f=11000,alimiter=limit=0.9:level=false[out]'
                cmd+=['-filter_complex',filters,'-map','[out]']
            else:
                cmd+=['-t',str(duration),'-af',f'afade=t=in:d=0.15,afade=t=out:st={duration-1.2}:d=1.2,highpass=f=100,alimiter=limit=0.9:level=false']
            subprocess.run(cmd+['-ar','44100','-ac','2','-c:a','pcm_s16le',str(destination/(name+'.wav'))],check=True)


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('archive',type=Path);prepare(p.parse_args().archive)
