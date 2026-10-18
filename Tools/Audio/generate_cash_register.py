"""Warm register: existing CC0 latch/relay recordings and an original struck-bell synthesis."""
from pathlib import Path
import wave
import numpy as np

root = Path(__file__).resolve().parents[2]
folder = root / "Assets/Audio/Sfx"
rate = 44100
audio = np.zeros(int(rate * .95))

def add_recording(name, at, gain):
    with wave.open(str(folder / name), "rb") as source:
        assert source.getsampwidth() == 2
        samples = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(float) / 32768
        samples = samples.reshape(-1, source.getnchannels()).mean(axis=1)
        samples = np.interp(np.arange(round(len(samples) * rate / source.getframerate())) / rate,
                            np.arange(len(samples)) / source.getframerate(), samples)
    start = round(at * rate)
    count = min(len(samples), len(audio) - start)
    audio[start:start + count] += samples[:count] * gain

add_recording("HoldLatch.wav", 0, 1.0)
add_recording("ConnectedRelay.wav", .055, .65)
add_recording("ConnectedRelay.wav", .23, .3)
t = np.arange(int(.78 * rate)) / rate
bell = np.zeros_like(t)
for frequency, gain, decay in [(1320, .28, .24), (2656, .10, .15), (3580, .035, .09)]:
    bell += gain * np.sin(2 * np.pi * frequency * t) * np.exp(-t / decay)
bell *= np.minimum(t / .002, 1)
start = round(.095 * rate)
audio[start:start + len(bell)] += bell
audio[-2205:] *= np.linspace(1, 0, 2205)
audio *= .82 / max(np.max(np.abs(audio)), 1e-9)
with wave.open(str(folder / "CashRegister.wav"), "wb") as output:
    output.setparams((1, 2, rate, 0, "NONE", "not compressed"))
    output.writeframes((audio * 32767).astype("<i2").tobytes())
print("CashRegister.wav: 0.95 seconds, mono 44100 Hz, peak 0.82")
