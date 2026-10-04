# Procedural Sound Generator — Copilot Planning Brief

## Purpose

Create a Unity Editor tool that procedurally generates short game sound effects from mathematical synthesis rather than recorded audio.

The intended workflow is:

1. Open an EditorWindow from the Unity toolbar.
2. Select a sound preset such as `Rift Open`, `Spell Impact`, `UI Click`, or `Lightning Zap`.
3. Adjust parameters with sliders.
4. Preview the generated sound inside the editor.
5. Randomise variants using a seed.
6. Save the chosen result as a `.wav` file under `Assets/Audio/Generated/`.
7. Import and use the saved file as a normal Unity audio asset.

The tool should prioritise practical stylised MMORPG sound effects rather than physically accurate acoustic modelling.

---

## High-Level Requirements

### Must Have

- Unity EditorWindow for controlling sound generation.
- Procedural PCM sample generation in C#.
- Preview playback in the editor.
- Export to `.wav` files.
- Presets for common game audio types.
- Deterministic randomisation using a seed.
- No external audio middleware required.
- Generated sounds should be usable as normal Unity assets.

### Should Have

- Layer-based synthesis model.
- Envelope controls: attack, decay, sustain, release or simplified attack/decay.
- Pitch sweep support.
- Noise generation.
- Basic filtering.
- Basic distortion/saturation.
- One-click variant generation.
- Save/load sound recipes as ScriptableObjects.

### Nice To Have Later

- Spectrogram or waveform preview.
- Batch-generate multiple variants.
- Loop-safe ambient/rift sounds.
- Stereo panning and width.
- Simple convolution/reverb approximation.
- Integration with spell definitions.
- AudioMixerGroup assignment after import.

---

## Suggested Folder Structure

```text
Assets/
  Editor/
    ProceduralAudio/
      ProceduralSoundGeneratorWindow.cs
      ProceduralSoundPreviewUtility.cs

  Scripts/
    Audio/
      Procedural/
        SoundRecipe.cs
        SoundPresetType.cs
        SoundSynthesizer.cs
        SynthesisLayer.cs
        Oscillator.cs
        Envelope.cs
        NoiseGenerator.cs
        SimpleFilter.cs
        Distortion.cs
        WavWriter.cs

  Audio/
    Generated/
```

If the project has a different convention for runtime/editor separation, follow that convention, but keep editor-only code out of runtime assemblies.

---

## Core Concepts

The synthesiser should generate floating-point PCM samples in the range `-1.0f` to `1.0f`.

A sound is built by summing one or more synthesis layers:

```text
finalSample = layer1 + layer2 + layer3 + ...
finalSample = applyMasterEnvelope(finalSample)
finalSample = applyDistortion(finalSample)
finalSample = normaliseOrLimit(finalSample)
```

Each layer can contain:

- Waveform type.
- Base frequency.
- Frequency sweep.
- Amplitude.
- Envelope.
- Noise amount.
- Filter settings.
- Random variation.

---

## Main Data Types

### `SoundPresetType.cs`

```csharp
public enum SoundPresetType
{
    UiClick,
    UiHover,
    SpellCastStart,
    SpellImpact,
    RiftOpen,
    RiftIdleLoop,
    RiftClose,
    FireBurst,
    LightningZap,
    VoidPulse,
    WeaponHit,
    Footstep
}
```

---

### `WaveformType.cs`

```csharp
public enum WaveformType
{
    Sine,
    Square,
    Saw,
    Triangle,
    WhiteNoise,
    PinkNoise
}
```

---

### `SoundRecipe.cs`

Make this a `ScriptableObject` so recipes can be saved and reused.

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Procedural Sound Recipe")]
public sealed class SoundRecipe : ScriptableObject
{
    public SoundPresetType presetType;
    public int seed = 12345;
    public int sampleRate = 44100;
    public float duration = 1.0f;
    public float masterVolume = 0.8f;
    public bool normalise = true;
    public List<SynthesisLayer> layers = new();
}
```

---

### `SynthesisLayer.cs`

```csharp
using System;
using UnityEngine;

