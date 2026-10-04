"""Original Tidebreak v0.5 score and effects. No samples or external recordings.

Deterministic additive/modal/noise synthesis, arranged on a bar grid. Musical tails
wrap around the loop rather than fading the entire mix to silence at the seam.
Run with the project Python (numpy + scipy); writes redistributable PCM WAVs.
"""
from pathlib import Path
import json
import math
import wave
import numpy as np
from scipy.signal import butter, sosfilt

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Tidebreak/Assets/Resources/Audio"
REPORT = ROOT / "Artifacts/audio-v05-audit.json"
SR = 32000
TAU = 2 * np.pi
RNG = np.random.default_rng(5005)
audit = []


def filtered_noise(length, low=0, high=4200):
    data = RNG.standard_normal(length)
    if low and high:
        sos = butter(2, [low, high], btype="bandpass", fs=SR, output="sos")
    else:
        sos = butter(2, high, btype="lowpass", fs=SR, output="sos")
    return sosfilt(sos, data)


def note(midi, duration, voice):
    t = np.arange(max(1, int(duration * SR))) / SR
    f = 440 * 2 ** ((midi - 69) / 12)
    release = np.minimum(1, (duration - t) / .22)
    if voice == "plucked":
        signal = sum(np.sin(TAU * f * n * t) * np.exp(-t * (3.1 + n * .6)) / n ** 1.5
                     for n in range(1, 7))
        envelope = np.minimum(1, t / .005) * release
    elif voice == "marimba":
        signal = (np.sin(TAU * f * t) * np.exp(-t * 3.5)
                  + .28 * np.sin(TAU * f * 3.99 * t) * np.exp(-t * 12)
                  + .07 * np.sin(TAU * f * 9.4 * t) * np.exp(-t * 22))
        envelope = np.minimum(1, t / .003) * release
    elif voice == "flute":
        phase = TAU * f * t + .07 * np.sin(TAU * 4.3 * t) * np.minimum(1, t * 3)
        signal = np.sin(phase) + .1 * np.sin(phase * 2) + filtered_noise(len(t), 600, 3500) * .075
        envelope = np.minimum(1, t / .095) * np.minimum(1, (duration - t) / .25)
    elif voice == "strings":
        signal = sum((np.sin(TAU * f * n * 1.002 * t + .03 * np.sin(TAU * 3 * t))
                      + np.sin(TAU * f * n * .998 * t)) / (2 * n ** 1.7) for n in range(1, 6))
        envelope = np.minimum(1, t / .45) * np.minimum(1, (duration - t) / .8)
    elif voice == "reed":
        phase = TAU * f * t + .05 * np.sin(TAU * 5 * t)
        signal = np.sin(phase) + .22 * np.sin(phase * 3) + .075 * np.sin(phase * 5)
        envelope = np.minimum(1, t / .035) * np.minimum(1, (duration - t) / .16)
    elif voice == "bell":
        signal = sum(np.sin(TAU * f * ratio * t) * np.exp(-t * decay) * amp
                     for ratio, decay, amp in [(1, 2, 1), (2.756, 3.5, .27), (5.404, 5, .08)])
        envelope = np.minimum(1, t / .004) * release
    else:  # rounded bass, without an ear-fatiguing sawtooth
        signal = np.sin(TAU * f * t) + .18 * np.sin(TAU * f * 2 * t)
        envelope = np.minimum(1, t / .015) * np.exp(-t * 1.5) * release
    return signal * envelope


def drum(kind, duration=.5):
    t = np.arange(int(duration * SR)) / SR
    edge = np.minimum(1, t / .002) * np.minimum(1, (duration - t) / .035)
    if kind == "kick":
        phase = TAU * (46 * t + 3.9 * (1 - np.exp(-t * 28)))
        data = np.sin(phase) * np.exp(-t * 10) + filtered_noise(len(t), 150, 1600) * np.exp(-t * 80) * .12
    elif kind == "tom":
        data = (np.sin(TAU * 83 * t) + .36 * np.sin(TAU * 131 * t)) * np.exp(-t * 8)
        data += filtered_noise(len(t), 350, 2200) * np.exp(-t * 35) * .25
    elif kind == "brush":
        data = filtered_noise(len(t), 500, 5800) * np.exp(-t * 20) * .65
    else:
        data = (filtered_noise(len(t), 900, 6500) * .6 + np.sin(TAU * 192 * t) * .1) * np.exp(-t * 31)
    return data * edge


