using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace UI_Rivals
{
    /// <summary>
    /// Manages shared memory that can be accessed across different files and classes
    /// </summary>
    public unsafe class SharedMemoryManager
    {

        // Windows API imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        // Constants
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const uint PAGE_READWRITE = 0x04;
        private const uint FILE_MAP_ALL_ACCESS = 0xF001F;

        // Static instance for singleton pattern
        private static SharedMemoryManager _instance;

        // Shared memory handles and pointers
        private IntPtr _hMapFile;
        private IntPtr _pSharedValue;
        private int* _pSharedValueInt;
        private bool _isInitialized = false;

        // Public singleton access
        public static SharedMemoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SharedMemoryManager();
                }
                return _instance;
            }
        }

        private SharedMemoryManager()
        {
            // Private constructor for singleton pattern
        }

        /// <summary>
        /// Initializes the shared memory
        /// </summary>
        /// <returns>True if initialization was successful</returns>
        public bool Initialize()
        {
            if (_isInitialized) return true;

            try
            {
                // Create file mapping
                _hMapFile = CreateFileMapping(
                    INVALID_HANDLE_VALUE,    // Use paging file
                    IntPtr.Zero,             // Default security
                    PAGE_READWRITE,          // Read/Write access
                    0,                       // Maximum object size (high-order DWORD)
                    sizeof(int),             // Maximum object size (low-order DWORD)
                    "SharedMemory");         // Name of mapping object

                if (_hMapFile == IntPtr.Zero)
                {
                    Console.WriteLine($"Could not create file mapping object ({Marshal.GetLastWin32Error()}).");
                    return false;
                }

                // Map the memory into the address space of the current process
                _pSharedValue = MapViewOfFile(
                    _hMapFile,
                    FILE_MAP_ALL_ACCESS,
                    0,
                    0,
                    sizeof(int));

                if (_pSharedValue == IntPtr.Zero)
                {
                    Console.WriteLine($"Could not map view of file ({Marshal.GetLastWin32Error()}).");
                    CloseHandle(_hMapFile);
                    return false;
                }

                // Store it as a pointer
                _pSharedValueInt = (int*)_pSharedValue.ToPointer();

                // Initialize the value to 0
                *_pSharedValueInt = 0;

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing shared memory: {ex.Message}");
                Cleanup();
                return false;
            }
        }

        /// <summary>
        /// Sets the value in shared memory
        /// </summary>
        public void SetValue(int value)
        {
            if (!_isInitialized)
            {
                if (!Initialize())
                {
                    throw new InvalidOperationException("SharedMemoryManager not initialized");
                }
            }

            *_pSharedValueInt = value;
        }

        /// <summary>
        /// Gets the value from shared memory
        /// </summary>
        
        public int GetValue()
        {
            if (!_isInitialized)
            {
                if (!Initialize())
                {
                    throw new InvalidOperationException("SharedMemoryManager not initialized");
                }
            }

            return *_pSharedValueInt;
        }

        /// <summary>
        /// Cleanup shared memory resources
        /// </summary>
        public void Cleanup()
        {
            if (_pSharedValue != IntPtr.Zero)
            {
                UnmapViewOfFile(_pSharedValue);
                _pSharedValue = IntPtr.Zero;
            }

            if (_hMapFile != IntPtr.Zero)
            {
                CloseHandle(_hMapFile);
                _hMapFile = IntPtr.Zero;
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Clean up resources when the application exits
        /// </summary>
        ~SharedMemoryManager()
        {
            Cleanup();
        }
    }
}