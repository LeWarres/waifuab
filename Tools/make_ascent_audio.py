"""Original synthesized flight SFX. Requires numpy; writes 44.1 kHz, 16-bit PCM WAVs.
No recordings or third-party samples are used. Fixed seeds make regeneration reproducible.
"""
import math
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/MusicSound/Resources/SFX'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 44100

def noise(n, low, high, seed):
    rng = np.random.default_rng(seed)
    f = np.fft.rfftfreq(n, 1 / RATE)
    spectrum = np.fft.rfft(rng.normal(size=n))
    band = np.exp(-(f / high)**4) * (1 - np.exp(-(f / low)**4))
    sound = np.fft.irfft(spectrum * band, n)
    return sound / max(1e-9, np.sqrt(np.mean(sound**2)))

def envelope(sound, attack=.006, release=.06):
    result = sound.copy()
    a, r = min(len(result), int(attack * RATE)), min(len(result), int(release * RATE))
    result[:a] *= np.sin(np.linspace(0, math.pi/2, a))**2
    result[-r:] *= np.cos(np.linspace(0, math.pi/2, r))**2
    return result

def master(sound, peak):
    result = np.tanh(sound)
    return result * peak / max(1e-9, np.max(np.abs(result)))

def save(path, sound):
    assert np.isfinite(sound).all() and np.max(np.abs(sound)) < 1, path
    channels = 1 if sound.ndim == 1 else sound.shape[1]
    with wave.open(str(path), 'wb') as file:
        file.setnchannels(channels)
        file.setsampwidth(2)
        file.setframerate(RATE)
        file.writeframes((sound * 32767).astype('<i2').tobytes())
    print(f'{path.name}: {len(sound)/RATE:.2f}s, peak {20*np.log10(max(1e-9,np.max(np.abs(sound)))):.1f} dBFS')

# Every oscillator and modulation completes an integer number of cycles in four seconds.
# FFT-filtered noise is periodic as well, so the loop needs no silent seam or recurring fade.
t = np.arange(RATE * 4) / RATE
pump = .85 + .08*np.sin(2*np.pi*.75*t) + .05*np.sin(2*np.pi*3*t)
hum = .4*np.sin(2*np.pi*62*t) + .15*np.sin(2*np.pi*124*t + .4)
hum += .055*np.sin(2*np.pi*186*t) + .025*np.sin(2*np.pi*372*t)
air = .09*noise(len(t), 140, 1900, 801)
engine = master((hum + air)*pump, .32)
assert abs(engine[-1] - engine[0]) < .025, 'Loop discontinuity'
save(OUT / 'ascent_propulsion.wav', engine)

passes, impacts = [], []
for variant, seconds in enumerate((.72, .81, .66), 1):
    t = np.arange(round(RATE * seconds)) / RATE
    u = t / seconds
    sweep = np.exp(-((u - .48)/.22)**2)
    air = noise(len(t), 180, 3700, 900+variant)
    flutter = noise(len(t), 650, 1900, 930+variant)
    frequency = 780 - variant*40 - 470*u
    phase = 2*np.pi*np.cumsum(frequency)/RATE
    metal = np.sin(phase) * np.exp(-((u-.52)/.13)**2)
    sound = envelope(sweep*(.34*air + .07*flutter) + .025*metal, .025, .11)
    sound = master(sound, .48)
    passes.append(sound)
    save(OUT / f'ascent_pass_{variant}.wav', sound)

for variant in range(1, 4):
    seconds = 1.02 + variant*.035
    t = np.arange(round(RATE*seconds)) / RATE
    metal = np.zeros(len(t))
    for i, f in enumerate((173, 317, 563, 941, 1459)):
        metal += (.24/(1+i*.65))*np.sin(2*np.pi*f*(.94+variant*.025)*t + i*.2)*np.exp(-t*(5.5+i*2.1))
    phase = 2*np.pi*np.cumsum(62+100*np.exp(-t*25))/RATE
    body = .28*np.sin(phase)*np.exp(-t*17)
    contact = .22*noise(len(t), 260, 3500, 1000+variant)*np.exp(-t*65)
    rattle = np.zeros(len(t))
    grit = noise(len(t), 700, 2900, 1050+variant)
    for i, at in enumerate((.052, .105, .174, .25)):
        age = np.maximum(t-at, 0)
        rattle += .055/(1+i*.7)*grit*np.exp(-age*100)*(t>=at)
    sound = master(envelope(metal + body + contact + rattle, .004, .12), .62)
    impacts.append(sound)
    save(OUT / f'ascent_impact_{variant}.wav', sound)

# Short audition at the same approximate balance as the in-game sources.
preview = np.zeros((RATE*8, 2))
bed = np.tile(engine, 2)
bed *= np.minimum(np.arange(len(bed))/int(.35*RATE), 1)
bed *= np.minimum(np.arange(len(bed))[::-1]/int(.6*RATE), 1)
preview += bed[:, None]*.22
def place(sound, at, gain, pan=0):
    first = int(at*RATE)
    size = min(len(sound), len(preview)-first)
    angle = (pan+1)*math.pi/4
    preview[first:first+size, 0] += sound[:size]*gain*math.cos(angle)
    preview[first:first+size, 1] += sound[:size]*gain*math.sin(angle)
place(passes[0], 1.25, .34, -.75)
place(passes[1], 2.5, .34, .8)
place(impacts[0], 4, .55)
place(passes[2], 5.3, .3, -.4)
place(impacts[2], 6.3, .55)
save(ROOT / 'Tools/ascenso_audio_preview.wav', preview)