[Serializable]
public sealed class SynthesisLayer
{
    public string name = "Layer";
    public WaveformType waveform = WaveformType.Sine;

    [Min(0f)] public float amplitude = 0.5f;
    [Min(0f)] public float frequency = 440f;

    public bool usePitchSweep = false;
    public float startFrequency = 440f;
    public float endFrequency = 220f;
    public AnimationCurve pitchCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public Envelope envelope = new();

    public bool useLowPass = false;
    public float lowPassCutoff = 8000f;

    public bool useHighPass = false;
    public float highPassCutoff = 80f;

    public bool useDistortion = false;
    public float distortionAmount = 0.25f;

    public float randomAmplitude = 0f;
    public float randomFrequency = 0f;
}
```

---

### `Envelope.cs`

Start with a simple attack-decay envelope. ADSR can be added later.

```csharp
using System;
using UnityEngine;

[Serializable]
public sealed class Envelope
{
    [Min(0f)] public float attack = 0.01f;
    [Min(0f)] public float decay = 0.3f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public float Evaluate(float time, float duration)
    {
        if (duration <= 0f)
            return 0f;

        if (attack > 0f && time < attack)
            return Mathf.Clamp01(time / attack);

        float decayStart = attack;
        float decayDuration = Mathf.Max(0.0001f, duration - decayStart);
        float t = Mathf.Clamp01((time - decayStart) / decayDuration);
        return Mathf.Clamp01(curve.Evaluate(t));
    }
}
```

---

## Synthesiser Responsibilities

### `SoundSynthesizer.cs`

Responsibilities:

- Convert a `SoundRecipe` into a `float[]` sample buffer.
- Use deterministic `System.Random` seeded by `recipe.seed`.
- Support mono initially.
- Clamp or normalise the result.
- Avoid allocations inside the per-sample loop where practical.

Suggested public API:

```csharp
public static class SoundSynthesizer
{
    public static float[] GenerateMono(SoundRecipe recipe);
    public static AudioClip GenerateAudioClip(SoundRecipe recipe, string clipName);
}
```

Generation outline:

```csharp
int sampleCount = Mathf.CeilToInt(recipe.duration * recipe.sampleRate);
float[] samples = new float[sampleCount];

for each layer:
    for each sample index:
        time = i / sampleRate
        normalisedTime = time / duration
        frequency = evaluate pitch sweep
        waveformSample = oscillator sample
        envelope = layer.envelope.Evaluate(time, duration)
        sample += waveformSample * envelope * amplitude

apply master volume
normalise if enabled
return samples
```

---

## Oscillator Requirements

### `Oscillator.cs`

Support:

- Sine.
- Square.
- Saw.
- Triangle.
- White noise.
- Pink noise later if desired.

Suggested API:

```csharp
public static class Oscillator
{
    public static float Sample(
        WaveformType waveform,
        float phase,
        System.Random random);
}
```

Important implementation note:

Use phase accumulation rather than recalculating simple `sin(2πft)` for everything if pitch sweeps are used.

Pseudo-code:

```text
phase += frequency / sampleRate
phase %= 1
sample = waveform(phase)
```

Waveform formulas:

```text
Sine:      sin(phase * 2π)
Square:    phase < 0.5 ? 1 : -1
Saw:       2 * phase - 1
Triangle:  1 - 4 * abs(round(phase - 0.25) - (phase - 0.25))
Noise:     random -1..1
```

---

## WAV Export

### `WavWriter.cs`

Responsibilities:

- Write 16-bit PCM WAV files.
- Accept mono float samples.
- Clamp samples to `[-1, 1]`.
- Convert to signed 16-bit integers.
- Write RIFF/WAVE header correctly.

Suggested API:

```csharp
public static class WavWriter
{
    public static void WriteMono16(string path, float[] samples, int sampleRate);
}
```

Output path convention:

```text
Assets/Audio/Generated/{PresetName}_{Seed}.wav
```

After writing:

```csharp
AssetDatabase.ImportAsset(path);
AssetDatabase.Refresh();
```

Editor-only code using `AssetDatabase` must remain inside an `Editor` folder or editor assembly.

---

## Editor Window Requirements

### `ProceduralSoundGeneratorWindow.cs`

Menu item:

```csharp
[MenuItem("Tools/Audio/Procedural Sound Generator")]
```

The window should include:

- Preset dropdown.
- Seed field.
- Duration slider.
- Master volume slider.
- Generate button.
- Preview button.
- Randomise Seed button.
- Save WAV button.
- Layer list UI.

Minimum viable interface:

```text
Preset: [Rift Open v]
Seed: [12345] [Randomise]
Duration: [-----]
Volume: [-----]

