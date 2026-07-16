using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

[assembly: AssemblyTitle("Bluetooth Keep-Alive")]
[assembly: AssemblyDescription("Keeps Bluetooth audio devices awake with a continuous high-frequency signal.")]
[assembly: AssemblyCompany("Yannam Builds")]
[assembly: AssemblyProduct("Bluetooth Keep-Alive")]
[assembly: AssemblyCopyright("Copyright © 2026 Yannam Builds")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace BluetoothKeepAlive
{
    static class Program
    {
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");

        [STAThread]
        static void Main()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                return; // App is already running
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (var context = new AppContext())
                {
                    Application.Run(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Bluetooth Keep-Alive", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }

    public class AppContext : ApplicationContext
    {
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem statusItem;
        private ToolStripMenuItem playToggleItem;
        private ToolStripMenuItem freqSubMenu;
        private ToolStripMenuItem volSubMenu;
        private ToolStripMenuItem deviceSubMenu;
        private ToolStripMenuItem startOnBootItem;

        private AudioPlayer player;
        private MessageWindow messageWindow;

        private int currentFrequency = 20000; // 20 kHz default
        private int currentVolume = 10;       // 10% volume default
        private int currentDeviceId = -1;     // WAVE_MAPPER default
        private bool isMuted = false;
        private bool isSystemSuspendedOrLocked = false;

        private IntPtr activeHIcon = IntPtr.Zero;
        private IntPtr mutedHIcon = IntPtr.Zero;
        private Icon activeIcon;
        private Icon mutedIcon;

        public AppContext()
        {
            player = new AudioPlayer();
            LoadSettings();

            // Create Icons
            CreateIcons();

            // Create NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Text = "Bluetooth Keep-Alive";
            notifyIcon.DoubleClick += (s, e) => TogglePlay();

            // Build Context Menu
            BuildMenu();
            notifyIcon.ContextMenuStrip = contextMenu;

            // Load and set startup checkbox state
            startOnBootItem.Checked = IsStartupEnabled();

            // Update UI Checked States
            UpdateMenuCheckedStates();

            // Start Audio
            if (!isMuted)
            {
                StartAudio();
            }

            // Create hidden message window for device change listening and set as MainForm to keep message loop alive
            messageWindow = new MessageWindow(OnDeviceChange);
            this.MainForm = messageWindow;

            // Subscribe to system sleep/lock events to automatically save Bluetooth device battery
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private void CreateIcons()
        {
            try
            {
                activeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                activeIcon = SystemIcons.Application;
            }

            try
            {
                Bitmap bmpMuted = activeIcon.ToBitmap();
                using (Graphics g = Graphics.FromImage(bmpMuted))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen redPen = new Pen(Color.FromArgb(255, 50, 50), Math.Max(2.0f, bmpMuted.Width / 8.0f)))
                    {
                        g.DrawLine(redPen, 0, 0, bmpMuted.Width, bmpMuted.Height);
                    }
                }
                mutedHIcon = bmpMuted.GetHicon();
                mutedIcon = Icon.FromHandle(mutedHIcon);
            }
            catch
            {
                mutedIcon = activeIcon;
            }
        }

        private void BuildMenu()
        {
            contextMenu = new ContextMenuStrip();

            // Status label (displays mode, disabled so it serves as header)
            statusItem = new ToolStripMenuItem("Status: Initializing...");
            statusItem.Enabled = false;
            statusItem.Font = new Font(statusItem.Font, FontStyle.Bold);
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Mute / Play toggle
            playToggleItem = new ToolStripMenuItem("Mute Sound");
            playToggleItem.Click += (s, e) => TogglePlay();
            contextMenu.Items.Add(playToggleItem);

            // Frequency sub-menu
            freqSubMenu = new ToolStripMenuItem("Frequency");
            int[] frequencies = { 18000, 19000, 20000, 21000, 22000 };
            foreach (int freq in frequencies)
            {
                ToolStripMenuItem item = new ToolStripMenuItem((freq / 1000).ToString() + " kHz");
                int f = freq;
                item.Click += (s, e) => SetFrequency(f);
                freqSubMenu.DropDownItems.Add(item);
            }
            contextMenu.Items.Add(freqSubMenu);

            // Volume sub-menu
            volSubMenu = new ToolStripMenuItem("Volume");
            int[] volumes = { 1, 5, 10, 25, 50, 100 };
            foreach (int vol in volumes)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(vol.ToString() + "%");
                int v = vol;
                item.Click += (s, e) => SetVolume(v);
                volSubMenu.DropDownItems.Add(item);
            }
            contextMenu.Items.Add(volSubMenu);

            // Device sub-menu
            deviceSubMenu = new ToolStripMenuItem("Output Device");
            PopulateDeviceMenu();
            contextMenu.Items.Add(deviceSubMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Restart stream menu item (manual re-sync if default changes)
            var restartItem = new ToolStripMenuItem("Restart Audio Stream");
            restartItem.Click += (s, e) => ForceRestartAudio();
            contextMenu.Items.Add(restartItem);

            // Start on boot option
            startOnBootItem = new ToolStripMenuItem("Start on Windows Boot");
            startOnBootItem.Click += (s, e) => ToggleStartup();
            contextMenu.Items.Add(startOnBootItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit application
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApp();
            contextMenu.Items.Add(exitItem);
        }

        private void UpdateMenuCheckedStates()
        {
            // Update Frequency subitems
            foreach (ToolStripMenuItem item in freqSubMenu.DropDownItems)
            {
                int itemFreq = int.Parse(item.Text.Replace(" kHz", "")) * 1000;
                item.Checked = (itemFreq == currentFrequency);
            }

            // Update Volume subitems
            foreach (ToolStripMenuItem item in volSubMenu.DropDownItems)
            {
                int itemVol = int.Parse(item.Text.Replace("%", ""));
                item.Checked = (itemVol == currentVolume);
            }

            // Update Device subitems (refreshing names and selection)
            PopulateDeviceMenu();

            // Update Play/Mute texts and icon
            if (isMuted)
            {
                playToggleItem.Text = "Play Sound";
                statusItem.Text = "Status: Muted";
                notifyIcon.Icon = mutedIcon;
            }
            else if (isSystemSuspendedOrLocked)
            {
                playToggleItem.Text = "Mute Sound";
                statusItem.Text = "Status: Locked / Suspended";
                notifyIcon.Icon = mutedIcon;
            }
            else
            {
                playToggleItem.Text = "Mute Sound";
                statusItem.Text = "Status: Active (" + (currentFrequency / 1000).ToString() + " kHz)";
                notifyIcon.Icon = activeIcon;
            }

            startOnBootItem.Checked = IsStartupEnabled();
        }

        private void StartAudio()
        {
            if (isSystemSuspendedOrLocked) return;

            try
            {
                player.Start(currentFrequency, currentVolume, currentDeviceId);
            }
            catch (Exception ex)
            {
                if (currentDeviceId != -1)
                {
                    currentDeviceId = -1;
                    SaveSettings();
                    PopulateDeviceMenu();
                    try
                    {
                        player.Start(currentFrequency, currentVolume, currentDeviceId);
                        return;
                    }
                    catch { }
                }
                notifyIcon.ShowBalloonTip(3000, "Audio Startup Failed", "Could not start audio stream: " + ex.Message, ToolTipIcon.Warning);
            }
        }

        private void ForceRestartAudio()
        {
            if (!isMuted && !isSystemSuspendedOrLocked)
            {
                player.Stop();
                StartAudio();
            }
        }

        private void TogglePlay()
        {
            isMuted = !isMuted;
            SaveSettings();
            UpdateMenuCheckedStates();

            if (isMuted)
            {
                player.Stop();
            }
            else
            {
                StartAudio();
            }
        }

        private void SetFrequency(int freq)
        {
            currentFrequency = freq;
            SaveSettings();
            UpdateMenuCheckedStates();
            if (!isMuted)
            {
                player.Stop();
                StartAudio();
            }
        }

        private void SetVolume(int vol)
        {
            currentVolume = vol;
            SaveSettings();
            UpdateMenuCheckedStates();
            if (!isMuted)
            {
                player.Stop();
                StartAudio();
            }
        }

        private void PopulateDeviceMenu()
        {
            deviceSubMenu.DropDownItems.Clear();
            var devices = AudioPlayer.GetOutputDevices();
            foreach (var dev in devices)
            {
                var item = new ToolStripMenuItem(dev.Value);
                int dId = dev.Key;
                item.Click += (s, e) => SetDevice(dId);
                item.Checked = (currentDeviceId == dId);
                deviceSubMenu.DropDownItems.Add(item);
            }
        }

        private void SetDevice(int deviceId)
        {
            currentDeviceId = deviceId;
            SaveSettings();
            PopulateDeviceMenu();
            if (!isMuted)
            {
                player.Stop();
                StartAudio();
            }
        }

        private void ToggleStartup()
        {
            try
            {
                bool nextState = !IsStartupEnabled();
                SetStartup(nextState);
                startOnBootItem.Checked = nextState;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to change startup setting: " + ex.Message, "Bluetooth Keep-Alive", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDeviceChange()
        {
            // Windows device changes can take a split second to settle.
            // Wait 1.5 seconds, then safely restart the stream so it goes to the new default device.
            if (!isMuted && !isSystemSuspendedOrLocked)
            {
                System.Windows.Forms.Timer restartTimer = new System.Windows.Forms.Timer();
                restartTimer.Interval = 1500;
                restartTimer.Tick += (s, e) => {
                    restartTimer.Stop();
                    restartTimer.Dispose();
                    PopulateDeviceMenu();
                    if (!isMuted && !isSystemSuspendedOrLocked)
                    {
                        ForceRestartAudio();
                    }
                };
                restartTimer.Start();
            }
        }

        // Suspend/Lock Event Handlers to protect battery life on remote devices
        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                isSystemSuspendedOrLocked = true;
                player.Stop();
                UpdateMenuCheckedStates();
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                isSystemSuspendedOrLocked = false;
                UpdateMenuCheckedStates();
                if (!isMuted)
                {
                    StartAudio();
                }
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                isSystemSuspendedOrLocked = true;
                player.Stop();
                UpdateMenuCheckedStates();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                isSystemSuspendedOrLocked = false;
                UpdateMenuCheckedStates();
                if (!isMuted)
                {
                    StartAudio();
                }
            }
        }

        // --- Settings / Registry operations ---
        private void LoadSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\BluetoothKeepAlive"))
            {
                if (key != null)
                {
                    currentFrequency = (int)key.GetValue("Frequency", 20000);
                    currentVolume = (int)key.GetValue("Volume", 10);
                    isMuted = ((int)key.GetValue("Muted", 0)) == 1;
                    currentDeviceId = (int)key.GetValue("DeviceId", -1);
                }
            }
        }

        private void SaveSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\BluetoothKeepAlive"))
            {
                if (key != null)
                {
                    key.SetValue("Frequency", currentFrequency);
                    key.SetValue("Volume", currentVolume);
                    key.SetValue("Muted", isMuted ? 1 : 0);
                    key.SetValue("DeviceId", currentDeviceId);
                }
            }
        }

        private bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                if (key != null)
                {
                    return key.GetValue("BluetoothKeepAlive") != null;
                }
            }
            return false;
        }

        private void SetStartup(bool startOnBoot)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (startOnBoot)
                    {
                        key.SetValue("BluetoothKeepAlive", "\"" + Application.ExecutablePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue("BluetoothKeepAlive", false);
                    }
                }
            }
        }

        private void ExitApp()
        {
            player.Stop();

            // Unsubscribe from system events
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;

            // Release GDI Icon Handles
            if (activeIcon != null) activeIcon.Dispose();
            if (activeHIcon != IntPtr.Zero) DestroyIcon(activeHIcon);
            if (mutedIcon != null) mutedIcon.Dispose();
            if (mutedHIcon != IntPtr.Zero) DestroyIcon(mutedHIcon);

            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            if (messageWindow != null)
            {
                messageWindow.Close();
                messageWindow.Dispose();
            }

            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ExitApp();
            }
            base.Dispose(disposing);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr handle);
    }

    // A hidden window class that catches WM_DEVICECHANGE messages.
    public class MessageWindow : Form
    {
        private const int WM_DEVICECHANGE = 0x0219;
        private Action onDeviceChange;

        public MessageWindow(Action onDeviceChange)
        {
            this.onDeviceChange = onDeviceChange;
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                Action handler = onDeviceChange;
                if (handler != null)
                {
                    handler();
                }
            }
            base.WndProc(ref m);
        }
    }

    // Zero-dependency direct WASAPI/winmm audio wrapper.
    public class AudioPlayer
    {
        private IntPtr hWaveOut = IntPtr.Zero;
        private IntPtr pWaveHeader = IntPtr.Zero;
        private GCHandle pinnedData;
        private short[] audioBuffer;

        // WaveOut Multimedia API Imports
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int waveOutReset(IntPtr hWaveOut);

        // Windows Multimedia Constants
        private const int WHDR_BEGINLOOP = 0x00000004;
        private const int WHDR_ENDLOOP = 0x00000008;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutGetDevCaps(IntPtr uDeviceID, ref WAVEOUTCAPS pwoc, uint cbwoc);

        public static List<KeyValuePair<int, string>> GetOutputDevices()
        {
            var devices = new List<KeyValuePair<int, string>>();
            devices.Add(new KeyValuePair<int, string>(-1, "Default Device"));
            
            uint numDevs = waveOutGetNumDevs();
            for (int i = 0; i < (int)numDevs; i++)
            {
                WAVEOUTCAPS caps = new WAVEOUTCAPS();
                if (waveOutGetDevCaps((IntPtr)i, ref caps, (uint)Marshal.SizeOf(typeof(WAVEOUTCAPS))) == 0)
                {
                    devices.Add(new KeyValuePair<int, string>(i, caps.szPname));
                }
            }
            return devices;
        }

        public void Start(int frequency, int volumePercent, int deviceId = -1)
        {
            Stop();

            // Set up format: 48000 Hz, 16-bit, Mono PCM
            int sampleRate = 48000;
            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag = 1; // WAVE_FORMAT_PCM
            format.nChannels = 1;
            format.nSamplesPerSec = sampleRate;
            format.wBitsPerSample = 16;
            format.nBlockAlign = (short)(format.nChannels * (format.wBitsPerSample / 8));
            format.nAvgBytesPerSec = format.nSamplesPerSec * format.nBlockAlign;
            format.cbSize = 0;

            // Open WaveOut
            int result = waveOutOpen(out hWaveOut, deviceId, ref format, IntPtr.Zero, IntPtr.Zero, 0);
            if (result != 0)
            {
                throw new Exception("Failed to open waveOut device (Error: " + result + ")");
            }

            // Generate Sine Wave Buffer
            // 4800 samples is 0.1 seconds at 48000Hz.
            // Any integer kHz frequency (18, 19, 20, 21, 22 kHz) will fit an exact integer number of cycles in 4800 samples,
            // preventing any boundary click/pop noise during looping.
            int bufferSizeSamples = 4800;
            audioBuffer = new short[bufferSizeSamples];
            double amplitude = 32767.0 * (volumePercent / 100.0);

            for (int i = 0; i < bufferSizeSamples; i++)
            {
                double time = (double)i / sampleRate;
                audioBuffer[i] = (short)(amplitude * Math.Sin(2.0 * Math.PI * frequency * time));
            }

            // Pin buffer memory so garbage collector does not move it
            pinnedData = GCHandle.Alloc(audioBuffer, GCHandleType.Pinned);

            // Allocate unmanaged memory for WAVEHDR to prevent garbage collection and moving of header pointer in memory
            pWaveHeader = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WAVEHDR)));

            WAVEHDR waveHeader = new WAVEHDR();
            waveHeader.lpData = pinnedData.AddrOfPinnedObject();
            waveHeader.dwBufferLength = bufferSizeSamples * 2; // short is 2 bytes
            waveHeader.dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
            waveHeader.dwLoops = -1; // -1 means infinite loops (0xFFFFFFFF)

            // Marshal struct into unmanaged memory
            Marshal.StructureToPtr(waveHeader, pWaveHeader, false);

            // Prepare header
            result = waveOutPrepareHeader(hWaveOut, pWaveHeader, Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                Stop();
                throw new Exception("Failed to prepare audio header (Error: " + result + ")");
            }

            // Play stream
            result = waveOutWrite(hWaveOut, pWaveHeader, Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                Stop();
                throw new Exception("Failed to start audio playback (Error: " + result + ")");
            }
        }

        public void Stop()
        {
            if (hWaveOut != IntPtr.Zero)
            {
                waveOutReset(hWaveOut);
                if (pWaveHeader != IntPtr.Zero)
                {
                    waveOutUnprepareHeader(hWaveOut, pWaveHeader, Marshal.SizeOf(typeof(WAVEHDR)));
                }
                waveOutClose(hWaveOut);
                hWaveOut = IntPtr.Zero;
            }

            if (pWaveHeader != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pWaveHeader);
                pWaveHeader = IntPtr.Zero;
            }

            if (pinnedData.IsAllocated)
            {
                pinnedData.Free();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR
    {
        public IntPtr lpData;
        public int dwBufferLength;
        public int dwBytesRecorded;
        public IntPtr dwUser;
        public int dwFlags;
        public int dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }
}
