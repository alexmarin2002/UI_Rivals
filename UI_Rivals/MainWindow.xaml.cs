using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using UI_Rivals;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Media;
using NAudio.Wave;
using System.Threading.Tasks;



namespace UI_Rivals
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        // Add this at the class level
        // Add this at the class level
        private ScreenCaptureService captureService;
        public KeyboardKey2 keybaord;
        private readonly string settingsFilePath;
        private int number = 0;
        public FileSharing fileSharing;
        // Add these properties for login functionality
        private bool _isLoggedIn = false;
        private string _loginUsername = "";
        string filePath = null;
        AntiDebugger antiDebuggerLocal = new AntiDebugger();

        LoginAuth loginAuthLocal = new LoginAuth();


        // Dictionary to map settings to their numerical values (0-30)
        private Dictionary<string, int> settingValues = new Dictionary<string, int>();
        public MainWindow()
        {


            try
            {

                InitializeComponent();

                // In MainWindow constructor
                captureService = new ScreenCaptureService(this);
                keybaord = new KeyboardKey2();


                // Start the capture service when appropriate
                captureService.Start();

                TextBlock resBlock = this.FindName("ResolutionTextBlock") as TextBlock;
                if (resBlock != null)
                {
                    resBlock.Text = $"{SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}";
                }
                // Initialize settings dictionary
                InitializeSettingValues();
                //this.Title = "Exodus Services - Valorant";


                // Initialize file sharing - this creates/reads the settings file
                fileSharing = new FileSharing();
                KeyboardKey2.Instance.SetTrackedKey(fileSharing.ReadSetting(FileSharing.SettingIndex.SelectedKey));
                KeyboardKey2.Instance.SetTrackedKeyDelay(fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayKey));
                captureService.pixelRange = fileSharing.ReadSetting(FileSharing.SettingIndex.PixelRange);


                captureService.shotingMethod = fileSharing.ReadSetting(FileSharing.SettingIndex.ShootingMethod);

                AntiDebugger.InitializeProtection(
                    terminateOnDetection: false,  // Set to true for aggressive mode
                    showMessageBoxes: true        // Set to true to show popups on detection
                );
                UsernameTextBox.Text = fileSharing.ReadFromSpecificLine(21);
                fileSharing.WriteToSpecificLine(26, "1");
                fileSharing.WriteToSpecificLine(25, "0"); //starting
                fileSharing.WriteToSpecificLine(24, "0"); //starting

                //-------------------------------------------------------------


                //antiDebugger.Main2(null);

                //---------------------------------------------------------------
                // Path for settings file
                settingsFilePath = filePath ?? Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "files",
                        "settings.txt");

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));

                // Separate path for launching jesus.exe
                string exePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "files",
                    "jesus.exe");

                // Check if file exists before trying to launch it
                if (File.Exists(exePath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = exePath;
                    startInfo.UseShellExecute = true;

                    Process.Start(startInfo);

                }
                else
                {
                    MessageBox.Show(
$"Contact the Developer: {exePath}",
"EXODUS message",
MessageBoxButton.OK,
MessageBoxImage.Information
);
                }
                // Subscribe to the Line20Changed event
                fileSharing.Line20Changed += OnLine20Changed;

                Console.WriteLine($"Settings file located at: {fileSharing.GetSettingsFilePath()}");

                fileSharing.printingValueExe();

                UserIDText.Text = fileSharing.printingValueExe();
                ;
                //LAUNCH SETTINGS-------------------------------------------------------------------------------------------------
                if (StatusText != null)
                    StatusText.Text = "OFF";

                if (fileSharing != null)
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.Status, settingValues["StatusFalse"]);

                //LAUNCH SETTINGS-------------------------------------------------------------------------------------------------
                RememberMeCheckbox.IsChecked = true;


                // Set initial values for controls based on file settings
                SetInitialValues();

                // Set visibility of GlobalSettingsView as the default view
                HideAllViews();
                GlobalSettingsView.Visibility = Visibility.Visible;



            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error during initialization: {ex.Message}");
            }

        }
        static async Task testAudio(string name, float volumeLevel)
        {
            // Path to your .wav file
            string wavFilePath = @"C:\Users\aleks\source\repos\UI_Rivals\UI_Rivals\" + name + ".wav";

            try
            {
                // Convert scale 1-10 to 0.0-1.0
                float normalizedVolume = Math.Max(0f, Math.Min(volumeLevel / 10f, 1f));

                using (var audioFile = new AudioFileReader(wavFilePath))
                using (var outputDevice = new WaveOutEvent())
                {
                    // Set volume (0.0 to 1.0)
                    audioFile.Volume = normalizedVolume;

                    Console.WriteLine($"Playing WAV file: {wavFilePath} at volume: {normalizedVolume * 100}%");

                    // Use TaskCompletionSource to convert event-based model to task-based
                    var tcs = new TaskCompletionSource<bool>();

                    outputDevice.PlaybackStopped += (s, e) => {
                        tcs.TrySetResult(true);
                    };

                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    // This will await without blocking the UI thread
                    await tcs.Task;

                    Console.WriteLine("Playback complete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error playing WAV file: " + ex.Message);
            }

        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();


        }
        private void NeedHelp_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the Discord link in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://discord.gg/exodusaim",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Discord link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowErrorMessage(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessagePanel.Visibility = Visibility.Visible;
        }
        private void CloseErrorMessage_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessagePanel.Visibility = Visibility.Collapsed;
        }
        string textBoxAfterKey = "";
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            loginAuthLocal.key = UsernameTextBox.Text;

            loginAuthLocal.CloudFireAsync();

            UsernameTextBox.Text = loginAuthLocal.textBoxAfterKey;
            captureService.isActive = true;
            LoginView.Visibility = Visibility.Collapsed;
            MainAppUI.Visibility = Visibility.Visible;

        }
        private void BuyOne_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the Discord link in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.exodusprivate.com/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open WebSite link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handle line 20 change event
        private void OnLine20Changed(object sender, string newValue)
        {
            // We need to use Dispatcher.Invoke because this will be called from a non-UI thread
            this.Dispatcher.Invoke(() =>
            {
                if (newValue == "1") //ON 
                {
                    StatusToggle.IsChecked = true;

                }
                if (newValue == "2") //OFF
                {
                    StatusToggle.IsChecked = false;
                }
                if (newValue == "3" && VandalOption != null)
                {
                    VandalOption.IsChecked = true;

                    if (fileSharing != null && settingValues.ContainsKey("VANDAL"))
                    {
                        int value = settingValues["VANDAL"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to VANDAL ({value}) via Line20 change");
                    }
                }
                if (newValue == "4" && GuardianOption != null)
                {
                    GuardianOption.IsChecked = true;
                    if (fileSharing != null && settingValues.ContainsKey("GUARDIAN"))
                    {
                        int value = settingValues["GUARDIAN"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to GUARDIAN ({value}) via Line20 change");
                    }
                }
                if (newValue == "5" && SherifOption != null)
                {
                    SherifOption.IsChecked = true;
                    if (fileSharing != null && settingValues.ContainsKey("SHERIF"))
                    {
                        int value = settingValues["SHERIF"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to SHERIF ({value}) via Line20 change");
                    }
                }
                if (newValue == "6" && PhantomOption != null)
                {
                    PhantomOption.IsChecked = true;
                    if (fileSharing != null && settingValues.ContainsKey("PHANTOM"))
                    {
                        int value = settingValues["PHANTOM"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to PHANTOM ({value}) via Line20 change");
                    }
                }
                if (newValue == "7" && GhostOption != null)
                {
                    GhostOption.IsChecked = true;
                    if (fileSharing != null && settingValues.ContainsKey("GHOST"))
                    {
                        int value = settingValues["GHOST"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to GHOST ({value}) via Line20 change");
                    }
                }
                if (newValue == "8" && NoDelayOption != null)
                {
                    NoDelayOption.IsChecked = true;
                    if (fileSharing != null && settingValues.ContainsKey("NO DELAY"))
                    {
                        int value = settingValues["NO DELAY"];
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                        Console.WriteLine($"Shooting Delay changed to NO DFELAY ({value}) via Line20 change");
                    }
                }
            });
        }

        private void InitializeSettingValues()
        {
            try
            {
                // Map toggle settings with distinct values
                settingValues["StatusTrue"] = 10;
                settingValues["StatusFalse"] = 0;
                settingValues["ShotWhileMovingTrue"] = 15;
                settingValues["ShotWhileMovingFalse"] = 5;

                // Map shooting method with distinct values
                settingValues["PressButton"] = 12;
                settingValues["HoldButton"] = 24;

                // Map color detection with distinct values
                settingValues["Red"] = 7;
                settingValues["Purple"] = 14;
                settingValues["Yellow"] = 21;
                settingValues["Purple(ReynaUlt)"] = 28;

                // Map pixel range values (1-10) to distinct values between 0-30
                settingValues["PixelRange1"] = 1;
                settingValues["PixelRange2"] = 2;
                settingValues["PixelRange3"] = 3;
                settingValues["PixelRange4"] = 4;
                settingValues["PixelRange5"] = 5;
                settingValues["PixelRange6"] = 6;
                settingValues["PixelRange7"] = 7;
                settingValues["PixelRange8"] = 8;
                settingValues["PixelRange9"] = 9;
                settingValues["PixelRange10"] = 10;

                //Map Audio Range Values
                settingValues["AudioRange0"] = 0;
                settingValues["AudioRange1"] = 1;
                settingValues["AudioRange2"] = 2;
                settingValues["AudioRange3"] = 3;
                settingValues["AudioRange4"] = 4;
                settingValues["AudioRange5"] = 5;
                settingValues["AudioRange6"] = 6;
                settingValues["AudioRange7"] = 7;
                settingValues["AudioRange8"] = 8;
                settingValues["AudioRange9"] = 9;
                settingValues["AudioRange10"] = 10;

                // Map shooting delay with distinct values
                settingValues["VANDAL"] = 5;
                settingValues["GUARDIAN"] = 9;
                settingValues["SHERIF"] = 13;
                settingValues["PHANTOM"] = 17;
                settingValues["GHOST"] = 22;
                settingValues["NO DELAY"] = 28;

                // Default weapon delays
                settingValues["DefaultVandal"] = 6;
                settingValues["DefaultGuardian"] = 11;
                settingValues["DefaultSheriff"] = 16;
                settingValues["DefaultPhantom"] = 20;
                settingValues["DefaultGhost"] = 25;
                settingValues["DefaultNoDelay"] = 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InitializeSettingValues: {ex.Message}");
            }
        }

        private void SetInitialValues()
        {
            try
            {
                if (fileSharing == null)
                {
                    Console.WriteLine("FileSharing not initialized, using default values");
                    return;
                }

                // Load settings from file for UI initialization

                // Status toggle
                bool status = fileSharing.ReadSetting(FileSharing.SettingIndex.Status) == 10;
                if (StatusToggle != null)
                {
                    StatusToggle.IsChecked = status;
                    if (StatusText != null)
                        StatusText.Text = status ? "ON" : "OFF";
                }

                // ShotWhileMoving toggle
                bool shotWhileMoving = fileSharing.ReadSetting(FileSharing.SettingIndex.ShotWhileMoving) == 15;
                if (ShotWhileMovingToggle != null)
                {
                    ShotWhileMovingToggle.IsChecked = shotWhileMoving;
                    if (ShotWhileMovingText != null)
                        ShotWhileMovingText.Text = shotWhileMoving ? "ON" : "OFF";
                }

                // Pixel Range slider
                int pixelRangeValue = fileSharing.ReadSetting(FileSharing.SettingIndex.PixelRange);

                PixelRangeSlider.Value = pixelRangeValue;

                if (PixelRangeSlider != null)
                {
                    if (PixelRangeValue != null)
                        PixelRangeValue.Text = pixelRangeValue.ToString();
                }

                // Audio Range slider
                int audioRangeValue = fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange);

                AudioRangeSlider.Value = audioRangeValue;

                if (AudioRangeSlider != null)
                {
                    if (AudioRangeValue != null)
                        AudioRangeValue.Text = audioRangeValue.ToString() + "0%";
                }

                // Shooting Method radio buttons
                int shootingMethod = fileSharing.ReadSetting(FileSharing.SettingIndex.ShootingMethod);
                if (PressButtonOption != null)
                    PressButtonOption.IsChecked = (shootingMethod == settingValues["PressButton"]);
                if (HoldButtonOption != null)
                    HoldButtonOption.IsChecked = (shootingMethod == settingValues["HoldButton"]);

                // If neither matches, default to Press Button
                if (shootingMethod != settingValues["PressButton"] &&
                    shootingMethod != settingValues["HoldButton"] &&
                    PressButtonOption != null)
                {
                    PressButtonOption.IsChecked = true;
                }

                // Color Detection radio buttons
                int colorDetection = fileSharing.ReadSetting(FileSharing.SettingIndex.ColorDetection);
                if (RedOption != null)
                    RedOption.IsChecked = (colorDetection == settingValues["Red"]);
                if (PurpleOption != null)
                    PurpleOption.IsChecked = (colorDetection == settingValues["Purple"]);
                if (YellowOption != null)
                    YellowOption.IsChecked = (colorDetection == settingValues["Yellow"]);
                if (PurpleReynaUltOption != null)
                    YellowOption.IsChecked = (colorDetection == settingValues["Purple(ReynaUlt)"]);

                // If no match, default to Red
                if (colorDetection != settingValues["Red"] &&
                    colorDetection != settingValues["Purple"] &&
                    colorDetection != settingValues["Yellow"] &&
                    colorDetection != settingValues["Purple(ReynaUlt)"] &&
                    PurpleOption != null)
                {
                    PurpleOption.IsChecked = true;
                }

                // Shooting Delay radio buttons                 QUIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII
                int shootingDelay = fileSharing.ReadSetting(FileSharing.SettingIndex.ShootingDelay);
                if (VandalOption != null)
                    VandalOption.IsChecked = (shootingDelay == settingValues["VANDAL"]);
                if (GuardianOption != null)
                    GuardianOption.IsChecked = (shootingDelay == settingValues["GUARDIAN"]);
                if (SherifOption != null)
                    SherifOption.IsChecked = (shootingDelay == settingValues["SHERIF"]);
                if (PhantomOption != null)
                    PhantomOption.IsChecked = (shootingDelay == settingValues["PHANTOM"]);
                if (GhostOption != null)
                    GhostOption.IsChecked = (shootingDelay == settingValues["GHOST"]);
                if (NoDelayOption != null)
                    NoDelayOption.IsChecked = (shootingDelay == settingValues["NO DELAY"]);

                // If no match, default to Vandal
                if (shootingDelay != settingValues["VANDAL"] &&
                    shootingDelay != settingValues["GUARDIAN"] &&
                    shootingDelay != settingValues["SHERIF"] &&
                    shootingDelay != settingValues["PHANTOM"] &&
                    shootingDelay != settingValues["GHOST"] &&
                    shootingDelay != settingValues["NO DELAY"] &&
                    VandalOption != null)
                {
                    VandalOption.IsChecked = true;
                }

                // Weapon delay text boxes
                if (VandalDelayTextBox != null)
                    VandalDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayVandal).ToString();
                if (GuardianDelayTextBox != null)
                    GuardianDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGuardian).ToString();
                if (SheriffDelayTextBox != null)
                    SheriffDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelaySheriff).ToString();
                if (PhantomDelayTextBox != null)
                    PhantomDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayPhantom).ToString();
                if (GhostDelayTextBox != null)
                    GhostDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGhost).ToString();
                if (NoDelayTextBox != null)
                    NoDelayTextBox.Text = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayNone).ToString();

                // Key bindings
                int selectedKey = fileSharing.ReadSetting(FileSharing.SettingIndex.SelectedKey);
                int weaponDelayKey = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayKey);

                if (SelectedKeyText != null)
                {
                    string hexString = selectedKey > 0 ? selectedKey.ToString("X") : "None";
                    char keyChar = selectedKey > 0 ? (char)selectedKey : ' ';
                    SelectedKeyText.Text = $"{keyChar} (0x{hexString})";
                }

                if (SelectedKeyText2 != null)
                {
                    string hexString2 = weaponDelayKey > 0 ? weaponDelayKey.ToString("X") : "None";
                    char keyChar2 = weaponDelayKey > 0 ? (char)weaponDelayKey : ' ';
                    SelectedKeyText2.Text = $"{keyChar2} (0x{hexString2})";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetInitialValues: {ex.Message}");
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }


        private void ShowFirstUI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WelcomeText != null)
                    WelcomeText.Text = "Welcome to WPF UI";
                if (MainButton != null)
                    MainButton.Content = "Click me!";

                // Show Close Right elements and hide Open Right elements
                if (CloseRightLabel != null)
                    CloseRightLabel.Visibility = Visibility.Visible;
                if (CloseRightText != null)
                    CloseRightText.Visibility = Visibility.Visible;
                if (OpenRightLabel != null)
                    OpenRightLabel.Visibility = Visibility.Collapsed;
                if (OpenRightText != null)
                    OpenRightText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowFirstUI_Click: {ex.Message}");
            }
        }

        private void ShowSecondUI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WelcomeText != null)
                    WelcomeText.Text = "Welcome to WPF UI - SECONDUI";
                if (MainButton != null)
                    MainButton.Content = "Click me! SECONDUI";

                // Hide Close Right elements and show Open Right elements
                if (CloseRightLabel != null)
                    CloseRightLabel.Visibility = Visibility.Collapsed;
                if (CloseRightText != null)
                    CloseRightText.Visibility = Visibility.Collapsed;
                if (OpenRightLabel != null)
                    OpenRightLabel.Visibility = Visibility.Visible;
                if (OpenRightText != null)
                    OpenRightText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowSecondUI_Click: {ex.Message}");
            }
        }

        // Toggle event handlers with file sharing
        private void StatusToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = "ON";
                    captureService.StatusTb = 1;
                }

                if (fileSharing != null)
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.Status, settingValues["StatusTrue"]);

                Console.WriteLine("Status changed: True");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StatusToggle_Checked: {ex.Message}");
            }
        }

        private void StatusToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = "OFF";
                    captureService.StatusTb = 0;
                }

                if (fileSharing != null)
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.Status, settingValues["StatusFalseStatusFalse"]);

                Console.WriteLine("Status changed: False");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StatusToggle_Unchecked: {ex.Message}");
            }
        }

        private void ShotWhileMovingToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShotWhileMovingText != null)
                    ShotWhileMovingText.Text = "ON";

                if (fileSharing != null)
                {

                    fileSharing.UpdateSetting(FileSharing.SettingIndex.ShotWhileMoving, settingValues["ShotWhileMovingTrue"]);
                    captureService.shotWhileMoving = 1;
                    Console.WriteLine("Shot While Moving changed: True");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShotWhileMovingToggle_Checked: {ex.Message}");
            }
        }

        private void ShotWhileMovingToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShotWhileMovingText != null)
                    ShotWhileMovingText.Text = "OFF";

                if (fileSharing != null)
                {
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.ShotWhileMoving, settingValues["ShotWhileMovingFalse"]);
                    captureService.shotWhileMoving = 0; 
                    Console.WriteLine("Shot While Moving changed: False");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShotWhileMovingToggle_Unchecked: {ex.Message}");
            }
        }

        // View switching methods
        private void ShowUserInfoView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideAllViews();
                if (UserInfoView != null)
                    UserInfoView.Visibility = Visibility.Visible;
                LoadSystemInformation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowUserInfoView_Click: {ex.Message}");
            }
        }


        private void ShowSettingsView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideAllViews();
                if (SettingsView != null)
                    SettingsView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowSettingsView_Click: {ex.Message}");
            }
        }

        private void ShowMacroSettingsView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideAllViews();
                if (MacroSettingsView != null)
                    MacroSettingsView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowMacroSettingsView_Click: {ex.Message}");
            }
        }

        private void ShowGlobalSettingsView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideAllViews();
                if (GlobalSettingsView != null)
                    GlobalSettingsView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowGlobalSettingsView_Click: {ex.Message}");
            }
        }
        // Key detection handlers with file sharing

        private int registeredKeyCode1 = 0;
        private int registeredKeyCode2 = 0;

        private void DetectKeyPress(object sender, KeyEventArgs e)
        {
            try
            {
                // Store the current button for reference
                Button sourceButton = KeyDetectionPopup.Tag as Button;
                if (sourceButton == null)
                {
                    if (KeyDetectionPopup != null)
                        KeyDetectionPopup.IsOpen = false;
                    return;
                }

                // Get the pressed key virtual key code in hexadecimal
                int hexValue = KeyInterop.VirtualKeyFromKey(e.Key);

                // Update the displayed key text and save to file
                string keyName = e.Key.ToString();
                string hexString = $"0x{hexValue:X}"; // Convert to hex format

                if (sourceButton == KeyDetectionButton && SelectedKeyText != null)
                {
                    SelectedKeyText.Text = $"{keyName} ({hexString})";
                    if (fileSharing != null)
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.SelectedKey, hexValue);


                    // Set the key to be tracked globally using KeyboardKey
                    KeyboardKey2.Instance.SetTrackedKey(hexValue);
                }
                else if (sourceButton == KeyDetectionButton2 && SelectedKeyText2 != null)
                {
                    SelectedKeyText2.Text = $"{keyName} ({hexString})";
                    if (fileSharing != null)
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayKey, hexValue);

                    // Set the key to be tracked globally using KeyboardKey
                    KeyboardKey2.Instance.SetTrackedKeyDelay(hexValue);
                }

                // Close the popup
                if (KeyDetectionPopup != null)
                    KeyDetectionPopup.IsOpen = false;

                // Remove the temporary event handlers
                this.PreviewKeyDown -= DetectKeyPress;
                this.PreviewMouseDown -= DetectMousePress;

                // Mark the event as handled
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectKeyPress: {ex.Message}");
                this.PreviewKeyDown -= DetectKeyPress;
                this.PreviewMouseDown -= DetectMousePress;
            }
        }

        private int registeredKeyCode = 0; // Store the registered key code

        int weaponSelected = 0;

        // This function monitors for the registered key

        private void StartKeyOrMouseDetection(object sender, RoutedEventArgs e)
        {
            try
            {
                // Store the button that triggered this for reference in event handlers
                if (KeyDetectionPopup != null)
                {
                    KeyDetectionPopup.Tag = sender;

                    // Show the popup
                    KeyDetectionPopup.IsOpen = true;

                    // Add temporary event handlers
                    this.PreviewKeyDown += DetectKeyPress;
                    this.PreviewMouseDown += DetectMousePress;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartKeyOrMouseDetection: {ex.Message}");
            }
        }
        private void MonitorRegisteredKeyPress2(object sender, KeyEventArgs e)  //weaponDelay
        {
            try
            {//radioButton == VandalOption
             // Convert the pressed key to virtual key code
                int pressedKeyCode = KeyInterop.VirtualKeyFromKey(e.Key);

                // Check if the pressed key matches our registered key
                if (pressedKeyCode == registeredKeyCode)
                {
                    weaponSelected++;
                    if (weaponSelected == 1)
                    {

                        VandalOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayVandal);
                    }
                    else if (weaponSelected == 2)
                    {
                        GuardianOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGuardian);

                    }
                    else if (weaponSelected == 3)
                    {
                        SherifOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelaySheriff);

                    }
                    else if (weaponSelected == 4)
                    {
                        PhantomOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayPhantom);

                    }
                    else if (weaponSelected == 5)
                    {
                        GhostOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGhost);

                    }
                    else if (weaponSelected == 6)
                    {
                        NoDelayOption.IsChecked = true;
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayNone);

                    }
                   

                    // ShootingDelay_Checked(null, null);
                    if (weaponSelected >= 6)
                    {
                        weaponSelected = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MonitorRegisteredKeyPress: {ex.Message}");
            }
        }
        private void StopMonitoring2()
        {
            this.KeyDown -= MonitorRegisteredKeyPress2;
            Console.WriteLine("Stopped monitoring for key presses");
        }

        public void CaptureService_OnKeyPressed()
        {
           //
           //Console.WriteLine($"toggleCaptureMain:  {keybaord.toggleSwitch}");
            keybaord.toggleSwitch++;
            if (keybaord.toggleSwitch % 2 == 1 && captureService.shotingMethod == 1)
            {
                StatusToggle.IsChecked = false;
                captureService.StatusTb = 0;
                StatusText.Text = "OFF";
                testAudio("OFF", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }

            // Handle global key presses here
            if (keybaord.toggleSwitch % 2 == 0 && captureService.shotingMethod == 1)
            {
                // Toggle status
                StatusToggle.IsChecked = true;
                captureService.StatusTb = 1;
                StatusText.Text = "ON";
                testAudio("ON", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            
        }

        public void CaptureService_OnKeyPressedDelay()
        {
            weaponSelected++;
            if (weaponSelected == 1)
            {
                VandalOption.IsChecked = true;
                testAudio("VANDAL", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            else if (weaponSelected == 2)
            {
                GuardianOption.IsChecked = true;
                testAudio("GUARDIAN", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            else if (weaponSelected == 3)
            {
                SherifOption.IsChecked = true;
                testAudio("SHERIF", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            else if (weaponSelected == 4)
            {
                PhantomOption.IsChecked = true;
                testAudio("PHANTOM", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            else if (weaponSelected == 5)
            {
                GhostOption.IsChecked = true;
                testAudio("GHOST", 5);
            }
            else if (weaponSelected == 6)
            {
                NoDelayOption.IsChecked = true;
                testAudio("NO_DELAY", fileSharing.ReadSetting(FileSharing.SettingIndex.AudioRange));
            }
            // ShootingDelay_Checked(null, null);
            if (weaponSelected >= 6)
            {
                weaponSelected = 0;
            }
        }
        private void DetectMousePress(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Store the current button for reference
                Button sourceButton = KeyDetectionPopup.Tag as Button;
                if (sourceButton == null)
                {
                    if (KeyDetectionPopup != null)
                        KeyDetectionPopup.IsOpen = false;
                    return;
                }

                // Map mouse buttons to their virtual key codes (VK codes)
                int hexValue = e.ChangedButton switch
                {
                    MouseButton.Left => 0x01,    // Left Mouse Button
                    MouseButton.Right => 0x02,   // Right Mouse Button
                    MouseButton.Middle => 0x04,  // Middle Mouse Button
                    MouseButton.XButton1 => 0x05, // Extra Mouse Button 1
                    MouseButton.XButton2 => 0x06, // Extra Mouse Button 2
                    _ => 0x00 // Unknown button
                };

                string buttonName = e.ChangedButton.ToString();
                string hexString = $"0x{hexValue:X}"; // Convert to hex format

                // Update the displayed key text and save to file
                if (sourceButton == KeyDetectionButton && SelectedKeyText != null)
                {
                    SelectedKeyText.Text = $"{buttonName} ({hexString})";
                    if (fileSharing != null)
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.SelectedKey, hexValue);


                    // Set the mouse button to be tracked globally
                    KeyboardKey2.Instance.SetTrackedKey(hexValue);

                }
                else if (sourceButton == KeyDetectionButton2 && SelectedKeyText2 != null)
                {
                    SelectedKeyText2.Text = $"{buttonName} ({hexString})";
                    if (fileSharing != null)
                        fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayKey, hexValue);


                    // Set the mouse button to be tracked globally
                    KeyboardKey2.Instance.SetTrackedKey(hexValue);
                }

                // Close the popup
                if (KeyDetectionPopup != null)
                    KeyDetectionPopup.IsOpen = false;

                // Remove the temporary event handlers
                this.PreviewKeyDown -= DetectKeyPress;
                this.PreviewMouseDown -= DetectMousePress;

                // Mark the event as handled
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DetectMousePress: {ex.Message}");
                this.PreviewKeyDown -= DetectKeyPress;
                this.PreviewMouseDown -= DetectMousePress;
            }
        }




        // Method to handle pixel range slider value change with file sharing
        private void PixelRangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                // Get the value directly from the slider (1-10)
                int pixelRange = (int)e.NewValue;

                // Update the pixel range value text
                if (PixelRangeValue != null)
                {
                    PixelRangeValue.Text = pixelRange.ToString();
                }

                // Calculate the mapped value and update in file
                string key = $"PixelRange{pixelRange}";
                if (settingValues.ContainsKey(key) && fileSharing != null)
                {
                    int mappedValue = settingValues[key];
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.PixelRange, mappedValue);
                    Console.WriteLine($"Pixel Range changed to: {pixelRange} (mapped: {mappedValue})");
                }
                else
                {
                    Console.WriteLine($"Pixel Range changed to: {pixelRange}");
                }
                captureService.pixelRange = pixelRange;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PixelRangeSlider_ValueChanged: {ex.Message}");
            }

        }

        private void AudioRangeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                // Get the value directly from the slider (1-10)
                int audioRange = (int)e.NewValue;

                // Update the pixel range value text
                if (AudioRangeValue != null)
                {
                    AudioRangeValue.Text = audioRange.ToString() + "0%";
                }

                // Calculate the mapped value and update in file
                string key = $"AudioRange{audioRange}";
                if (settingValues.ContainsKey(key) && fileSharing != null)
                {
                    int mappedValue = settingValues[key];
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.AudioRange, mappedValue);
                    Console.WriteLine($"A Range changed to: {audioRange} (mapped: {mappedValue})");
                }
                else
                {
                    Console.WriteLine($"Audio Range changed to: {audioRange}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AudioRangeSlider_ValueChanged: {ex.Message}");
            }
        }


        private void HideAllViews()
        {
            try
            {
                if (UserInfoView != null)
                    UserInfoView.Visibility = Visibility.Collapsed;
                if (SettingsView != null)
                    SettingsView.Visibility = Visibility.Collapsed;
                if (MacroSettingsView != null)
                    MacroSettingsView.Visibility = Visibility.Collapsed;
                if (GlobalSettingsView != null)
                    GlobalSettingsView.Visibility = Visibility.Collapsed;
                if (DefaultView != null)
                    DefaultView.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HideAllViews: {ex.Message}");
            }
        }

        private void CloseRightToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                number++;
                if (CloseRightText != null)
                {
                    if (number % 2 == 0)
                    {
                        CloseRightText.Text = "False";
                    }
                    else
                    {
                        CloseRightText.Text = "True";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CloseRightToggle_Click: {ex.Message}");
            }
        }

        // Method to handle shooting method selection changes with file sharing
        private void ShootingMethod_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton radioButton = sender as RadioButton;
                if (radioButton != null && radioButton.IsChecked == true && fileSharing != null)
                {
                    int value = 0;
                    string method = "";

                    if (radioButton == PressButtonOption)
                    {
                        value = settingValues["PressButton"];
                        method = "Press Button";
                        captureService.shotingMethod = 1;
                    }
                    else if (radioButton == HoldButtonOption)
                    {
                        value = settingValues["HoldButton"];
                        method = "Hold Button";
                        captureService.shotingMethod = 2;

                    }

                    // Update the setting in the file
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingMethod, captureService.shotingMethod);
                    Console.WriteLine($"Shooting Method changed: {method} ({value})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShootingMethod_Checked: {ex.Message}");
            }
        }

        // Method to handle color detection selection changes with file sharing
        private void ColorDetection_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton radioButton = sender as RadioButton;
                if (radioButton != null && radioButton.IsChecked == true && fileSharing != null)
                {
                    int value = 0;
                    string color = "";

                    if (radioButton == RedOption)
                    {
                        value = settingValues["Red"];
                        color = "Red";
                        captureService.colorSelected = 1;
                        //    trigger.variableTru = true;
                        //   MessageBox.Show($"variablevalue: {trigger.colorSelected}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (radioButton == PurpleOption)
                    {
                        value = settingValues["Purple"];
                        color = "Purple";
                        captureService.colorSelected = 0;
                        //  MessageBox.Show($"variablevalue: {trigger.colorSelected}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (radioButton == YellowOption)
                    {
                        value = settingValues["Yellow"];
                        color = "Yellow";
                        captureService.colorSelected = 2;
                        //  MessageBox.Show($"variablevalue: {trigger.colorSelected}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (radioButton == PurpleReynaUltOption)
                    {
                        value = settingValues["Purple(ReynaUlt)"];
                        color = "Purple(ReynaUlt)";
                        //   MessageBox.Show($"variablevalue: {trigger.colorSelected}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Update the setting in the file
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.ColorDetection, value);
                    Console.WriteLine($"Color Detection changed: {color} ({value})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ColorDetection_Checked: {ex.Message}");
            }
        }

        // Method to handle shooting delay selection changes with file sharing
        private void ShootingDelay_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton radioButton = sender as RadioButton;
                if (radioButton != null && radioButton.IsChecked == true && fileSharing != null)
                {
                    int value = 0;
                    string weapon = "";

                    if (radioButton == VandalOption)
                    {
                        value = settingValues["VANDAL"];
                        weapon = "VANDAL";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayVandal);
                        //   MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (radioButton == GuardianOption)
                    {
                        value = settingValues["GUARDIAN"];
                        weapon = "GUARDIAN";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGuardian);
                        //   MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                    else if (radioButton == SherifOption)
                    {
                        value = settingValues["SHERIF"];
                        weapon = "SHERIF";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelaySheriff);
                        //  MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                    else if (radioButton == PhantomOption)
                    {
                        value = settingValues["PHANTOM"];
                        weapon = "PHANTOM";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayPhantom);
                        //  MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                    else if (radioButton == GhostOption)
                    {
                        value = settingValues["GHOST"];
                        weapon = "GHOST";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayGhost);
                        //  MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }
                    else if (radioButton == NoDelayOption)
                    {
                        value = settingValues["NO DELAY"];
                        weapon = "NO DELAY";
                        captureService.shotDelay2 = fileSharing.ReadSetting(FileSharing.SettingIndex.WeaponDelayNone);
                        //  MessageBox.Show($"variablevalue: {trigger.shotDelay2}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Update the setting in the file
                    fileSharing.UpdateSetting(FileSharing.SettingIndex.ShootingDelay, value);
                    Console.WriteLine($"Shooting Delay changed: {weapon} ({value})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShootingDelay_Checked: {ex.Message}");
            }
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        // Method to handle weapon delay text changes with file sharing
        private void WeaponDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (fileSharing == null)
                    return;

                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    // Try to parse the text to an integer
                    if (int.TryParse(textBox.Text, out int value))
                    {
                        // Clamp the value to 0-30
                        value = Clamp(value, 0, 1000);
                        // Update the appropriate setting based on which TextBox was changed
                        if (textBox == VandalDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayVandal, value);
                            Console.WriteLine($"Vandal delay set to: {value}");
                        }
                        else if (textBox == GuardianDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayGuardian, value);
                            Console.WriteLine($"Guardian delay set to: {value}");
                        }
                        else if (textBox == SheriffDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelaySheriff, value);
                            Console.WriteLine($"Sheriff delay set to: {value}");
                        }
                        else if (textBox == PhantomDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayPhantom, value);
                            Console.WriteLine($"Phantom delay set to: {value}");
                        }
                        else if (textBox == GhostDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayGhost, value);
                            Console.WriteLine($"Ghost delay set to: {value}");
                        }
                        else if (textBox == NoDelayTextBox)
                        {
                            fileSharing.UpdateSetting(FileSharing.SettingIndex.WeaponDelayNone, value);
                            Console.WriteLine($"No Delay value set to: {value}");
                        }
                    }
                    else
                    {
                        // Handle invalid input
                        string defaultKey = "";
                        FileSharing.SettingIndex settingIndex = FileSharing.SettingIndex.WeaponDelayVandal;
                        bool validSetting = true;

                        if (textBox == VandalDelayTextBox)
                        {
                            defaultKey = "DefaultVandal";
                            settingIndex = FileSharing.SettingIndex.WeaponDelayVandal;
                        }
                        else if (textBox == GuardianDelayTextBox)
                        {
                            defaultKey = "DefaultGuardian";
                            settingIndex = FileSharing.SettingIndex.WeaponDelayGuardian;
                        }
                        else if (textBox == SheriffDelayTextBox)
                        {
                            defaultKey = "DefaultSheriff";
                            settingIndex = FileSharing.SettingIndex.WeaponDelaySheriff;
                        }
                        else if (textBox == PhantomDelayTextBox)
                        {
                            defaultKey = "DefaultPhantom";
                            settingIndex = FileSharing.SettingIndex.WeaponDelayPhantom;
                        }
                        else if (textBox == GhostDelayTextBox)
                        {
                            defaultKey = "DefaultGhost";
                            settingIndex = FileSharing.SettingIndex.WeaponDelayGhost;
                        }
                        else if (textBox == NoDelayTextBox)
                        {
                            defaultKey = "DefaultNoDelay";
                            settingIndex = FileSharing.SettingIndex.WeaponDelayNone;
                        }
                        else
                        {
                            validSetting = false;
                        }

                        int defaultValue = 0;

                        // Reset to default if key exists
                        if (validSetting && !string.IsNullOrEmpty(defaultKey) && settingValues.ContainsKey(defaultKey))
                        {
                            defaultValue = settingValues[defaultKey];
                            textBox.Text = defaultValue.ToString();
                            fileSharing.UpdateSetting(settingIndex, defaultValue);
                        }
                        else
                        {
                            // Fallback to 0 if no default found
                            textBox.Text = "0";
                            if (validSetting)
                                fileSharing.UpdateSetting(settingIndex, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WeaponDelay_TextChanged: {ex.Message}");
            }
        }
        private void LoadSystemInformation()
        {
            try
            {
                // Set static values
                //  if (UserIDText != null)
                //   UserIDText.Text = "EXODUS no user found";

                if (DiscordServerText != null)
                    DiscordServerText.Text = "https://discord.gg/exodusaim";

                if (AdminDSText != null)
                    AdminDSText.Text = "iTERIvEnTAuSTIoNeWaYnaRbandIcuS";

                if (DeveloperDSText != null)
                    DeveloperDSText.Text = "MrRobot1259";

                // Get operating system info
                if (SystemOSText != null)
                    SystemOSText.Text = Environment.OSVersion.VersionString;

                // Get CPU info
                if (CPUText != null)
                {
                    // Use environment variables instead of WMI
                    string processorId = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "";
                    string processorArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "";
                    CPUText.Text = $"{processorId} ({processorArch})";
                }

                // Get RAM info
                if (RAMText != null)
                {
                    // Use GC.GetTotalMemory for a rough estimate
                    long memoryBytes = GC.GetTotalMemory(false);
                    double memoryGB = Math.Round((double)memoryBytes / (1024 * 1024 * 1024), 2);
                    RAMText.Text = $"~{memoryGB} GB";
                }

                // For GPU, we have to set a placeholder since there's no simple way to get it
                if (GPUText != null)
                {
                    GPUText.Text = "Graphics Adapter";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading system information: {ex.Message}");
                // Set default values if there's an error
                if (SystemOSText != null) SystemOSText.Text = "Unknown";
                if (CPUText != null) CPUText.Text = "Unknown";
                if (GPUText != null) GPUText.Text = "Unknown";
                if (RAMText != null) RAMText.Text = "Unknown";
            }
        }

        // Event handler for global key presse
        private void JoinDiscordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the Discord link in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://discord.gg/exodusaim",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Discord link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        bool closedWindow = false;
        // Window closing event to clean up resources
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                fileSharing.WriteToSpecificLine(26, "0");

                // Unsubscribe from events
                if (fileSharing != null)
                {

                    fileSharing.Line20Changed -= OnLine20Changed;

                    // Clean up file sharing resources
                    fileSharing.Dispose();
                    fileSharing = null;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnClosed: {ex.Message}");
            }

            base.OnClosed(e);
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}