[Generate]
[Preview]
[Save WAV]

Layers
  Layer 1: Low Hum
  Layer 2: Noise Burst
  Layer 3: Pitch Sweep
```

Use IMGUI initially. UI Toolkit can be used later if desired.

---

## Editor Preview Playback

Unity does not expose all editor audio preview helpers as stable public APIs across versions. Use one of these approaches:

### Option A — Temporary GameObject Preview

Create a hidden temporary `GameObject` with an `AudioSource`, play the generated clip, then destroy the object after playback.

This is simple and robust enough for the first version.

### Option B — Reflection-Based Editor Preview

Use UnityEditor internal audio preview methods through reflection.

This is less stable and should not be the first implementation unless needed.

Recommended first implementation: **Option A**.

---

## Preset Design

Presets should create default recipes. The user can then modify the generated layers manually.

### `UiClick`

Purpose: short interface click.

Suggested layers:

```text
Layer 1: sine wave, 1200 Hz, 0.05s, fast decay
Layer 2: tiny white-noise transient, 0.015s
```

### `SpellImpact`

Purpose: generic magical hit.

```text
Layer 1: low sine thump, 90 → 45 Hz, 0.4s
Layer 2: white noise burst, 0.2s
Layer 3: bright sine ping, 900 → 500 Hz, 0.25s
```

### `RiftOpen`

Purpose: unstable portal/rift opening.

```text
Layer 1: low sine hum, 55 → 80 Hz, 1.2s
Layer 2: detuned saw/sine sweep, 300 → 900 Hz, 0.8s
Layer 3: airy noise tail, high-passed, 1.5s
Layer 4: short click transient at start
```

### `RiftIdleLoop`

Purpose: looping rift ambience.

```text
Layer 1: low sine drone, slow amplitude modulation
Layer 2: filtered noise shimmer
Layer 3: subtle detuned tone
```

Looping can be implemented later. For now, generate a non-looping preview.

### `LightningZap`

Purpose: sharp electrical spell.

```text
Layer 1: white noise burst, very short attack and decay
Layer 2: high sine sweep, 3000 → 600 Hz, 0.15s
Layer 3: crackle impulses generated randomly
```

### `FireBurst`

Purpose: flame spell or explosion.

```text
Layer 1: low thump, 100 → 50 Hz
Layer 2: filtered noise burst
Layer 3: crackle particles/noise impulses
```

### `VoidPulse`

Purpose: dark unstable magic.

```text
Layer 1: sub sine, 45 Hz
Layer 2: descending pitch sweep, 500 → 80 Hz
Layer 3: distorted low-mid saw
Layer 4: reversed-feeling fade-in envelope
```

---

## Implementation Phases

### Phase 1 — Minimal Working Generator

Goal: generate and save simple WAV files.

Tasks:

- Create `SoundPresetType`.
- Create `WaveformType`.
- Create `Envelope`.
- Create `SynthesisLayer`.
- Create `SoundRecipe`.
- Create `Oscillator`.
- Create `SoundSynthesizer.GenerateMono()`.
- Create `WavWriter.WriteMono16()`.
- Create minimal EditorWindow with Generate and Save buttons.

Acceptance criteria:

- User can open the tool from `Tools/Audio/Procedural Sound Generator`.
- User can generate a sine click.
- User can save a valid `.wav` file.
- Saved file appears in Unity under `Assets/Audio/Generated/`.
- The file can be dragged onto an `AudioSource` and played.

---

### Phase 2 — Preview and Presets

Goal: make the tool useful inside the editor.

Tasks:

- Add preview playback.
- Add preset dropdown.
- Add default recipe generation per preset.
- Add random seed field.
- Add Randomise button.
- Add master volume and duration controls.

Acceptance criteria:

- User can select `RiftOpen`, click Generate, and hear a rift-like sound.
- Randomising the seed changes the sound.
- Reusing the same seed reproduces the same sound.
- User can save the generated result.

---

### Phase 3 — Layer Editing

Goal: expose enough control to design sounds manually.

Tasks:

- Show editable list of synthesis layers.
- Allow adding/removing layers.
- Expose waveform, frequency, amplitude, envelope, and pitch sweep settings.
- Add per-layer enable/disable toggle.

Acceptance criteria:

- User can create a new sound from scratch using layers.
- User can modify a preset and save the result.
- Disabled layers are ignored during generation.

---

### Phase 4 — Polish

Goal: make generated SFX less synthetic.

Tasks:

- Add simple one-pole low-pass filter.
- Add simple one-pole high-pass filter.
- Add tanh distortion/saturation.
- Add amplitude modulation for drones.
- Add random impulse/crackle generator.
- Add fade-in/fade-out safety to avoid clicks.
- Add normalisation toggle.

Acceptance criteria:

- Fire sounds have convincing noisy texture.
- Lightning sounds have sharp crackle.
- Rift sounds have unstable movement rather than static tones.
- No generated file clips harshly unless distortion is intentionally high.

---

### Phase 5 — Recipe Assets

Goal: allow reusable sound recipes.

Tasks:

- Save current recipe as a `SoundRecipe` asset.
- Load existing recipe assets into the window.
- Add `Generate From Recipe` command.
- Add batch generation of variants.

Acceptance criteria:

- User can save `RiftOpenRecipe.asset`.
- User can reopen the window later and load the recipe.
- User can generate multiple WAV variants from the same recipe.

---

## Important Implementation Notes

### Avoid Runtime Dependency on Editor Classes

Anything using `UnityEditor`, `EditorWindow`, `AssetDatabase`, or `MenuItem` must be inside an Editor folder or editor-only assembly definition.

Runtime-safe classes:

```text
SoundRecipe
SynthesisLayer
Envelope
Oscillator
SoundSynthesizer
WavWriter, if it does not use AssetDatabase
```

Editor-only classes:

```text
ProceduralSoundGeneratorWindow
ProceduralSoundPreviewUtility
Asset import helpers
```

---

### Avoid Clicks

Clicks happen when a waveform starts or ends abruptly away from zero.

Add a very short safety fade:

```text
fade in:  2–5 ms
fade out: 2–10 ms
```

This should be applied globally after synthesis.

---

### Normalisation

After summing layers, find the largest absolute sample value.

If it exceeds `1.0`, divide all samples by that value.

Optionally normalise to `0.95` instead of `1.0` to avoid inter-sample clipping.

---

### Deterministic Randomness

Use `System.Random(recipe.seed)`.

Do not use `UnityEngine.Random` unless its state is explicitly managed.

Same recipe + same seed should produce the same samples.

---

### Sample Rate

Default to:

```text
44100 Hz
```

Allow later support for:

```text
22050 Hz
48000 Hz
```

---

## Initial Preset Parameter Suggestions

### Rift Open

```text
Duration: 1.4s
Master Volume: 0.85

