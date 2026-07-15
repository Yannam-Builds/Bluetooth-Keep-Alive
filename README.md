<div align="center">
  <img src="app.ico" alt="Bluetooth Keep-Alive Logo" width="128" height="128">
  <h1>Bluetooth Keep-Alive</h1>
  <p><i>An ultra-lightweight, native Windows system tray utility that keeps your Bluetooth audio devices awake by silently streaming an inaudible high-frequency sine wave.</i></p>
</div>

---

## 🎧 The Problem
Many Bluetooth headphones, speakers, and soundbars have aggressive power-saving features. If there is a brief moment of silence during a video or between songs, the Bluetooth device enters a "standby" or "sleep" mode to save battery. When audio starts playing again, it takes the device a second or two to wake up and re-establish the connection, causing you to miss the beginning of the audio.

## 🚀 The Solution
**Bluetooth Keep-Alive** prevents your devices from going to sleep by continuously playing an inaudible, high-frequency sound (defaulting to 20 kHz) in the background. Because the sound is continuously streaming, your Bluetooth device remains fully awake, ensuring zero audio latency when you play media.

### Why is this unique?
While other solutions exist on GitHub, they often suffer from significant drawbacks:
- **Command-line only**: Many tools have no graphical interface and require editing launch arguments.
- **Bloated Electron apps**: Web-based wrappers that consume 100-200 MB of RAM just to play a silent wave.
- **Audio popping/clicking**: Scripts that loop `.wav` files frequently cause popping sounds at the seam of the loop.

**Bluetooth Keep-Alive** is built differently:
1. **Zero-Dependency Native App**: Built purely in C# (`WinForms` & `winmm.dll`). It consumes **<20 MB of RAM** and ~0% CPU. No .NET runtime installations or Python dependencies required.
2. **Perfect Sine Wave Generator**: It generates a mathematically perfect sine wave buffer in real-time, matching the cycle boundary to eliminate all pops and clicks when the loop repeats.
3. **Dynamic System Tray GUI**: You can adjust the frequency, volume, and even **hot-swap output devices** natively from your Windows taskbar.
4. **Robust & Polished**: It intelligently mutes itself when you lock your PC or put it to sleep (saving your Bluetooth battery), prevents duplicate instances, and automatically falls back to your default audio device if a custom device is disconnected.

## ✨ Features
* **System Tray Interface**: Clean, minimalist UI in the Windows taskbar.
* **Custom Frequency (18kHz - 22kHz)**: Easily adjust the frequency to ensure it's completely inaudible to you (and your pets!).
* **Volume Control**: Tune the amplitude to perfectly match your specific device's wake threshold.
* **Per-Device Routing**: Select exactly which audio output device should receive the keep-alive signal.
* **Smart Power Management**: Automatically pauses playback when Windows is locked or suspended.
* **Start on Boot**: A one-click toggle to automatically start the utility when Windows launches.

## 🛠️ Usage
1. Download the standalone `BluetoothKeepAlive.exe` from the Releases tab.
2. Run the executable. 
3. A minimalist speaker icon will appear in your system tray (near the clock).
4. Right-click the icon to:
   - Mute/Play the sound
   - Change the output frequency
   - Adjust the volume
   - Select your output device
   - Enable "Start on Windows Boot"

## 💻 Compiling From Source
If you prefer to build the app yourself, it's incredibly easy. No Visual Studio required! The app is designed to be compiled using the native C# compiler (`csc.exe`) that ships with Windows.

```powershell
# Run the included build script
.\build.ps1
```

## 📜 License
This project is open-source and free to use.