def add(buffer, signal, seconds, gain, pan=0):
    start = int(seconds * SR)
    # Wrap note/reverb tails naturally through the bar-grid loop seam.
    indices = (start + np.arange(len(signal))) % len(buffer)
    left = math.sqrt((1 - pan) / 2)
    right = math.sqrt((1 + pan) / 2)
    np.add.at(buffer[:, 0], indices, signal * gain * left)
    np.add.at(buffer[:, 1], indices, signal * gain * right)


def save(relative, samples, loop=False, description="", bpm=None, target_rms=.14, peak_limit=.78):
    samples = np.asarray(samples, dtype=np.float64)
    if samples.ndim == 1:
        samples = np.column_stack((samples, samples))
    samples -= np.mean(samples, axis=0)
    if loop:
        # Correct only a 6 ms neighbourhood, retaining the wrapped musical tails.
        # This makes the PCM endpoints identical without an audible whole-loop dip.
        window = int(.006 * SR)
        delta = (samples[0] - samples[-1]) * .5
        ramp = np.linspace(1, 0, window)[:, None]
        samples[:window] -= ramp * delta
        samples[-window:] += ramp[::-1] * delta
    rms = float(np.sqrt(np.mean(samples * samples)))
    peak = float(np.max(np.abs(samples)))
    samples *= min(target_rms / max(rms, 1e-8), peak_limit / max(peak, 1e-8))
    if not loop:
        fade = max(2, int(SR * .004))
        samples[:fade] *= np.linspace(0, 1, fade)[:, None]
        samples[-fade:] *= np.linspace(1, 0, fade)[:, None]
    pcm = np.round(np.clip(samples, -.999, .999) * 32767).astype("<i2")
    path = OUT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    normalized = pcm.astype(float) / 32768
    seam = normalized[0] - normalized[-1]
    stat = {"file": path.relative_to(ROOT).as_posix(), "description": description, "sample_rate": SR,
            "channels": 2, "seconds": len(samples) / SR, "bpm": bpm, "loop": loop,
            "peak_dbfs": round(20 * np.log10(max(1e-9, np.max(np.abs(normalized)))), 3),
            "rms_dbfs": round(20 * np.log10(max(1e-9, np.sqrt(np.mean(normalized ** 2)))), 3),
            "dc_offset": np.mean(normalized, axis=0).tolist(),
            "loop_endpoint_jump": float(np.max(np.abs(seam))), "clipped_samples": int(np.sum(np.abs(pcm) >= 32767)),
            "bytes": path.stat().st_size}
    audit.append(stat)
    print(relative, round(stat["seconds"], 2), "seconds", stat["peak_dbfs"], "peak dBFS")