Layer: Low Rift Hum
Waveform: Sine
Frequency: 48 → 72 Hz
Amplitude: 0.45
Attack: 0.05
Decay: 1.3
Distortion: light

Layer: Arcane Sweep
Waveform: Saw or Triangle
Frequency: 220 → 880 Hz
Amplitude: 0.18
Attack: 0.01
Decay: 0.8
Distortion: medium

Layer: Air Tear
Waveform: WhiteNoise
Amplitude: 0.25
Attack: 0.02
Decay: 1.2
High-pass: enabled

Layer: Opening Click
Waveform: WhiteNoise
Amplitude: 0.8
Attack: 0.001
Decay: 0.04
High-pass: enabled
```

### Spell Impact

```text
Duration: 0.6s
Master Volume: 0.9

Layer: Impact Thump
Waveform: Sine
Frequency: 110 → 45 Hz
Amplitude: 0.7
Attack: 0.001
Decay: 0.35

Layer: Magic Crack
Waveform: WhiteNoise
Amplitude: 0.45
Attack: 0.001
Decay: 0.12

Layer: Resonant Ping
Waveform: Sine
Frequency: 700 → 350 Hz
Amplitude: 0.25
Attack: 0.005
Decay: 0.4
```

### UI Click

```text
Duration: 0.08s
Master Volume: 0.5

