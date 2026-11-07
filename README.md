# NEON ASTEROIDS

**A synthwave reimagining of the classic arcade shooter, built in Unity 6 (URP) with custom HLSL shaders.**

![Unity](https://img.shields.io/badge/Unity-6000.3-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/URP-HLSL%20shaders-8a2be2)
![Platforms](https://img.shields.io/badge/Platforms-Windows%20%7C%20Android-1af2ff)

### ▶ [Play it on itch.io](https://loneretr0.itch.io/neon-asteroids)

---

## About

Pilot a vector ship under a neon sunset, break asteroids into smaller and faster pieces, and survive endless waves for the high score.
Every visual in the game (ship, asteroids, debris and background) is generated at runtime from code. There are no sprites.

## Features

- Classic Asteroids gameplay with inertia-based movement and screen wrapping
- Neon look: HDR line rendering, bloom, chromatic aberration and a CRT screen effect
- Game feel: camera shake, hit-stop, shattering vector debris and impact "kicks" on the post-processing
- Hyperspace jump with visual feedback (beam, closing reticle, shockwave) so the player can find the ship instantly
- Endless waves that scale in count and speed, extra life every 10,000 points, persistent high score
- Pause menu, keyboard / gamepad / touch support, Windows and Android builds

## Technical highlights

### Custom shaders (URP, HLSL)
| Shader | What it does |
|---|---|
| `NeonUnlit` | Additive line shader that pushes vertex colors into HDR with a soft white core, so URP's Bloom turns `LineRenderer`s into neon tubes. |
| `SynthwaveBackground` | Fully procedural backdrop: gradient sky, twinkling hashed stars, striped scrolling sun and a perspective grid anti-aliased with `fwidth`. |
| `CRTFullscreen` | Full Screen Pass Renderer Feature: barrel curvature, scanlines, RGB phosphor mask, chromatic split, noise, flicker, rolling band and vignette. A global shader property drives a glitch effect on death and hyperspace. |

### Rendering and game feel
- Post-processing `Volume` built at runtime; bloom and chromatic aberration are "kicked" on impacts and decay back to a baseline.
- Perlin-noise camera shake and hit-stop through `Time.timeScale`, with UI and effects running on unscaled time.
- Asteroids are procedural polygons; when destroyed, each segment becomes an independent piece of debris.

### Audio
- Sound effects are **synthesized at runtime** (`AudioClip.Create`): oscillators, filtered noise, pitch sweeps and envelopes. No audio files are needed.
- Anti-fatigue design: random pitch and volume variation, multiple variants per sound and per-sound rate limiting.
- Music playlist with **crossfade** between two `AudioSource`s, a shuffle bag that never repeats a track back to back, and a low-pass filter while paused.
- Handles focus loss and app suspension without desyncing or doubling tracks.

### Spawning and fairness
- Wrap-aware distance checks, so new waves never spawn on top of the player, even across screen edges.
- Respawn picks the safest point on a grid, scoring clearance against both current and predicted asteroid positions.

### Input and mobile
- New Input System with a legacy Input Manager fallback.
- Touch controls: floating virtual joystick (aim and thrust), hold-to-fire half of the screen and hyperspace / pause buttons.
- Safe area support for notches, landscape-only orientation, 60 FPS target and auto-pause when the app loses focus.

## Controls

| Action | Keyboard | Gamepad | Touch |
|---|---|---|---|
| Rotate / aim | A / D or ← / → | Left stick | Left side (joystick) |
| Thrust | W or ↑ | Left stick up / LT | Push the joystick past halfway |
| Shoot | Space or J | RT or A | Hold the right side |
| Hyperspace | Left Shift or K | B | HYPER button |
| Pause | Esc or P | Start | II button |

## Running the project

1. Clone the repository and open it with **Unity 6000.3** (Universal Render Pipeline).
2. Open `Assets/Scenes/Game.unity` and press Play.

The whole game is driven by a single component, `NeonAsteroidsGame`, which builds the camera, background and post-processing at startup. The HUD and pause menu live in a regular Canvas in the scene.

> **Music is not included** in this repository due to licensing. The game runs without it: sound effects are synthesized, and you can add your own tracks to the **Music Tracks** list of the `NeonAudio` component.

## Project structure

```
Assets/NeonAsteroids/
├── Scripts/
│   ├── NeonAsteroidsGame.cs   Game states, waves, collisions, scoring, safe spawning
│   ├── PlayerShip.cs          Movement, shooting, hyperspace, invulnerability
│   ├── Asteroid.cs            Procedural asteroids
│   ├── Bullet.cs              Bullets with neon trails
│   ├── NeonDebris.cs          Shatter and spark effects
│   ├── NeonFx.cs              Rings and beams (hyperspace feedback)
│   ├── PostFXController.cs    Runtime Volume, kicks, shake, hit-stop, CRT glitch
│   ├── NeonAudio.cs           Synthesized SFX and crossfading playlist
│   ├── GameHUD.cs             Canvas HUD and pause menu (TextMeshPro)
│   ├── SafeArea.cs            Fits the HUD to notches and rounded corners
│   ├── NeonInput.cs           Keyboard, gamepad and touch abstraction
│   ├── MobileControls.cs      Virtual joystick and touch buttons
│   ├── NeonFactory.cs         Palette and LineRenderer factory
│   ├── ScreenWrapper.cs       Screen wrapping
│   └── CursorAutoHide.cs      Hides the cursor when idle
├── Editor/
│   └── HUDBuilder.cs          One-click tool that builds the HUD Canvas
└── Shaders/
    ├── NeonUnlit.shader
    ├── SynthwaveBackground.shader
    └── CRTFullscreen.shader
```

## Credits

- **Game:** LoneRetr0
- **Music (not included):** Free SynthWave Music Pack by VOiD1 Gaming
- **Fonts:** [Audiowide](https://fonts.google.com/specimen/Audiowide) and [Orbitron](https://fonts.google.com/specimen/Orbitron), SIL Open Font License
