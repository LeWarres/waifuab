"""Original waxy sci-fi boss cues, synthesized without samples. Mono PCM16 / 44.1 kHz."""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/MusicSound/Resources/SFX'
RATE = 44100

def tone(f, t):
    return np.sin(2*np.pi*np.cumsum(np.broadcast_to(f, t.shape))/RATE)

def noise(t, seed, cutoff=2500):
    rng = np.random.default_rng(seed)
    hz = np.fft.rfftfreq(len(t), 1/RATE)
    x = np.fft.irfft(np.fft.rfft(rng.normal(size=len(t))) * np.exp(-(hz/cutoff)**4), len(t))
    return x / max(1e-9, np.std(x))

def save(name, seconds, make, peak=.45, loop=False):
    t = np.arange(round(seconds*RATE))/RATE
    x = np.tanh(make(t))
    if not loop:
        a, r = min(len(t), int(.008*RATE)), min(len(t), int(.045*RATE))
        x[:a] *= np.sin(np.linspace(0, np.pi/2, a))**2
        x[-r:] *= np.cos(np.linspace(0, np.pi/2, r))**2
    x *= peak/max(1e-9, np.max(np.abs(x)))
    with wave.open(str(OUT / (name+'.wav')), 'wb') as w:
        w.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        w.writeframes((x*32767).astype('<i2').tobytes())
    print(name, seconds)

save('eye_intro', 1.8, lambda t: (.3*tone(90+22*np.sin(t*7),t)+.16*tone(137+30*np.sin(t*4),t)+.11*noise(t,91,1100))*np.sin(np.pi*t/1.8)**.7)
save('eye_mark', .18, lambda t: tone(880+700*t,t)*np.exp(-t*19), .32)
save('eye_beam', .3, lambda t: (.4*tone(1700*np.exp(-t*12)+140,t)+.09*noise(t,92))*np.exp(-t*14), .46)
save('eye_orb', .36, lambda t: (tone(230+800*t,t)+.3*tone(351+1100*t,t))*np.exp(-t*10), .4)
save('eye_hit', .14, lambda t: (.4*noise(t,93,3600)+tone(560*np.exp(-t*20)+190,t))*np.exp(-t*35), .38)
save('eye_shut', .42, lambda t: (tone(820*np.exp(-t*7)+180,t)+.3*tone(1230*np.exp(-t*8),t))*np.exp(-t*11), .44)
save('eye_turret', .65, lambda t: .17*noise(t,94,1500)*np.sin(np.pi*t/.65)**2 + .45*tone(100+350*t,t)*np.exp(-((t-.18)/.15)**2) + .3*tone(420,t)*np.exp(-np.maximum(0,t-.44)*30)*(t>.44), .48)
save('eye_jump', .22, lambda t: (tone(230+1600*t,t)+.16*noise(t,95))*np.sin(np.pi*t/.22), .3)
save('eye_land', .13, lambda t: (.3*noise(t,96,1600)+tone(100,t))*np.exp(-t*38), .3)
save('eye_shot', .095, lambda t: (.3*noise(t,97,3200)+tone(920*np.exp(-t*40)+160,t))*np.exp(-t*60), .4)
save('eye_charge', 1.8, lambda t: (.4*tone(110+1000*(t/1.8)**2,t)+.15*tone(170+1550*(t/1.8)**2,t)+.04*noise(t,98))* (.2+.8*(t/1.8))*(.75+.25*np.sin(2*np.pi*(5*t+4*t*t))), .48)
save('eye_blast', 1.1, lambda t: (.4*noise(t,99,2100)+.6*tone(45+150*np.exp(-t*15),t))*np.exp(-t*7), .58)
# Integer-cycle oscillators give a continuous seam. Fade in/out is controlled in Unity.
save('eye_sweep_loop', 2, lambda t: (.25*np.sin(2*np.pi*110*t+.5*np.sin(2*np.pi*3*t))+.13*np.sin(2*np.pi*221*t)+.06*np.sin(2*np.pi*880*t))*(.8+.2*np.cos(2*np.pi*4*t)), .32, True)