Layer: Click Tone
Waveform: Sine
Frequency: 1200 → 800 Hz
Amplitude: 0.6
Attack: 0.001
Decay: 0.06

Layer: Tick
Waveform: WhiteNoise
Amplitude: 0.2
Attack: 0.001
Decay: 0.015
```

---

## Suggested Copilot Prompt

Use this prompt when asking Copilot to implement the first version:

```text
Implement a Unity procedural sound generator Editor tool.

Requirements:
- Add an EditorWindow available at Tools/Audio/Procedural Sound Generator.
- Generate mono PCM audio samples procedurally in C#.
- Support SoundPresetType, WaveformType, SoundRecipe, SynthesisLayer, Envelope, Oscillator, SoundSynthesizer, and WavWriter.
- Support sine, square, saw, triangle, and white noise.
- Support simple attack/decay envelope.
- Support pitch sweep from startFrequency to endFrequency.
- Generate an AudioClip for preview.
- Save generated audio as 16-bit PCM WAV under Assets/Audio/Generated/.
- Use AssetDatabase.Refresh/ImportAsset only from editor code.
- Keep editor code in Assets/Editor/ProceduralAudio/.
- Keep reusable synthesis code outside Editor folders.
- Use deterministic System.Random seeded by the recipe seed.
- Avoid placeholders. Provide complete compiling C# files.

Start with a minimal implementation that includes presets for UiClick, SpellImpact, RiftOpen, FireBurst, LightningZap, and VoidPulse.
```

---

## Common Failure Cases To Avoid

- Do not put `using UnityEditor;` in runtime scripts.
- Do not save generated files outside the Unity project without asking.
- Do not use unsupported compressed audio formats for export in the first version.
- Do not generate samples outside `[-1, 1]` without clamping or normalisation.
- Do not use non-deterministic randomness for seeded generation.
- Do not make the first implementation depend on AudioMixer, Timeline, FMOD, Wwise, or external packages.
- Do not attempt realistic orchestral/music synthesis in the first pass.

---

## Definition of Done for Version 1

Version 1 is complete when:

- The tool opens from the Unity toolbar.
- A preset can be selected.
- A sound can be generated.
- The sound can be previewed.
- A `.wav` file can be saved into the project.
- The `.wav` imports into Unity automatically.
- The imported file plays correctly through a normal `AudioSource`.
- The same seed produces the same result.
- No editor-only APIs leak into runtime scripts.
