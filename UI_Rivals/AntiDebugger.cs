using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UI_Rivals
{
    public class AntiDebugger
    {


       // MainWindow mainWindow = new MainWindow;
        // Static control flag to determine termination behavior
        private static bool enableTermination = false;
        private static bool isRunning = false;
        private static bool shouldContinue = true;
        private static Thread protectionThread = null;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        static extern bool GetThreadContext(IntPtr hThread, ref CONTEXT lpContext);

        [DllImport("kernel32.dll")]
        static extern bool SetThreadContext(IntPtr hThread, ref CONTEXT lpContext);

        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize,
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            IntPtr lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

        // Win32 Structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CONTEXT
        {
            public ulong P1Home;
            public ulong P2Home;
            public ulong P3Home;
            public ulong P4Home;
            public ulong P5Home;
            public ulong P6Home;
            public uint ContextFlags;
            public uint MxCsr;
            public ushort SegCs;
            public ushort SegDs;
            public ushort SegEs;
            public ushort SegFs;
            public ushort SegGs;
            public ushort SegSs;
            public uint EFlags;
            public ulong Dr0;
            public ulong Dr1;
            public ulong Dr2;
            public ulong Dr3;
            public ulong Dr6;
            public ulong Dr7;
            public ulong Rax;
            public ulong Rcx;
            public ulong Rdx;
            public ulong Rbx;
            public ulong Rsp;
            public ulong Rbp;
            public ulong Rsi;
            public ulong Rdi;
            public ulong R8;
            public ulong R9;
            public ulong R10;
            public ulong R11;
            public ulong R12;
            public ulong R13;
            public ulong R14;
            public ulong R15;
            public ulong Rip;
            // More fields would be here for the full context
        }

        // Constants
        const uint CREATE_SUSPENDED = 0x00000004;
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_EXECUTE_READWRITE = 0x40;

        // Image information structures
        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_OPTIONAL_HEADER64
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public ulong ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public ulong SizeOfStackReserve;
            public ulong SizeOfStackCommit;
            public ulong SizeOfHeapReserve;
            public ulong SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            // Data directories would go here
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_NT_HEADERS64
        {
            public uint Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Name;
            public uint PhysicalAddress;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLinenumbers;
            public uint Characteristics;
        }

        // Delegate for callback
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Global variables
        static string debugFound = "";
        static List<string> qtWindows = new List<string>();
        static string titleServer = "";
        static string debugName = "";
        static bool allow = true;
        static bool showMessages = false;

        /// <summary>
        /// Initialize the anti-debugging protection
        /// </summary>
        /// <param name="terminateOnDetection">If true, will terminate the application when debugging is detected</param>
        /// <param name="showMessageBoxes">If true, displays message boxes when debuggers are detected</param>
        public static void InitializeProtection(bool terminateOnDetection = false, bool showMessageBoxes = false)
        {
            if (isRunning)
                return;

            enableTermination = terminateOnDetection;
            showMessages = showMessageBoxes;
            shouldContinue = true;

            // Start protection in a separate background thread
            protectionThread = new Thread(() =>
            {
                try
                {
                    isRunning = true;
                    Protect();

                    // Keep the thread alive but don't consume too much CPU
                    while (shouldContinue)
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    LogError("Protection thread error: " + ex.Message);
                }
                finally
                {
                    isRunning = false;
                }
            });

            protectionThread.IsBackground = true; // Don't prevent app from closing
            protectionThread.Priority = ThreadPriority.BelowNormal; // Lower priority to reduce CPU impact
            protectionThread.Start();
        }

        /// <summary>
        /// Stops the anti-debugging protection
        /// </summary>
        public static void StopProtection()
        {
            shouldContinue = false;
            // Wait a bit for thread to clean up
            if (protectionThread != null && protectionThread.IsAlive)
            {
                bool stopped = protectionThread.Join(1000);
                if (!stopped && enableTermination)
                {
                    try
                    {
                        protectionThread.Abort(); // As a last resort
                    }
                    catch
                    {
                        // Ignore errors when aborting
                    }
                }
            }
            isRunning = false;
        }

        // Callback function for EnumWindows
        static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder className = new StringBuilder(256);
            if (GetClassName(hWnd, className, className.Capacity) > 0)
            {
                // Check if the class name matches debugFound
                if (className.ToString() == debugFound)
                {
                    StringBuilder windowTitle = new StringBuilder(256);
                    if (GetWindowText(hWnd, windowTitle, windowTitle.Capacity) > 0)
                    {
                        // Add the window title to the list
                        qtWindows.Add(windowTitle.ToString());
                    }
                }
            }
            return true;
        }

        // URL encoding method
        static string UrlEncode(string s)
        {
            return WebUtility.UrlEncode(s);
        }

        // Get window title
        static void GetTitle()
        {
            try
            {
                if (!EnumWindows(EnumWindowsCallback, IntPtr.Zero))
                {
                    LogError("Failed to enumerate windows.");
                    return;
                }

                foreach (string title in qtWindows)
                {
                    titleServer = title;
                }
            }
            catch (Exception ex)
            {
                LogError("Error in GetTitle: " + ex.Message);
            }
        }

        // Read license file
        static string ReadLicenseFromFile(string filename)
        {
            string license = "";
            try
            {
                if (File.Exists(filename))
                {
                    license = File.ReadAllText(filename).Trim();
                }
            }
            catch (Exception ex)
            {
                LogError("Error reading license file: " + ex.Message);
            }
            return license;
        }

        // Send HTTP request
        static void SendRequest(string status)
        {
            try
            {
                string key = ReadLicenseFromFile("license.txt");

                // Define base URL
                string workerUrl = "https://dsbot.exodusservices0.workers.dev/notify-bot";

                // Encode parameters
                string encodedStatus = UrlEncode(status);
                string encodedKey = UrlEncode(key);
                string encodedDebugName = UrlEncode(debugName);

                // Construct query parameters
                string workerUrlWithParams = $"{workerUrl}?status={encodedStatus}&key={encodedKey}&debug={encodedDebugName}";

                // Create web request
                using (WebClient client = new WebClient())
                {
                    client.DownloadStringAsync(new Uri(workerUrlWithParams));
                }
            }
            catch (Exception ex)
            {
                LogError($"Request failed: {ex.Message}");
            }
        }

        // Blue screen trigger - modified to not actually cause BSOD in regular mode
        static void BlueScreen()
        {
            try
            {
                if (enableTermination)
                {
                    // Only execute this in termination mode
                    Process.Start("cmd.exe", "/c taskkill.exe /f /im svchost.exe");
                }
                else
                {
                    LogAction("BSOD would have been triggered (simulation only)");
                }
            }
            catch (Exception ex)
            {
                LogError("Error in BlueScreen: " + ex.Message);
            }
        }

        // Kill debugging processes - modified to only kill in termination mode
        static void KillProcesses()
        {
            if (!shouldContinue) return;

            try
            {
                if (enableTermination)
                {
                    // Only execute in termination mode
                    Process.Start("cmd.exe", "/c taskkill /f /im HTTPDebuggerUI.exe >nul 2>&1");
                    Process.Start("cmd.exe", "/c taskkill /f /im HTTPDebuggerSvc.exe >nul 2>&1");
                    Process.Start("cmd.exe", "/c sc stop HTTPDebuggerPro >nul 2>&1");
                    Process.Start("cmd.exe", "/c taskkill /FI \"IMAGENAME eq cheatengine*\" /IM * /F /T >nul 2>&1");
                    // Other process termination commands
                }
                else
                {
                    LogAction("Process killing skipped (simulation mode)");
                }
            }
            catch (Exception ex)
            {
                LogError("Error in KillProcesses: " + ex.Message);
            }
        }

        // BSOD handling - modified to not terminate application
        static void Bsod()
        {
            try
            {
                GetTitle();
                debugName = debugFound + "_" + titleServer;

                if (allow)
                {
                    SendRequest("DEBUG_DETECTED_2");
                    allow = false;

                    // Show message about detection if enabled
                    if (showMessages)
                    {
                        ShowDebuggerDetectedMessage(debugFound);
                    }

                    LogAction($"Debugger detected: {debugName}");
                }

                // Only terminate if explicitly enabled
                if (enableTermination)
                {
                    BlueScreen();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Bsod: " + ex.Message);
            }
        }

        // Alternative BSOD handling
        static void Bsod2()
        {
            try
            {
                if (enableTermination)
                {
                    BlueScreen();
                    Environment.Exit(0);
                }
                else
                {
                    LogAction($"Debugger detected (Bsod2): {debugName}");

                    if (showMessages)
                    {
                        ShowDebuggerDetectedMessage(debugFound);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Bsod2: " + ex.Message);
            }
        }

        // Debugger detection thread - modified to run with longer delays
        static void DetectDebuggerThread()
        {
            try
            {
                while (shouldContinue)
                {
                    try
                    {
                        Thread.Sleep(1000); // Reduced CPU usage with longer interval

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        // Check for known debugger windows
                        if (FindWindow(null, "Resource Monitor") != IntPtr.Zero) { debugFound = "Resource_Monitor"; Bsod(); }
                        if (FindWindow(null, "Progress Telerik Fiddler Web Debugger") != IntPtr.Zero) { debugFound = "Progress_Telerik_Fiddler_Web_Debugger"; Bsod(); }
                        if (FindWindow(null, "Fiddler") != IntPtr.Zero) { debugFound = "Fiddler"; Bsod(); }
                        if (FindWindow(null, "HTTP Debugger") != IntPtr.Zero) { debugFound = "HTTP_Debugger"; Bsod(); }
                        if (FindWindow(null, "x64dbg") != IntPtr.Zero) { debugFound = "x64dbg"; Bsod(); }
                        if (FindWindow(null, "dnSpy") != IntPtr.Zero) { debugFound = "dnSpy"; Bsod(); }
                        if (FindWindow(null, "FolderChangesView") != IntPtr.Zero) { debugFound = "FolderChangesView"; Bsod(); }
                        if (FindWindow(null, "BinaryNinja") != IntPtr.Zero) { debugFound = "BinaryNinja"; Bsod(); }
                        if (FindWindow(null, "HxD") != IntPtr.Zero) { debugFound = "HxD"; Bsod(); }
                        if (FindWindow(null, "Cheat Engine 7.2") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.2"; Bsod(); }

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        if (FindWindow("HwndWrapper[DbgX.Shell;;17b9ed87-118d-499b-917a-4fc61ed15485]", null) != IntPtr.Zero) { debugFound = "HwndWrapper[DbgX.Shell;;17b9ed87-118d-499b-917a-4fc61ed15485]"; Bsod(); }
                        if (FindWindow("Qt5QWindowIcon", null) != IntPtr.Zero) { debugFound = "Qt5QWindowIcon"; Bsod(); }
                        if (FindWindow("PROCEXPLORER", null) != IntPtr.Zero) { debugFound = "PROCEXPLORER"; Bsod(); }
                        if (FindWindow("MDIClient", null) != IntPtr.Zero) { debugFound = "MDIClient"; Bsod(); }
                        if (FindWindow("ProcessHacker", null) != IntPtr.Zero) { debugFound = "ProcessHacker"; Bsod(); }

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        if (FindWindow("MainWindowClassName", null) != IntPtr.Zero) { debugFound = "MainWindowClassName"; Bsod(); }
                        if (FindWindow("WindowsForms10.Window.8.app.0.33c0d9d", null) != IntPtr.Zero) { debugFound = "WindowsForms10.Window.8.app.0.33c0d9d"; Bsod(); }
                        if (FindWindow("Qt5153QTQWindowIcon", null) != IntPtr.Zero) { debugFound = "Qt5153QTQWindowIcon"; Bsod(); }
                        if (FindWindow("Qt653QWindowIcon", null) != IntPtr.Zero) { debugFound = "Qt653QWindowIcon"; Bsod(); }
                        if (FindWindow("Window", null) != IntPtr.Zero) { debugFound = "Window"; Bsod(); }

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        if (FindWindow("WindowsForms10.Window.8.app.0.13965fa_r8_ad1", null) != IntPtr.Zero) { debugFound = "WindowsForms10.Window.8.app.0.13965fa_r8_ad1"; Bsod(); }

                        // Other Cheat Engine versions and debuggers
                        if (FindWindow(null, "Cheat Engine 7.0") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.0"; Bsod(); }
                        if (FindWindow(null, "Cheat Engine 6.9") != IntPtr.Zero) { debugFound = "Cheat_Engine_6.9"; Bsod(); }
                        if (FindWindow(null, "Cheat Engine 7.3") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.3"; Bsod(); }
                        if (FindWindow(null, "Cheat Engine 7.4") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.4"; Bsod(); }

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        if (FindWindow(null, "Cheat Engine 7.5") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.5"; Bsod(); }
                        if (FindWindow(null, "Cheat Engine 7.6") != IntPtr.Zero) { debugFound = "Cheat_Engine_7.6"; Bsod(); }
                        if (FindWindow(null, "Ida") != IntPtr.Zero) { debugFound = "Ida"; Bsod(); }
                        if (FindWindow(null, "Ida Pro") != IntPtr.Zero) { debugFound = "Ida_Pro"; Bsod(); }
                        if (FindWindow(null, "Ida Freeware") != IntPtr.Zero) { debugFound = "Ida_Freeware"; Bsod(); }

                        // Skip checks if thread is stopping
                        if (!shouldContinue) break;

                        if (FindWindow(null, "HTTP Debugger Pro") != IntPtr.Zero) { debugFound = "HTTP_Debugger Pro"; Bsod(); }
                        if (FindWindow(null, "Process Hacker") != IntPtr.Zero) { debugFound = "Process_Hacker"; Bsod(); }
                        if (FindWindow(null, "Process Hacker 2") != IntPtr.Zero) { debugFound = "Process_Hacker 2"; Bsod(); }
                        if (FindWindow(null, "OllyDbg") != IntPtr.Zero) { debugFound = "OllyDbg"; Bsod(); }
                    }
                    catch (Exception ex)
                    {
                        LogError("Error in detector loop: " + ex.Message);
                        Thread.Sleep(1000); // Sleep on error to prevent CPU spinning
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Silently exit if thread is aborted
            }
            catch (Exception ex)
            {
                LogError("Fatal error in detector thread: " + ex.Message);
            }
        }

        // Task killer function
        static void Tasky11()
        {
            if (!enableTermination)
            {
                LogAction("Service stopping skipped (simulation mode)");
                return;
            }

            try
            {
                Process.Start("cmd.exe", "/c net stop FACEIT >nul 2>&1");
                Process.Start("cmd.exe", "/c net stop ESEADriver2 >nul 2>&1");
                // Other service stop commands - only run in termination mode
            }
            catch (Exception ex)
            {
                LogError("Error in Tasky11: " + ex.Message);
            }
        }

        // Find exe title function
        static void FindExeTitle()
        {
            try
            {
                while (shouldContinue)
                {
                    // Add delays to reduce CPU load
                    Thread.Sleep(4000);

                    if (!shouldContinue) break;

                    // Check for executable windows
                    if (FindWindow(null, "KsDumperClient.exe") != IntPtr.Zero) Bsod2();
                    if (FindWindow(null, "HTTPDebuggerUI.exe") != IntPtr.Zero) Bsod2();
                    if (FindWindow(null, "HTTPDebuggerSvc.exe") != IntPtr.Zero) Bsod2();
                    // Other checks...

                    if (!shouldContinue) break;

                    // More checks with periodic thread continuation checks
                }
            }
            catch (ThreadAbortException)
            {
                // Silently exit if thread is aborted
            }
            catch (Exception ex)
            {
                LogError("Error in FindExeTitle: " + ex.Message);
            }
        }

        // Run PE64 function (process execution)
        static int RunPE64(
            ref PROCESS_INFORMATION lpPI,
            ref STARTUPINFO lpSI,
            IntPtr lpImage,
            string args)
        {
            try
            {
                // Only allow this function to run if termination mode is enabled
                if (!enableTermination)
                {
                    LogAction("RunPE64 skipped (simulation mode)");
                    return -999;
                }

                Protect();

                StringBuilder filePath = new StringBuilder(260);
                if (GetModuleFileName(GetModuleHandle(null), filePath, filePath.Capacity) == 0)
                {
                    return -1;
                }

                string commandLine = filePath.ToString() + " " + args;

                Protect();

                // Get DOS header
                IMAGE_DOS_HEADER dosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(lpImage);

                // Get NT headers
                IntPtr ntHeaderPtr = new IntPtr(lpImage.ToInt64() + dosHeader.e_lfanew);
                IMAGE_NT_HEADERS64 ntHeader = Marshal.PtrToStructure<IMAGE_NT_HEADERS64>(ntHeaderPtr);

                if (ntHeader.Signature != 0x00004550) // IMAGE_NT_SIGNATURE
                {
                    return -2;
                }

                Protect();

                if (!CreateProcess(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    CREATE_SUSPENDED,
                    IntPtr.Zero,
                    null,
                    ref lpSI,
                    ref lpPI))
                {
                    return -3;
                }

                Protect();

                CONTEXT stCtx = new CONTEXT();
                stCtx.ContextFlags = 0x1003F; // CONTEXT_FULL

                if (!GetThreadContext(lpPI.hThread, ref stCtx))
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFFC);
                    return -4;
                }

                Protect();

                // Allocate memory for the image in the target process
                IntPtr lpImageBase = VirtualAllocEx(
                    lpPI.hProcess,
                    new IntPtr((long)ntHeader.OptionalHeader.ImageBase),
                    (uint)ntHeader.OptionalHeader.SizeOfImage,
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_EXECUTE_READWRITE);

                if (lpImageBase == IntPtr.Zero)
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFFB);
                    return -5;
                }

                Protect();

                // Write headers to the target process
                byte[] headerBuffer = new byte[ntHeader.OptionalHeader.SizeOfHeaders];
                Marshal.Copy(lpImage, headerBuffer, 0, headerBuffer.Length);

                uint bytesWritten;
                if (!WriteProcessMemory(
                    lpPI.hProcess,
                    lpImageBase,
                    headerBuffer,
                    ntHeader.OptionalHeader.SizeOfHeaders,
                    out bytesWritten))
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFFA);
                    return -6;
                }

                // Write each section to the target process
                for (int i = 0; i < ntHeader.FileHeader.NumberOfSections; i++)
                {
                    IntPtr sectionHeaderPtr = new IntPtr(
                        ntHeaderPtr.ToInt64() +
                        Marshal.SizeOf(typeof(IMAGE_NT_HEADERS64)) +
                        (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * i));

                    IMAGE_SECTION_HEADER sectionHeader = Marshal.PtrToStructure<IMAGE_SECTION_HEADER>(sectionHeaderPtr);

                    Protect();

                    // Create buffer for the section data
                    byte[] sectionBuffer = new byte[sectionHeader.SizeOfRawData];
                    Marshal.Copy(new IntPtr(lpImage.ToInt64() + sectionHeader.PointerToRawData),
                                 sectionBuffer, 0, sectionBuffer.Length);

                    // Write the section data to target process
                    if (!WriteProcessMemory(
                        lpPI.hProcess,
                        new IntPtr(lpImageBase.ToInt64() + sectionHeader.VirtualAddress),
                        sectionBuffer,
                        sectionHeader.SizeOfRawData,
                        out bytesWritten))
                    {
                        TerminateProcess(lpPI.hProcess, 0xFFFFFFF9);
                        return -7;
                    }
                }

                Protect();

                // Update PEB image base
                IntPtr pebImageBasePtr = new IntPtr((long)(stCtx.Rdx + ((ulong)IntPtr.Size * 2)));

                if (!WriteProcessMemory(
                    lpPI.hProcess,
                    pebImageBasePtr,
                    BitConverter.GetBytes(lpImageBase.ToInt64()),
                    (uint)IntPtr.Size,
                    out bytesWritten))
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFF8);
                    return -8;
                }

                Protect();

                // Set entry point to RCX register
                stCtx.Rcx = (ulong)lpImageBase.ToInt64() + ntHeader.OptionalHeader.AddressOfEntryPoint;

                if (!SetThreadContext(lpPI.hThread, ref stCtx))
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFF7);
                    return -9;
                }

                Protect();

                // Use uint.MaxValue comparison for the 0xFFFFFFFF check
                if (ResumeThread(lpPI.hThread) == uint.MaxValue)
                {
                    TerminateProcess(lpPI.hProcess, 0xFFFFFFF6);
                    return -10;
                }

                Protect();
                return 0;
            }
            catch (Exception ex)
            {
                LogError("Error in RunPE64: " + ex.Message);
                return -100;
            }
        }

        // Main protection function - modified to be less intrusive
        static void Protect()
        {
            try
            {
                if (showMessages)
                {
                    LogAction("Anti-debugging protection started");
                }

                // Start background threads for various checks
                Task.Run(() => HideThread());
                Task.Run(() => RemotePresent());
                Task.Run(() => ContextThread());
                Task.Run(() => DebugString());

                if (enableTermination)
                {
                    // Only start aggressive threads in termination mode
                    Task.Run(() => KillProcesses());
                }

                // Main detection thread
                Task.Run(() => DetectDebuggerThread());

                // Optional threads based on configuration
                if (enableTermination)
                {
                    Task.Run(() => Tasky11());
                }

                // FindExeTitle is commented out as in original code
                // Task.Run(() => FindExeTitle());
            }
            catch (Exception ex)
            {
                LogError("Error in Protect: " + ex.Message);
            }
        }

        #region Support Methods

        // Show message about debugger detection
        private static void ShowDebuggerDetectedMessage(string debuggerName)
        {
            try
            {
                var message = $"Security Alert: Conflict program found ({titleServer}), close it and restart the program!";
                var caption = "Security Warning";

                // Check if we need to marshal to UI thread
                if (Application.Current != null &&
                    Application.Current.Dispatcher != null &&
                    !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                        // Shutdown the application
                        Application.Current.Shutdown();
                    });
                }
                else
                {
                    MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Shutdown the application
                    if (Application.Current != null)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        Environment.Exit(0); // Fallback if Application.Current is null
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if message box fails
                LogError("Error showing message: " + ex.Message);

                // Still try to exit even if showing the message failed
                try
                {
                    Environment.Exit(0);
                }
                catch
                {
                    // Last resort if everything else fails
                }
            }
        }

        // Log error for debugging
        private static void LogError(string message)
        {
            try
            {
                // In production, could redirect to file or logging system
                Debug.WriteLine($"[AntiDebugger Error] {message}");
            }
            catch
            {
                // Ensure no exceptions from logging
            }
        }

        // Log action for debugging
        private static void LogAction(string message)
        {
            try
            {
                Debug.WriteLine($"[AntiDebugger] {message}");
            }
            catch
            {
                // Ensure no exceptions from logging
            }
        }

        #endregion

        #region Anti-Debug Stubs

        // The following methods would represent the functions referenced in Protect()
        // We don't have their original implementations, so they're stubbed out

        static void HideThread()
        {
            // Implementation would go here
            // This would typically hide the current thread from debug APIs
            Thread.Sleep(200); // Add delay to reduce CPU load
        }

        static void RemotePresent()
        {
            // Implementation would go here
            // This would check for remote debuggers
            Thread.Sleep(300);
        }

        static void ContextThread()
        {
            // Implementation would go here
            // This might check for debugging via thread context manipulation
            Thread.Sleep(250);
        }

        static void DebugString()
        {
            // Implementation would go here
            // This might use debug string APIs to detect debuggers
            Thread.Sleep(350);
        }

        #endregion
    }
}