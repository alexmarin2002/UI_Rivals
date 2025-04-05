using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Threading;

namespace UI_Rivals
{
    public partial class MainWindow : Window
    {
        private bool isListeningForKey = false;
        private Key currentBindKey = Key.F;
        private ScreenCaptureService captureService;
        private SolidColorBrush activeColorBrush = new SolidColorBrush(Color.FromRgb(74, 110, 245)); // Default blue

        // For simulating mouse click
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        // Cooldown for trigger
        private DateTime lastTriggerTime = DateTime.MinValue;
        private int triggerDelayMs = 150; // Default delay

        public MainWindow()
        {
            InitializeComponent();

            // Initialize screen capture service
            try
            {
                captureService = new ScreenCaptureService();
                captureService.OnColorDetected += TriggerDetected;
                captureService.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing screen capture: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Set initial tag values for toggles
            InitializeUIComponents();

            // Set up key listener
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Show the current keybind
            UpdateKeybindButtonText();
            LoadSettings();

            // Set the active state
            UpdateCaptureServiceSettings();

            // Make sure to clean up on closing
            Closing += MainWindow_Closing;
        }

        private void InitializeUIComponents()
        {
            // Set tag values for Toggle controls to work correctly with color changes
            var grids = FindVisualChildren<Grid>(this);
            foreach (var grid in grids)
            {
                if (grid.Tag == null)
                {
                    grid.Tag = activeColorBrush;
                }
            }
        }

        // Helper method to find all children of a specific type
        private System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void UpdateCaptureServiceSettings()
        {
            if (captureService == null) return;

            // Set pixel range
            captureService.SetPixelRange((int)PixelRangeSlider.Value);

            // Set color mode based on selection
            int colorMode = 0; // Default blue
            if (RedColor.IsChecked == true) colorMode = 1;
            if (YellowColor.IsChecked == true) colorMode = 2;
            captureService.SetColorMode(colorMode);

            // Set active state
            captureService.SetActive(StatusToggle.IsChecked == true);

            // Set trigger delay
            if (!string.IsNullOrEmpty(DelayTextBox.Text) && int.TryParse(DelayTextBox.Text, out int delay))
            {
                triggerDelayMs = delay;
            }
        }

        private void TriggerDetected()
        {
            // Check if we're within the cooldown period
            if ((DateTime.Now - lastTriggerTime).TotalMilliseconds < triggerDelayMs)
            {
                return;
            }

            // Simulate mouse click
            // mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            // mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            MessageBox.Show($"TRIGGERBOT DETECTED", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            // Update last trigger time
            lastTriggerTime = DateTime.Now;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            captureService?.Dispose();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StatusToggle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCaptureServiceSettings();
            SaveSettings();
        }

        private void StatusToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateCaptureServiceSettings();
            SaveSettings();
        }

        private void ColorOption_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                // Update color based on which radio button was checked
                if (radioButton == BlueColor)
                    activeColorBrush = new SolidColorBrush(Color.FromRgb(74, 110, 245));
                else if (radioButton == RedColor)
                    activeColorBrush = new SolidColorBrush(Color.FromRgb(245, 74, 110));
                else if (radioButton == YellowColor)
                    activeColorBrush = new SolidColorBrush(Color.FromRgb(245, 222, 74));

                UpdateToggleTags();
                UpdateCaptureServiceSettings();
                SaveSettings();
            }
        }

        private void UpdateToggleTags()
        {
            // Update Tag properties for all relevant Grid containers
            var grids = FindVisualChildren<Grid>(this);
            foreach (var grid in grids)
            {
                if (grid.Tag != null && grid.Tag is SolidColorBrush)
                {
                    grid.Tag = activeColorBrush;
                }
            }
        }

        private void KeybindButton_Click(object sender, RoutedEventArgs e)
        {
            // Start listening for keybind
            isListeningForKey = true;
            KeybindButton.Content = "Press any key...";
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (isListeningForKey)
            {
                // Ignore certain system keys
                if (e.Key != Key.System && e.Key != Key.Tab &&
                    e.Key != Key.LeftAlt && e.Key != Key.RightAlt)
                {
                    currentBindKey = e.Key;
                    isListeningForKey = false;
                    UpdateKeybindButtonText();
                    SaveSettings();
                    e.Handled = true;
                }
            }
        }

        private void UpdateKeybindButtonText()
        {
            KeybindButton.Content = $"Press to rebind [{currentBindKey}]";
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCaptureServiceSettings();
            SaveSettings();
        }

        private void PixelRangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PixelRangeText != null)
            {
                PixelRangeText.Text = $"{(int)PixelRangeSlider.Value}";
                UpdateCaptureServiceSettings();
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            // Save all settings to a file
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RivalsTriggerbot",
                    "settings.txt");

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                using (StreamWriter writer = new StreamWriter(settingsPath))
                {
                    writer.WriteLine($"Status={Convert.ToInt32(StatusToggle.IsChecked == true)}");

                    string colorValue = "0"; // Default (Blue)
                    if (RedColor.IsChecked == true) colorValue = "1";
                    if (YellowColor.IsChecked == true) colorValue = "2";
                    writer.WriteLine($"ColorDetection={colorValue}");

                    writer.WriteLine($"Key={(int)currentBindKey}");

                    int delay = 150;
                    if (!string.IsNullOrEmpty(DelayTextBox.Text) && int.TryParse(DelayTextBox.Text, out int parsedDelay))
                    {
                        delay = parsedDelay;
                    }
                    writer.WriteLine($"ShootingDelay={delay}");

                    writer.WriteLine($"PixelRange={(int)PixelRangeSlider.Value}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Settings Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RivalsTriggerbot",
                    "settings.txt");

                if (File.Exists(settingsPath))
                {
                    string[] settings = File.ReadAllLines(settingsPath);

                    foreach (string line in settings)
                    {
                        if (line.StartsWith("Status="))
                        {
                            StatusToggle.IsChecked = line.Substring(7) == "1";
                        }
                        else if (line.StartsWith("ColorDetection="))
                        {
                            string colorValue = line.Substring(15);
                            BlueColor.IsChecked = colorValue == "0";
                            RedColor.IsChecked = colorValue == "1";
                            YellowColor.IsChecked = colorValue == "2";

                            // Update active color
                            if (colorValue == "0")
                                activeColorBrush = new SolidColorBrush(Color.FromRgb(74, 110, 245));
                            else if (colorValue == "1")
                                activeColorBrush = new SolidColorBrush(Color.FromRgb(245, 74, 110));
                            else if (colorValue == "2")
                                activeColorBrush = new SolidColorBrush(Color.FromRgb(245, 222, 74));

                            UpdateToggleTags();
                        }
                        else if (line.StartsWith("Key="))
                        {
                            if (int.TryParse(line.Substring(4), out int keyValue))
                            {
                                currentBindKey = (Key)keyValue;
                                UpdateKeybindButtonText();
                            }
                        }
                        else if (line.StartsWith("ShootingDelay="))
                        {
                            if (int.TryParse(line.Substring(14), out int delay))
                            {
                                DelayTextBox.Text = delay.ToString();
                                triggerDelayMs = delay;
                            }
                        }
                        else if (line.StartsWith("PixelRange="))
                        {
                            if (int.TryParse(line.Substring(11), out int pixelRange))
                            {
                                PixelRangeSlider.Value = pixelRange;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}