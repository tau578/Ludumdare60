"""Render shop cues from existing CC0 mechanisms and original synthesized tones.

Requires numpy. Run from any directory; outputs deterministic mono 48 kHz PCM WAVs.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "Assets/Audio/Sfx"
RATE = 48000


def mechanism(name):
    with wave.open(str(SFX / name)) as source:
        assert source.getsampwidth() == 2 and source.getnchannels() == 1
        signal = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(float) / 32768
        if source.getframerate() != RATE:
            signal = np.interp(np.arange(round(len(signal) * RATE / source.getframerate())) / RATE,
                               np.arange(len(signal)) / source.getframerate(), signal)
    return signal


def place(track, signal, seconds, gain=1):
    start = round(seconds * RATE)
    count = min(len(signal), len(track) - start)
    track[start:start + count] += signal[:count] * gain


def tone(hz, seconds, decay, tremolo=False):
    t = np.arange(round(seconds * RATE)) / RATE
    # A struck bar: fundamental plus rapidly fading, non-harmonic upper modes.
    signal = (np.sin(2 * np.pi * hz * t) * np.exp(-t / decay)
              + .22 * np.sin(2 * np.pi * hz * 3.99 * t) * np.exp(-t / .12)
              + .045 * np.sin(2 * np.pi * hz * 10 * t) * np.exp(-t / .035))
    signal *= 1 - np.exp(-t / .004)
    if tremolo:
        signal *= .90 + .10 * np.cos(2 * np.pi * 5.2 * t)
    signal *= np.minimum(1, np.maximum(0, (seconds - t) / .12))
    return signal


def save(name, signal, peak):
    fade = min(240, len(signal) // 2)
    signal[:fade] *= np.linspace(0, 1, fade)
    signal[-fade:] *= np.linspace(1, 0, fade)
    signal *= peak / max(1e-9, np.max(np.abs(signal)))
    with wave.open(str(SFX / name), "wb") as target:
        target.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        target.writeframes((signal * 32767).astype("<i2").tobytes())
    print(f"{name}: {len(signal)/RATE:.2f}s, peak {np.max(np.abs(signal)):.3f}")


purchase = np.zeros(round(.48 * RATE))
place(purchase, mechanism("ConnectedRelay.wav"), 0, .8)
place(purchase, tone(1396.91, .38, .085), .09, .15)
save("ShopPurchase.wav", purchase, .80)

# A padded pair of knocks, with no electronic buzzer.
refusal = np.zeros(round(.24 * RATE))
rng = np.random.default_rng(60)
for at, gain in [(0, .3), (.105, .24)]:
    t = np.arange(round(.12 * RATE)) / RATE
    noise = np.convolve(rng.normal(0, 1, len(t)), np.ones(20) / 20, mode="same")
    # Midrange harmonics keep the padded knock audible on laptop speakers.
    knock = (np.sin(2 * np.pi * 320 * t) + .3 * np.sin(2 * np.pi * 640 * t)
             + .3 * noise) * np.exp(-t / .032)
    knock *= 1 - np.exp(-t / .0015)
    place(refusal, knock, at, gain)
save("ShopInsufficientFunds.wav", refusal, .85)

secret = np.zeros(round(2.05 * RATE))
place(secret, mechanism("HoldLatch.wav"), 0, 1)
for hz, at, gain in [(587.33, .16, .27), (698.46, .49, .25), (880.0, .82, .23)]:
    note = tone(hz, 2.05 - at, .40, tremolo=True)
    place(secret, note, at, gain)
    place(secret, note, at + .09, gain * .12)
    place(secret, note, at + .18, gain * .06)
save("SecretUpgrade.wav", secret, .80)
