<div align="center">

<img src="assets/header.svg?v=3" alt="Bluetooth Keep-Alive — native Windows tray utility" width="100%">

<br>

<a href="https://github.com/Yannam-Builds/Bluetooth-Keep-Alive/releases"><img src="https://img.shields.io/badge/Download-Releases-ffffff?style=flat-square&labelColor=0d1117" alt="Download releases"></a>
<img src="https://img.shields.io/badge/Platform-Windows-ffffff?style=flat-square&labelColor=0d1117" alt="Windows">
<img src="https://img.shields.io/badge/App%20Type-System%20Tray-ffffff?style=flat-square&labelColor=0d1117" alt="System tray utility">
<img src="https://img.shields.io/badge/Stack-C%23%20WinForms-ffffff?style=flat-square&labelColor=0d1117" alt="C# WinForms">
<img src="https://img.shields.io/badge/License-MIT-ffffff?style=flat-square&labelColor=0d1117" alt="MIT License">

</div>

---

## 01 · What it solves

Bluetooth headphones, speakers, and soundbars often enter standby during short silent gaps. When sound resumes, the device wakes late and cuts off the beginning of the audio.

**Bluetooth Keep-Alive** keeps the audio path warm by generating a quiet high-frequency sine wave in the background. No visible window. No browser. No Electron wrapper. No looped WAV seam clicks.

## 02 · Why it is clean

| Area | Decision |
|---|---|
| **Runtime** | Single native Windows tray utility. No Electron or Python dependency. |
| **Audio path** | Generated sine-wave buffer instead of a looping audio file. |
| **Interface** | System tray only; no heavy foreground UI. |
| **Controls** | Frequency, volume, output device, mute/play, restart stream, and start-on-boot. |
| **Power behavior** | Pauses on lock/suspend and resumes after unlock/resume. |
| **Branding** | One self-contained logo source is used by the README artwork and Windows icon builder. |

<p align="center">
  <img src="assets/system-map.svg?v=3" alt="Bluetooth Keep-Alive system map" width="100%">
</p>

## 03 · Features

- **System tray control surface** — right-click the tray icon to manage the app.
- **Frequency range** — switch between `18 kHz`, `19 kHz`, `20 kHz`, `21 kHz`, and `22 kHz`.
- **Volume tuning** — choose from `1%`, `5%`, `10%`, `25%`, `50%`, or `100%`.
- **Per-device routing** — send the keep-alive signal to the default output device or a selected device.
- **Smart power handling** — pause on system lock/suspend to avoid wasting Bluetooth battery.
- **Start on Windows boot** — optional registry-based startup toggle.

## 04 · Usage

1. Download `BluetoothKeepAlive.exe` from **Releases**.
2. Run it.
3. Look for the white keep-alive logo in the Windows system tray.
4. Right-click the icon to adjust frequency, volume, output device, startup behavior, or mute/play state.

## 05 · Build from source

No Visual Studio project is required. The build script generates the multi-resolution Windows icon from the same supplied logo silhouette and embeds it into the executable.

```powershell
.\build.ps1
```

## 06 · Project layout

```text
Bluetooth-Keep-Alive/
├─ Program.cs                 # Native tray app, audio engine, routing and settings
├─ build.ps1                  # Generates app.ico and compiles the executable
├─ tools/
│  └─ IconBuilder.cs          # Multi-resolution Windows icon generator
├─ assets/
│  ├─ logo.svg                # Supplied keep-alive mark
│  ├─ header.svg              # Self-contained repository header
│  └─ system-map.svg          # Self-contained signal-flow panel
├─ LICENSE
└─ README.md
```

---

<div align="center">

<img src="assets/logo.svg?v=3" alt="Bluetooth Keep-Alive logo" width="170">

<br>

<sub>1 silent stream a day keeps the Bluetooth awake.</sub>

</div>