def score(name, bpm, mood, roots, mode, instrument, layer=False):
    beat = 60 / bpm
    bars = 16
    length = int(bars * 4 * beat * SR)
    mix = np.zeros((length, 2))
    rhythm = np.zeros_like(mix)
    # Original seven-note motif. The latter half answers instead of repeating an arpeggio.
    motif = [0, 7, 10, 14, 15, 14, 7, 2, 0, 5, 9, 12, 10, 7, 2, 0]
    for bar in range(bars):
        root = roots[(bar // 2) % len(roots)]
        start = bar * 4 * beat
        chord = [root + 12, root + mode[(bar // 2) % len(mode)], root + 19, root + 26]
        for index, midi in enumerate(chord):
            add(mix, note(midi, beat * 4.75, "strings"), start, .07 if mood < 2 else .085, (index - 1.5) * .22)
        bass_pattern = [0, 2.5] if mood < 2 else [0, 1.5, 2, 3.5]
        for count, offset in enumerate(bass_pattern):
            add(mix, note(root - (12 if mood == 4 else 0), beat * 1.5, "bass"), start + offset * beat, .14, -.04)
        # Humanized, deterministic offbeats are kept away from the actual loop boundary.
        for step in range(8 if mood >= 2 else 4):
            offset = step * (.5 if mood >= 2 else 1)
            degree = [0, 7, 12, 10, 7, 14, 12, 7][(step + bar) % 8]
            add(mix, note(root + 12 + degree, beat * 1.5, "plucked" if mood < 3 else "marimba"),
                start + offset * beat, .07 if mood < 2 else .085, -.38 if step % 2 else .38)
        if bar % 4 != 3:  # Leave a breath between statements.
            for phrase in range(2):
                midi = 62 + motif[(bar * 2 + phrase) % len(motif)] + (12 if mood == 0 else 0)
                if mood == 1:
                    midi -= 2
                add(mix, note(midi, beat * (1.7 if phrase == 0 else 1.4), instrument),
                    start + (phrase * 2 + .25) * beat, .1 if mood < 2 else .12, -.15)
        if bar % 4 == 3:
            add(mix, note(root + 31, beat * 3, "bell"), start + 2 * beat, .065, .45)
        if mood == 0:
            add(rhythm, drum("brush"), start + 3 * beat, .045, .2)
        elif mood == 1:
            for off in [1, 3]:
                add(rhythm, drum("brush"), start + off * beat, .07, -.3)
            add(rhythm, drum("tom"), start, .085, .2)
        else:
            for off in [0, 2] + ([3.5] if mood >= 3 and bar % 2 else []):
                add(rhythm, drum("kick", .65), start + off * beat, .28 if mood == 4 else .22)
            for off in [1, 3]:
                add(rhythm, drum("tom" if mood >= 3 else "snare", .55), start + off * beat, .18, .2)
            for off in np.arange(.5, 4, .5):
                add(rhythm, drum("brush", .2), start + off * beat, .07, -.3 if int(off * 2) % 2 else .3)
            if bar % 4 == 3:
                for off in [2.5, 3, 3.5, 3.75]:
                    add(rhythm, drum("tom", .5), start + off * beat, .115, (off - 3) * .4)
    dry = mix.copy()
    for delay, amount in [(.071, .10), (.149, .08), (.293, .045)]:
        mix += np.roll(dry[:, ::-1], int(delay * SR), axis=0) * amount
    if not layer:
        mix += rhythm
    save("Music/" + name + ".wav", mix, True, "Original 16-bar " + name, bpm, target_rms=.13)
    if layer:
        save("Music/" + name + "_drums.wav", rhythm, True, "Phase-responsive percussion companion", bpm, target_rms=.115)


def weapon(name):
    duration = {"shot": .58, "shotgun": .82, "harpoon": .78, "carbine": .31, "burst": .38, "arc": .7}[name]
    t = np.arange(int(SR * duration)) / SR
    low = filtered_noise(len(t), 0, 750)
    middle = filtered_noise(len(t), 450, 3800)
    top = filtered_noise(len(t), 1800, 6200)
    if name == "shot":
        signal = .44 * top * np.exp(-t * 155) + .52 * np.sin(TAU * 116 * t) * np.exp(-t * 27) + low * np.exp(-t * 9) * .38
        click = np.maximum(0, t - .11)
        signal += (t > .11) * .13 * np.sin(TAU * 780 * click) * np.exp(-click * 65)
    elif name == "shotgun":
        signal = .45 * middle * np.exp(-t * 35) + .78 * np.sin(TAU * 67 * t) * np.exp(-t * 14) + low * np.exp(-t * 5) * .6
        pump = np.maximum(0, t - .34)
        signal += (t > .34) * middle * .2 * np.exp(-pump * 27)
    elif name == "harpoon":
        signal = .26 * middle * np.exp(-t * 45) + .65 * np.sin(TAU * 154 * t) * np.exp(-t * 20)
        signal += np.sin(TAU * 382 * t) * np.exp(-t * 10) * .18 + filtered_noise(len(t), 600, 2400) * np.exp(-t * 7) * .13
        cable = np.maximum(0, t - .2)
        signal += (t > .2) * np.sin(TAU * 295 * cable) * np.exp(-cable * 14) * .12
    elif name == "carbine":
        signal = top * np.exp(-t * 140) * .32 + middle * np.exp(-t * 43) * .28 + np.sin(TAU * 174 * t) * np.exp(-t * 34) * .35
        signal += low * np.exp(-t * 17) * .2
    elif name == "burst":
        signal = middle * np.exp(-t * 90) * .4 + np.sin(TAU * 139 * t) * np.exp(-t * 25) * .42 + low * np.exp(-t * 12) * .24
        latch = np.maximum(0, t - .075)
        signal += (t > .075) * np.sin(TAU * 630 * latch) * np.exp(-latch * 52) * .13
    else:
        # Stable electrical partials, avoiding the former piercing negative sweep.
        signal = sum(np.sin(TAU * f * t) * a for f, a in [(195, .38), (391, .2), (587, .08)]) * np.exp(-t * 9)
        signal += middle * np.exp(-t * 60) * .18 + low * np.exp(-t * 15) * .2
    signal *= np.minimum(1, t / .0007)
    stereo = np.column_stack((signal, signal * .98 + np.roll(signal, int(.007 * SR)) * .05))
    save("Effects/" + name + ".wav", stereo, False, "Original layered " + name + " report", target_rms=.18, peak_limit=.78)


def cues():
    configs = {
        "ready": ("bell", [74], .32), "weak": ("bell", [81, 86], .23),
        "coin": ("bell", [83, 90], .4), "discovery": ("flute", [74, 81, 83, 86], 1.4),
        "heal": ("strings", [62, 69, 74], 1.0), "win": ("reed", [62, 69, 74, 78], 1.8),
        "lose": ("strings", [62, 60, 57], 1.6), "sonar": ("bell", [74], .85),
        "boss": ("bass", [26, 33, 38], 1.55), "bite": ("marimba", [77, 74], .38),
        "catch": ("marimba", [69, 74], .45), "select": ("plucked", [81], .12),
        "danger": ("marimba", [62, 62], .65), "heartbeat": ("bass", [26, 26], .65),
    }
    for name, (voice, pitches, duration) in configs.items():
        signal = np.zeros((int(SR * duration), 2))
        for i, pitch in enumerate(pitches):
            offset = i * duration * .55 / max(1, len(pitches))
            sound = note(pitch, duration - offset, voice)
            # Non-loop cue uses direct addition, never wraps a delayed event.
            at = int(offset * SR)
            count = min(len(sound), len(signal) - at)
            signal[at:at + count] += sound[:count, None] * .4
        save("Effects/" + name + ".wav", signal, False, "Original " + name + " cue", target_rms=.13)
    noises = {"impact": (.16, 110, 2400), "hurt": (.27, 45, 950), "step": (.18, 60, 1600),
              "reel": (.17, 750, 3200), "equip": (.3, 240, 3000), "reload": (.48, 180, 2800),
              "cast": (.38, 220, 2200), "splash": (.65, 80, 4400), "dash": (.31, 250, 2700),
              "beam": (.52, 380, 2400), "explosion": (1.1, 35, 1900), "freeze": (.62, 500, 4500),
              "steam": (.72, 350, 3400)}
    for name, (duration, low, high) in noises.items():
        t = np.arange(int(SR * duration)) / SR
        data = filtered_noise(len(t), low, high)
        decay = 4 if name in ("splash", "steam", "explosion") else 11
        envelope = np.minimum(1, t / .008) * np.exp(-t * decay)
        if name == "reload":
            envelope = np.exp(-t * 38) + (t > .24) * np.exp(-np.maximum(0, t - .24) * 37)
        if name == "reel":
            envelope *= .35 + .65 * (np.sin(t * TAU * 31) > 0)
        data *= envelope
        if name == "explosion":
            data += np.sin(TAU * 43 * t) * np.exp(-t * 6) * .38
        save("Effects/" + name + ".wav", data, False, "Original filtered " + name + " texture", target_rms=.14)


def sea():
    duration = 24
    size = SR * duration
    t = np.arange(size) / SR
    # A circular overlap avoids an audible discontinuity in the noise bed.
    noise = np.zeros((size, 2))
    for channel in range(2):
        raw = filtered_noise(size + SR, 65, 3200)
        noise[:, channel] = raw[:size]
        fade = np.linspace(0, 1, SR)
        noise[:SR, channel] = raw[size:size + SR] * (1 - fade) + raw[:SR] * fade
    wave_envelope = .45 + .22 * np.sin(TAU * t / 8) + .10 * np.sin(TAU * t / 3)
    noise *= wave_envelope[:, None]
    save("Ambience/coastal_air.wav", noise, True, "Original stereo surf / wind bed", target_rms=.11)


def preview():
    def load(path):
        with wave.open(str(path), "rb") as stream:
            return np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").reshape(-1, 2).astype(float) / 32768
    pieces = []
    chapters = []
    elapsed = 0
    for name in ("explore_act1", "explore_act2", "explore_act3", "combat", "boss", "legend"):
        data = load(OUT / "Music" / (name + ".wav"))[:8 * SR].copy()
        if name in ("boss", "legend"):
            data += load(OUT / "Music" / (name + "_drums.wav"))[:8 * SR] * .7
        fade = int(.35 * SR)
        data[:fade] *= np.linspace(0, 1, fade)[:, None]
        data[-fade:] *= np.linspace(1, 0, fade)[:, None]
        chapters.append({"seconds": elapsed, "section": name});pieces.append(data);elapsed += 8
    for name in ("shot", "shotgun", "harpoon", "carbine", "burst", "arc"):
        data = np.zeros((int(1.4 * SR), 2));sound = load(OUT / "Effects" / (name + ".wav"));data[:len(sound)] = sound * .7
        chapters.append({"seconds": round(elapsed, 1), "section": name});pieces.append(data);elapsed += 1.4
    data = np.concatenate(pieces);data *= min(1, .88 / np.max(np.abs(data)))
    target = ROOT / "Artifacts/AudioPreview/Tidebreak-v05-preview.wav";target.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(target), "wb") as stream:
        stream.setnchannels(2);stream.setsampwidth(2);stream.setframerate(SR);stream.writeframes(np.round(data * 32767).astype("<i2").tobytes())
    return {"file": target.relative_to(ROOT).as_posix(), "seconds": len(data) / SR, "chapters": chapters}


def audit_weapons():
    result = []
    for name in ("shot", "shotgun", "harpoon", "carbine", "burst", "arc"):
        with wave.open(str(OUT / "Effects" / (name + ".wav")), "rb") as stream:
            data = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").reshape(-1, 2).mean(1) / 32768
        magnitude = np.abs(np.fft.rfft(data))
        frequencies = np.fft.rfftfreq(len(data), 1 / SR)
        centroid = float(np.sum(magnitude * frequencies) / np.sum(magnitude))
        early = float(np.sum(data[:int(.05 * SR)] ** 2) / np.sum(data ** 2))
        result.append({"weapon": name, "centroid_hz": round(centroid, 1),
                       "first_50ms_energy_ratio": round(early, 3), "seconds": len(data) / SR})
    return result


if __name__ == "__main__":
    score("explore_act1", 72, 0, [38, 34, 41, 36], [15, 16, 15, 16], "flute")
    score("explore_act2", 82, 1, [38, 36, 34, 33], [15, 15, 16, 15], "marimba")
    score("explore_act3", 84, 2, [38, 34, 33, 36], [15, 16, 15, 15], "bell")
    score("combat", 104, 2, [38, 38, 34, 36], [15, 15, 16, 15], "reed")
    score("boss", 112, 3, [38, 34, 36, 33], [15, 16, 15, 15], "reed", True)
    score("legend", 126, 4, [38, 33, 34, 36], [15, 15, 16, 15], "strings", True)
    for name in ("shot", "shotgun", "harpoon", "carbine", "burst", "arc"):
        weapon(name)
    cues()
    sea()
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    preview_info = preview()
    REPORT.write_text(json.dumps({"provenance": "Original procedural composition and synthesis; no external samples", "preview": preview_info, "assets": audit, "weapon_timbre_audit": audit_weapons()}, indent=2), encoding="utf-8")
    assert all(item["clipped_samples"] == 0 for item in audit)
    assert all(item["peak_dbfs"] <= -2 for item in audit)
    assert all(item["loop_endpoint_jump"] == 0 for item in audit if item["loop"])
    print("Wrote", len(audit), "assets and", REPORT)
