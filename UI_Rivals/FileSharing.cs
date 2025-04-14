using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;


namespace UI_Rivals
{

    public class FileSharing : IDisposable
    {
        public bool programming = true;

        // Enum to define the indices for different settings
        public enum SettingIndex
        {
            Status = 0,
            ShotWhileMoving = 1,
            PixelRange = 2,
            ShootingMethod = 3,
            ColorDetection = 4,
            ShootingDelay = 5,
            SelectedKey = 6,
            WeaponDelayKey = 7,
            WeaponDelayVandal = 8,
            WeaponDelayGuardian = 9,
            WeaponDelaySheriff = 10,
            WeaponDelayPhantom = 11,
            WeaponDelayGhost = 12,
            WeaponDelayNone = 13,
            AudioRange = 14

        }

        // File path for settings storage
        private readonly string settingsFilePath;
        private readonly Dictionary<SettingIndex, int> settingValues;
        private readonly object fileLock = new object();
        private bool isDisposed = false;
        private FileSystemWatcher watcher;
        private DateTime lastWriteTime = DateTime.MinValue;

        // Line 20 monitoring
        private string lastLine20Value = "";
        private Timer monitoringTimer;
        private bool isMonitoringActive = false;
        public event EventHandler<string> Line20Changed;


        public FileSharing(string filePath = null)
        {
            if (programming)
            {
                // Use default file path if none provided
                settingsFilePath = filePath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TestingUI3",
                    "settings.txt");
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));
            }
            else
            {
                settingsFilePath = filePath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "files",
                "settings.txt");
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));

                // Initializ

            }
            settingValues = new Dictionary<SettingIndex, int>();

            // Load existing settings or create default
            if (File.Exists(settingsFilePath))
            {
                LoadSettingsFromFile();
            }
            else
            {
                InitializeDefaultValues();
                SaveSettingsToFile();
            }

            // Set up file watcher to detect changes from C++ application
            SetupFileWatcher();

            // Start monitoring line 20 for changes
            SetupLine20Monitoring();
        }

        // Initialize default settings values
        private void InitializeDefaultValues()
        {
            // Status off by default
            settingValues[SettingIndex.Status] = 0;

            // ShotWhileMoving off by default
            settingValues[SettingIndex.ShotWhileMoving] = 5;

            // Default pixel range 5 (mapped to 15)
            settingValues[SettingIndex.PixelRange] = 15;

            // Default to Press Button
            settingValues[SettingIndex.ShootingMethod] = 12;

            // Default to Red
            settingValues[SettingIndex.ColorDetection] = 14;

            // Default to Vandal
            settingValues[SettingIndex.ShootingDelay] = 5;

            // Default weapon delays
            settingValues[SettingIndex.WeaponDelayVandal] = 6;
            settingValues[SettingIndex.WeaponDelayGuardian] = 11;
            settingValues[SettingIndex.WeaponDelaySheriff] = 16;
            settingValues[SettingIndex.WeaponDelayPhantom] = 20;
            settingValues[SettingIndex.WeaponDelayGhost] = 25;
            settingValues[SettingIndex.WeaponDelayNone] = 30;

            // Default to no keys assigned
            settingValues[SettingIndex.SelectedKey] = 0;
            settingValues[SettingIndex.WeaponDelayKey] = 0;

            //Default Audio Range
            settingValues[SettingIndex.AudioRange] = 5;

        }

        // Load settings from the file
        private void LoadSettingsFromFile()
        {
            lock (fileLock)
            {
                try
                {
                    string[] lines = File.ReadAllLines(settingsFilePath);

                    foreach (string line in lines)
                    {
                        // Skip empty lines and comments
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;

                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            if (Enum.TryParse<SettingIndex>(key, out SettingIndex index) &&
                                int.TryParse(value, out int intValue))
                            {
                                settingValues[index] = intValue;
                            }
                        }
                    }

                    // Record when we last read the file
                    lastWriteTime = File.GetLastWriteTime(settingsFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings from file: {ex.Message}");

                    // If loading fails, initialize with defaults
                    InitializeDefaultValues();
                }
            }
        }

        // Save settings to the file
        private void SaveSettingsToFile()
        {
            lock (fileLock)
            {
                try
                {
                    // Temporarily disable file watcher to prevent triggering our own events
                    if (watcher != null)
                        watcher.EnableRaisingEvents = false;

                    using (StreamWriter writer = new StreamWriter(settingsFilePath, false, Encoding.UTF8))
                    {
                        writer.WriteLine("# TestingUI3 Settings File");
                        writer.WriteLine("# Format: SettingName=Value");
                        writer.WriteLine("# Last Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        writer.WriteLine();

                        foreach (var kvp in settingValues)
                        {
                            writer.WriteLine($"{kvp.Key}={kvp.Value}");
                        }
                    }

                    // Record when we last wrote to the file
                    lastWriteTime = File.GetLastWriteTime(settingsFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving settings to file: {ex.Message}");
                }
                finally
                {
                    // Re-enable file watcher
                    if (watcher != null)
                        watcher.EnableRaisingEvents = true;
                }
            }
        }

        // Set up file watcher to detect external changes
        private void SetupFileWatcher()
        {
            try
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(settingsFilePath))
                {
                    Filter = Path.GetFileName(settingsFilePath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnFileChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up file watcher: {ex.Message}");
            }
        }

        // Handle file change event
        public void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Avoid processing the same change multiple times
                DateTime currentWriteTime = File.GetLastWriteTime(settingsFilePath);

                if (currentWriteTime == lastWriteTime)
                    return;

                // Wait a moment to ensure the file is not locked
                Thread.Sleep(100);
                // Read specific line (Example: line 20)
                LoadSettingsFromFile();

                Console.WriteLine("Settings file was modified externally. Reloaded settings.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling file change: {ex.Message}");
            }
        }

        private string ReadSpecificLine(int lineNumber)
        {
            lock (fileLock)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(settingsFilePath))
                    {
                        for (int i = 1; i < lineNumber; i++)
                        {
                            if (reader.ReadLine() == null)
                                return ""; // Return empty if line doesn't exist
                        }
                        return reader.ReadLine() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading specific line: {ex.Message}");
                    return "";
                }
            }
        }

        // Setup monitoring for line 20
        private void SetupLine20Monitoring()
        {
            try
            {
                // Initialize the last known value
                lastLine20Value = ReadSpecificLine(20);

                // Setup a timer to periodically check for changes
                monitoringTimer = new Timer(350); // Check every second
                monitoringTimer.Elapsed += (s, e) => CheckLine20Changes();
                monitoringTimer.AutoReset = true;
                monitoringTimer.Enabled = true;
                isMonitoringActive = true;

                Console.WriteLine($"Line 20 monitoring started. Initial value: {lastLine20Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up line 20 monitoring: {ex.Message}");
            }
        }
        public string printingValueExe()
        {

            string randomValue = ReadSpecificLine(21);
            return randomValue;

        }
        // Check for changes in line 20
        private void CheckLine20Changes()
        {
            if (!isMonitoringActive) return;

            try
            {
                string currentValue = ReadSpecificLine(20);

                // Check if the value has changed
                if (currentValue != lastLine20Value)
                {
                    Console.WriteLine($"Line 20 changed from '{lastLine20Value}' to '{currentValue}'");

                    // Save the new value
                    lastLine20Value = currentValue;

                    // Trigger the event
                    Line20Changed?.Invoke(this, currentValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking line 20 changes: {ex.Message}");
            }
        }
        // Write to a specific line in the settings file
        public void WriteToSpecificLine(int lineNumber, string content)
        {
            lock (fileLock)
            {
                try
                {
                    // Temporarily disable file watcher to prevent triggering our own events
                    if (watcher != null)
                        watcher.EnableRaisingEvents = false;

                    // Read all lines from the file
                    string[] lines = File.ReadAllLines(settingsFilePath);

                    // Make sure we have enough lines
                    while (lines.Length < lineNumber)
                    {
                        Array.Resize(ref lines, lines.Length + 1);
                        lines[lines.Length - 1] = "";
                    }

                    // Update the specific line
                    lines[lineNumber - 1] = content;

                    // Write all lines back to the file
                    File.WriteAllLines(settingsFilePath, lines);

                    // Record when we last wrote to the file
                    lastWriteTime = File.GetLastWriteTime(settingsFilePath);

                    Console.WriteLine($"Updated line {lineNumber} with content: {content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to specific line: {ex.Message}");
                }
                finally
                {
                    // Re-enable file watcher
                    if (watcher != null)
                        watcher.EnableRaisingEvents = true;
                }
            }
        }



        // Read content from a specific line in the settings file
        public string ReadFromSpecificLine(int lineNumber)
        {
            lock (fileLock)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(settingsFilePath))
                    {
                        for (int i = 1; i < lineNumber; i++)
                        {
                            if (reader.ReadLine() == null)
                                return ""; // Return empty if line doesn't exist
                        }
                        return reader.ReadLine() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading from specific line: {ex.Message}");
                    return "";
                }
            }
        }

        // Update a setting value
        public void UpdateSetting(SettingIndex index, int value)
        {
            lock (fileLock)
            {
                if (settingValues.ContainsKey(index) && settingValues[index] == value)
                    return; // No change needed

                // Update in memory
                settingValues[index] = value;

                // Save to file
                SaveSettingsToFile();

                Console.WriteLine($"Updated setting {index} to {value}");
            }
        }

        // Read a setting value
        public int ReadSetting(SettingIndex index)
        {
            lock (fileLock)
            {
                if (settingValues.TryGetValue(index, out int value))
                    return value;

                return 0; // Default if not found
            }
        }

        // Get the file path (for C++ program to know where to look)
        public string GetSettingsFilePath()
        {
            return settingsFilePath;
        }

        // Implement IDisposable to clean up resources
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (watcher != null)
                    {
                        watcher.Changed -= OnFileChanged;
                        watcher.Dispose();
                        watcher = null;
                    }

                    // Stop and dispose the timer
                    if (monitoringTimer != null)
                    {
                        isMonitoringActive = false;
                        monitoringTimer.Stop();
                        monitoringTimer.Dispose();
                        monitoringTimer = null;
                    }
                }

                isDisposed = true;
            }
        }

        ~FileSharing()
        {
            Dispose(false);
        }
    }
}