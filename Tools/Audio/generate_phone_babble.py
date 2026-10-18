"""Create the original, nonverbal telephone babble (no recordings or third-party samples)."""
from pathlib import Path
import wave
import numpy as np

rate = 44100
rng = np.random.default_rng(6018)
audio = np.zeros(rate * 8)
cursor = .06
vowels = [(500, 1100, 2300), (400, 1500, 2500), (650, 1300, 2400), (350, 850, 2200)]
while cursor < 7.6:
    for _ in range(rng.integers(3, 7)):
        duration = rng.uniform(.12, .24)
        t = np.arange(int(duration * rate)) / rate
        f0 = rng.uniform(145, 200) * (1 + .07 * np.sin(t * 11) - .12 * t / duration)
        phase = 2 * np.pi * np.cumsum(f0) / rate
        formants = np.array(vowels[rng.integers(len(vowels))]) * rng.uniform(.94, 1.06)
        syllable = np.zeros_like(t)
        for h in range(1, 22):
            weight = sum(g * np.exp(-.5 * ((h * f0 - f) / width) ** 2)
                         for f, width, g in zip(formants, [160, 220, 300], [1, .5, .15]))
            syllable += np.sin(h * phase + .12 * h) * weight / h ** .75
        envelope = np.minimum(t / .025, 1) * np.minimum((duration - t) / .045, 1)
        syllable = syllable * envelope * rng.uniform(.7, 1) + rng.normal(0, .018, len(t)) * envelope * np.exp(-t / .025)
        start = int(cursor * rate)
        count = min(len(syllable), len(audio) - start)
        if count <= 0:
            break
        audio[start:start + count] += syllable[:count]
        cursor += duration + rng.uniform(.018, .055)
    cursor += rng.uniform(.18, .38)
freq = np.fft.rfftfreq(len(audio), 1 / rate)
audio = np.fft.irfft(np.fft.rfft(audio) * (1 - np.exp(-(freq / 280) ** 4)) * np.exp(-(freq / 2600) ** 6), n=len(audio))
fade = int(.04 * rate)
audio[:fade] *= np.linspace(0, 1, fade)
audio[-fade:] *= np.linspace(1, 0, fade)
pcm = np.round(audio / np.max(np.abs(audio)) * .65 * 32767).astype('<i2')
path = Path(__file__).resolve().parents[2] / 'Assets/Audio/Sfx/PhoneBabble.wav'
with wave.open(str(path), 'wb') as output:
    output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
    output.writeframes(pcm.tobytes())
print(path)
