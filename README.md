<p align="center">
  <img src="./assets/hero-v3.svg" width="100%" alt="Bluetooth Keep-Alive — native Windows tray utility">
</p>

<p align="center">
  <a href="https://github.com/Yannam-Builds/Bluetooth-Keep-Alive/releases/latest/download/BluetoothKeepAlive.exe"><img alt="Download Bluetooth Keep-Alive" src="https://img.shields.io/badge/DOWNLOAD-1.0-0969da?style=for-the-badge&logo=windows11&logoColor=white"></a>
  <a href="https://github.com/Yannam-Builds/Bluetooth-Keep-Alive/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Yannam-Builds/Bluetooth-Keep-Alive?display_name=tag&style=for-the-badge&label=RELEASE&color=238636"></a>
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/WINDOWS-10_%2F_11-1f6feb?style=for-the-badge">
  <a href="./LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/LICENSE-MIT-8b949e?style=for-the-badge"></a>
</p>

<p align="center">
  <strong>Keep Bluetooth audio ready for the first millisecond—not the second one.</strong><br>
  A tiny native tray app that prevents aggressive speaker and headphone standby with a continuous, pop-free high-frequency signal.
</p>

---

## The signal that stays out of your way

<p align="center">
  <img src="./assets/sine-lab-v3.svg" width="100%" alt="Animated sine-wave laboratory showing the Bluetooth keep-alive signal, sample playhead, selectable frequency, and seamless loop">
</p>

The stream is deliberately simple: one clean sine wave, a very low configurable amplitude, and an exact loop boundary. Choose 18–22 kHz and the app regenerates the buffer immediately—no audio files, codecs, or network activity involved.

## Silence should not disconnect your sound

Bluetooth speakers, soundbars, and headphones often enter standby during quiet moments. When audio returns, the device needs time to wake up, so dialogue, alerts, and the start of songs get clipped.

Bluetooth Keep-Alive maintains a minimal audio stream on the output you choose. It stays out of the way, remembers its settings, and automatically pauses when Windows locks or sleeps.

<table>
  <tr>
    <td width="50%">
      <strong>Native and tiny</strong><br>
      C# 5, WinForms, and <code>winmm.dll</code>. No Electron, background service, installer, or bundled runtime.
    </td>
    <td width="50%">
      <strong>Pop-free looping</strong><br>
      A 4,800-sample buffer closes on an exact cycle boundary, preventing clicks at the loop seam.
    </td>
  </tr>
  <tr>
    <td width="50%">
      <strong>Per-device routing</strong><br>
      Send the keep-alive stream to a specific output and fall back to the Windows default if it disconnects.
    </td>
    <td width="50%">
      <strong>Power-aware</strong><br>
      Playback stops on lock or suspend, then resumes safely when the session returns.
    </td>
  </tr>
</table>

## Install

1. Download [`BluetoothKeepAlive.exe`](https://github.com/Yannam-Builds/Bluetooth-Keep-Alive/releases/latest/download/BluetoothKeepAlive.exe).
2. Run it—there is no installer.
3. Find the wireless icon in the Windows system tray and right-click it to configure playback.

> [!NOTE]
> Start with the lowest volume. Frequencies near 18 kHz may still be audible to some people and animals, depending on hearing range and hardware.

## Controls

| Control | What it does |
| --- | --- |
| **Mute Sound / Play Sound** | Stops or resumes the keep-alive stream. |
| **Frequency** | Selects 18, 19, 20, 21, or 22 kHz. |
| **Volume** | Sets the stream amplitude from 1% to 100%. |
| **Output Device** | Routes the signal to the default output or a specific device. |
| **Restart Audio Stream** | Reopens the selected output after a routing change. |
| **Start on Windows Boot** | Adds or removes the app from the current user's startup list. |

## How it works

<p align="center">
  <img src="./assets/signal-flow-v3.svg" width="100%" alt="Animated signal flow from the sine-wave buffer through winmm to the Bluetooth output">
</p>

The app generates 0.1 seconds of 48 kHz, 16-bit mono PCM audio. At each selectable integer-kilohertz frequency, the 4,800-sample buffer contains a whole number of sine-wave cycles. The final sample therefore meets the first cleanly when `waveOut` loops the buffer.

Device names and IDs come from `waveOutGetDevCaps`. If a selected output disappears, the stream retries through `WAVE_MAPPER`, Windows' default-device route.

## Built for the tray

- **Single instance:** a named mutex prevents duplicate background streams.
- **Persistent settings:** frequency, volume, mute state, and device selection live under `HKCU\Software\BluetoothKeepAlive`.
- **Session aware:** Windows lock, unlock, suspend, and resume events control playback.
- **Private by default:** no accounts, network requests, analytics, or telemetry.
- **Portable:** delete the executable to remove the app; disable **Start on Windows Boot** first if enabled.

## Build from source

No Visual Studio project is required. On Windows 10 or 11, run:

```powershell
.\build.ps1
```

The script embeds the supplied, hand-optimized multi-resolution `app.ico` and compiles the portable tray executable. If the icon is missing, the included builder can regenerate it from the canonical logo silhouette.

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Repository layout:

```text
Program.cs             Tray UI, settings, power events, and audio engine
build.ps1              Portable application build script
app.ico                Supplied 16–256px executable and tray icon
tools/IconBuilder.cs   Deterministic fallback icon generator
assets/logo.svg        Canonical vector mark
assets/logo.png        Supplied transparent 1024px master
assets/*.svg           Animated README and social artwork
```

## Release

The current stable build is [Bluetooth Keep-Alive 1.0](https://github.com/Yannam-Builds/Bluetooth-Keep-Alive/releases/tag/1.0).

## License

Released under the [MIT License](./LICENSE).

<p align="center">
  <img src="./assets/live-status-v3.svg" width="100%" alt="Animated status footer: signal active, native Windows utility, zero dependencies and zero telemetry">
</p>
