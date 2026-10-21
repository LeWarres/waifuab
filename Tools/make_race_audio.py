"""Original race sound design: mechanical hover engines, friction and clean sci-fi cues.
Uses numpy and no external recordings. Output: mono 44.1 kHz PCM, plus a stereo audition.
"""
from pathlib import Path
import math
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/MusicSound/Resources/SFX'
RATE = 44100
sounds = {}

def noise(n, low, high, seed):
    f = np.fft.rfftfreq(n, 1/RATE)
    spectrum = np.fft.rfft(np.random.default_rng(seed).normal(size=n))
    band = np.exp(-(f/high)**4)*(1-np.exp(-(f/low)**4))
    x = np.fft.irfft(spectrum*band, n)
    return x/max(1e-9, np.sqrt(np.mean(x*x)))

def times(seconds): return np.arange(round(RATE*seconds))/RATE

def save(name, x, peak, loop=False, release=.08):
    x = np.tanh(x)
    x *= peak/max(1e-9, np.max(np.abs(x)))
    if not loop:
        a, b = min(len(x), int(.006*RATE)), min(len(x), int(release*RATE))
        x[:a] *= np.sin(np.linspace(0, math.pi/2, a))**2
        x[-b:] *= np.cos(np.linspace(0, math.pi/2, b))**2
    else:
        assert abs(x[-1]-x[0]) < .04, f'Loop seam: {name}'
    assert np.isfinite(x).all() and np.max(np.abs(x)) < 1
    with wave.open(str(OUT/(name+'.wav')), 'wb') as file:
        file.setnchannels(1); file.setsampwidth(2); file.setframerate(RATE)
        file.writeframes((x*32767).astype('<i2').tobytes())
    sounds[name] = x
    print(f'{name}: {len(x)/RATE:.2f}s, peak {20*np.log10(max(1e-9,np.max(np.abs(x)))):.1f} dBFS')

t = times(4)
engine = .36*np.sin(2*np.pi*48*t)+.17*np.sin(2*np.pi*96*t+.3)+.07*np.sin(2*np.pi*144*t)
engine += .035*np.sin(2*np.pi*288*t)+.08*noise(len(t),100,1500,3101)
save('race_engine_loop', engine*(.9+.07*np.sin(2*np.pi*2*t)), .34, True)
save('race_wind_loop', noise(len(t),90,3600,3102)*(.8+.08*np.sin(2*np.pi*.5*t)), .3, True)
t = times(2)
save('race_turbo_loop', .16*np.sin(2*np.pi*186*t)+.06*np.sin(2*np.pi*372*t)+.13*noise(len(t),300,3200,3103), .34, True)
save('race_drift_loop', .15*noise(len(t),250,2500,3104)+.045*np.sin(2*np.pi*330*t+.35*np.sin(2*np.pi*3*t)), .32, True)

def sweep(t, start, end):
    return np.sin(2*np.pi*np.cumsum(np.linspace(start,end,len(t)))/RATE)

t = times(.55)
save('race_boost', (.17*sweep(t,130,840)+.17*noise(len(t),180,3300,3110))*np.sin(np.pi*t/.55)**1.4, .4)
t = times(.42)
save('race_drift_release', (.16*sweep(t,300,1050)+.10*noise(len(t),400,2600,3111))*np.exp(-t*6), .33)
t = times(.17)
alarm = np.zeros(len(t))
for start, frequency in ((.008, 980), (.086, 1320)):
    age = t-start
    pulse = (age>=0)&(age<.06)
    gate = np.sin(np.pi*np.clip(age/.06,0,1))**.6
    alarm += (.22*np.sin(2*np.pi*frequency*t)+.045*np.sin(2*np.pi*frequency*2*t))*gate*pulse
save('race_warning', alarm, .32, release=.012)
t = times(.16)
save('race_countdown', (.2*np.sin(2*np.pi*550*t)+.05*np.sin(2*np.pi*1100*t))*np.exp(-t*17), .3)
t = times(.45)
save('race_go', (.18*sweep(t,550,1100)+.08*np.sin(2*np.pi*825*t))*np.exp(-t*6), .36)
t = times(.38)
save('race_scrape', (.18*noise(len(t),500,2900,3112)+.09*np.sin(2*np.pi*710*t))*np.exp(-t*10), .35)
t = times(1.3)
crash = .31*sweep(t,125,42)*np.exp(-t*11)+.27*noise(len(t),80,3400,3113)*np.exp(-t*14)
for i, f in enumerate((211,379,677,1129)):
    crash += .15/(i+1)*np.sin(2*np.pi*f*t)*np.exp(-t*(5+i*2))
save('race_crash', crash, .6)
t = times(1.8)
lift = np.sin(np.pi*t/1.8)**1.5
save('race_launch', (.22*noise(len(t),80,4300,3114)+.16*sweep(t,90,680))*lift, .48)

def melody(name, notes, spacing, seconds):
    t = times(seconds); x = np.zeros(len(t))
    for i, frequency in enumerate(notes):
        age = np.maximum(t-i*spacing, 0)
        attack = 1-np.exp(-age*110)
        x += (.16*np.sin(2*np.pi*frequency*age)+.035*np.sin(2*np.pi*frequency*2*age))*attack*np.exp(-age*4.5)*(t>=i*spacing)
    save(name,x,.34)

melody('race_win',(523.25,659.25,783.99,1046.5),.14,1.4)
melody('race_lose',(392,329.63,261.63),.18,1.3)

# Audition: countdown, acceleration, turbo, drift release, contact, launch and finish.
preview = np.zeros((RATE*14,2))
def place(name, at, gain, pan=0, length=None):
    x = sounds[name]
    if length:
        x = np.tile(x, math.ceil(length*RATE/len(x)))[:round(length*RATE)].copy()
        fade = min(len(x)//2,int(.18*RATE))
        x[:fade] *= np.linspace(0,1,fade); x[-fade:] *= np.linspace(1,0,fade)
    n = min(len(x),len(preview)-int(at*RATE))
    angle = (pan+1)*math.pi/4
    preview[int(at*RATE):int(at*RATE)+n] += x[:n,None]*gain*np.array([math.cos(angle),math.sin(angle)])
for at in (.2,1.2,2.2): place('race_countdown',at,.45)
place('race_go',3.2,.45)
place('race_engine_loop',2.9,.2,length=9.5)
place('race_wind_loop',3.3,.09,length=8.9)
place('race_boost',4.5,.42)
place('race_turbo_loop',4.6,.12,length=1.4)
place('race_drift_loop',6.25,.16,length=1.1)
place('race_drift_release',7.35,.4)
place('race_scrape',8.4,.4,-.5)
place('race_warning',9.15,.4)
place('race_launch',10,.45)
place('race_win',12.2,.5)
assert np.max(np.abs(preview)) < 1
with wave.open(str(ROOT/'Tools/race_audio_preview.wav'),'wb') as file:
    file.setnchannels(2);file.setsampwidth(2);file.setframerate(RATE)
    file.writeframes((preview*32767).astype('<i2').tobytes())
print('Stereo audition: Tools/race_audio_preview.wav')
