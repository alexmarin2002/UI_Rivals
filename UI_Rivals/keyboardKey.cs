using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace UI_Rivals
{
    public class KeyboardKey2
    {
        private MainWindow mainWindow;

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        public int toggleSwitch = 0;

        // Common virtual key codes
        public const int VK_W = 0x57;
        public const int VK_A = 0x41;
        public const int VK_S = 0x53;
        public const int VK_D = 0x44;

        #endregion

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _keyboardProc;
        private IntPtr _hookID = IntPtr.Zero;

        // Dictionary to track key states
        private Dictionary<uint, bool> _keyStates = new Dictionary<uint, bool>();
        private readonly object _keyLock = new object();

        // Track the last time a key was processed to prevent key repeat flooding
        private Dictionary<uint, long> _lastKeyProcessTime = new Dictionary<uint, long>();
        private const int KEY_REPEAT_DELAY_MS = 200; // Adjust as needed

        // Custom key tracking for toggle keys
        private List<int> _registeredToggleKeys = new List<int>();
        private List<int> _registeredToggleKeysDelay = new List<int>();

        // Delegate and event for global key detection
        public delegate void GlobalKeyPressedHandler(int keyCode);
        public event GlobalKeyPressedHandler OnGlobalKeyPressed;

        // Store tracked hex value
        public int TrackedKeyCode { get; private set; } = 0;
        public int TrackedKeyCodeDelay { get; private set; } = 0;

        // Flag to enable/disable key detection printing
        public bool PrintKeyDetection { get; set; } = true;


        private bool _isTrackedKeyDelayPressed = false;
        private DateTime _trackedKeyDelayPressTime = DateTime.MinValue;
        private const int LONG_PRESS_THRESHOLD_MS = 500; // Adjust as needed for long press detection

        // Public properties to expose the key press state
        public bool IsTrackedKeyDelayPressed => _isTrackedKeyDelayPressed;
        public bool IsTrackedKeyDelayLongPressed =>
            _isTrackedKeyDelayPressed &&
            (DateTime.Now - _trackedKeyDelayPressTime).TotalMilliseconds >= LONG_PRESS_THRESHOLD_MS;


        // Singleton instance
        private static KeyboardKey2 _instance;
        public static KeyboardKey2 Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KeyboardKey2();
                }
                return _instance;
            }
        }

        public KeyboardKey2()
        {
            // Initialize key states for WASD
            lock (_keyLock)
            {
                _keyStates[VK_W] = false;
                _keyStates[VK_A] = false;
                _keyStates[VK_S] = false;
                _keyStates[VK_D] = false;
            }
        }

        /// <summary>
        /// Set a specific key to track globally
        /// </summary>
        /// <param name="keyCode">The virtual key code to track</param>
        public void SetTrackedKey(int keyCode)
        {
            TrackedKeyCode = keyCode;
            Debug.WriteLine($"Now tracking key with virtual code: 0x{keyCode:X}");

            // Also register this key for toggle detection
            RegisterToggleKey(keyCode);
        }

        public void SetTrackedKeyDelay(int keyCodeDelay)
        {
            TrackedKeyCodeDelay = keyCodeDelay;
            Debug.WriteLine($"Now tracking key with virtual code: 0x{keyCodeDelay:X}");

            // Also register this key for toggle detection
            RegisterToggleKeyDelay(keyCodeDelay);
        }

        /// <summary>
        /// Initialize the keyboard hook
        /// </summary>
        public bool Initialize()
        {
            try
            {
                // Create the keyboard hook procedure
                _keyboardProc = new LowLevelKeyboardProc(KeyboardHookCallback);

                // Get module handle for the current process
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    // Set the keyboard hook
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }

                if (_hookID == IntPtr.Zero)
                {
                    Debug.WriteLine($"Failed to set keyboard hook. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }
                else
                {
                    Debug.WriteLine("Keyboard hook successfully installed");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up keyboard hook: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Register a key to be globally monitored
        /// </summary>
        public void RegisterToggleKey(int keyCode)
        {
            if (!_registeredToggleKeys.Contains(keyCode))
            {
                _registeredToggleKeys.Add(keyCode);
                Debug.WriteLine($"Registered toggle key: 0x{keyCode:X}");
            }
        }

        public void RegisterToggleKeyDelay(int keyCodeDelay)
        {
            if (!_registeredToggleKeysDelay.Contains(keyCodeDelay))
            {
                _registeredToggleKeysDelay.Add(keyCodeDelay);
                Debug.WriteLine($"Registered toggle key: 0x{keyCodeDelay:X}");
            }
        }


        /// <summary>
        /// Check if the tracked delay key is currently pressed
        /// </summary>
        public bool IsKeyDelayPressed()
        {
            return IsTrackedKeyDelayPressed;
        }

        /// <summary>
        /// Check if the tracked delay key is currently long-pressed
        /// </summary>
        public bool IsKeyDelayLongPressed()
        {
            return IsTrackedKeyDelayLongPressed;
        }

        /// <summary>
        /// Check if WASD movement keys are released
        /// </summary>
        public bool AreMovementKeysReleased()
        {
            lock (_keyLock)
            {
                // Check if any WASD keys are pressed according to our global hook
                bool isWPressed = _keyStates[VK_W];
                bool isAPressed = _keyStates[VK_A];
                bool isSPressed = _keyStates[VK_S];
                bool isDPressed = _keyStates[VK_D];

                // Return true if all keys are released
                return !(isWPressed || isAPressed || isSPressed || isDPressed);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                uint vkCode = hookStruct.vkCode;
                int keyCodeInt = (int)vkCode;

                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                // Process WASD keys for movement tracking
                if (vkCode == VK_W || vkCode == VK_A || vkCode == VK_S || vkCode == VK_D)
                {
                    lock (_keyLock)
                    {
                        _keyStates[vkCode] = isKeyDown;
                    }
                }

                // Track key state for TrackedKeyCodeDelay
                if (TrackedKeyCodeDelay != 0 && keyCodeInt == TrackedKeyCodeDelay)
                {
                    if (isKeyDown && !_isTrackedKeyDelayPressed)
                    {
                        _isTrackedKeyDelayPressed = true;
                        _trackedKeyDelayPressTime = DateTime.Now;
                    }
                    else if (isKeyUp)
                    {
                        _isTrackedKeyDelayPressed = false;
                    }
                }


                // Only process KeyDown events for tracked and toggle keys
                if (isKeyDown)
                {
                    long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    bool shouldProcess = true;

                    // Check if we should process this key (prevent rapid repeats)
                    lock (_keyLock)
                    {
                        if (_lastKeyProcessTime.ContainsKey(vkCode))
                        {
                            long lastTime = _lastKeyProcessTime[vkCode];
                            if (currentTime - lastTime < KEY_REPEAT_DELAY_MS)
                            {
                                // Don't process this key press, it's too soon after the last one
                                shouldProcess = false;
                            }
                        }

                        if (shouldProcess)
                        {
                            _lastKeyProcessTime[vkCode] = currentTime;
                        }
                    }

                    if (shouldProcess)
                    {
                        // Handle tracked key
                        if (TrackedKeyCode != 0 && keyCodeInt == TrackedKeyCode)
                        {
                            if (PrintKeyDetection)
                            {
                                Debug.WriteLine($"KEY DETECTED: 0x{keyCodeInt:X}");

                                // Cache the MainWindow reference to avoid multiple lookups
                                if (mainWindow == null && Application.Current != null)
                                {
                                    mainWindow = (MainWindow)Application.Current.MainWindow;
                                }

                                if (mainWindow != null)
                                {
                                    // Use priority higher than Background for more responsiveness
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        mainWindow.CaptureService_OnKeyPressed();
                                    }), DispatcherPriority.Normal);
                                }
                            }
                        }

                        // Handle tracked key delay
                        if (TrackedKeyCodeDelay != 0 && keyCodeInt == TrackedKeyCodeDelay)
                        {
                            if (PrintKeyDetection)
                            {
                                Debug.WriteLine($"KEY DELAY DETECTED: 0x{keyCodeInt:X}");

                                // Cache the MainWindow reference
                                if (mainWindow == null && Application.Current != null)
                                {
                                    mainWindow = (MainWindow)Application.Current.MainWindow;
                                }

                                if (mainWindow != null)
                                {
                                    // Use priority higher than Background for more responsiveness
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        mainWindow.CaptureService_OnKeyPressedDelay();
                                    }), DispatcherPriority.Normal);
                                }
                            }
                        }

                        // Check for registered toggle keys
                        if (_registeredToggleKeys.Contains(keyCodeInt))
                        {
                            Debug.WriteLine($"DETECTED: Toggle key pressed: 0x{keyCodeInt:X}");
                            OnGlobalKeyPressed?.Invoke(keyCodeInt);
                        }
                    }
                }
            }

            // Call the next hook in the chain
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Cleanup keyboard hook
        /// </summary>
        public void Cleanup()
        {
            // Remove keyboard hook
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Debug.WriteLine("Keyboard hook removed");
            }
        }

        ~KeyboardKey2()
        {
            Cleanup();
        }
    }
}