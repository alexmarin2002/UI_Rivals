using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using System.Windows;

namespace UI_Rivals
{
   
    public class ScreenCaptureService : IDisposable
    {

        // RGB structure to match the C++ code
        public struct RGB
        {
            public byte R;
            public byte G;
            public byte B;

            public RGB(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        // HSV structure to match the C++ code
        public struct HSV
        {
            public float h;
            public float s;
            public float v;
        }

        // Convert RGB to HSV, matching the C++ implementation
        public static void RGBtoHSV(RGB rgb, ref HSV hsv)
        {
            float r = rgb.R / 255.0f;
            float g = rgb.G / 255.0f;
            float b = rgb.B / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            // Value
            hsv.v = max;

            // Saturation
            if (max != 0)
                hsv.s = delta / max;
            else
            {
                hsv.s = 0;
                hsv.h = -1; // Undefined
                return;
            }

            // Hue
            if (r == max)
                hsv.h = (g - b) / delta; // Between yellow and magenta
            else if (g == max)
                hsv.h = 2 + (b - r) / delta; // Between cyan and yellow
            else
                hsv.h = 4 + (r - g) / delta; // Between magenta and cyan

            hsv.h *= 60; // Convert to degrees
            if (hsv.h < 0)
                hsv.h += 360;
        }

        public static bool IsPurple(byte red, byte green, byte blue)
        {
            bool isRedInRange = (red >= 100 && red <= 255);
            bool isGreenInRange = (green >= 35 && green <= 200);
            bool isBlueInRange = (blue >= 130 && blue <= 255);

            if (!isRedInRange || !isGreenInRange || !isBlueInRange)
            {
                return false; // Note: fixed the missing return statement from the C++ code
            }

            // stat2 = true; // This line appears to be commented out or unused

            RGB rgb = new RGB(red, green, blue);
            HSV hsv = new HSV();
            RGBtoHSV(rgb, ref hsv);

            bool isHueInRange = ((hsv.h >= 277.0f && hsv.h <= 320.0f) || (hsv.h == -60.0f));
            bool isSaturationInRange = (hsv.s >= 0.270f && hsv.s <= 0.800f);
            bool isValueInRange = (hsv.v >= 0.7f && hsv.v <= 1.0f);
            bool isNotEdgeCase = !(red <= 224 && green >= 90);
            bool isNotEdgeCase2 = !(red <= 160 && red >= 140 && green >= 45 && green <= 60);
            bool isNotEdgeCase3 = !(red <= 160 && red >= 140 && green >= 80 && green <= 89);
            bool isValueInRange2 = (hsv.v >= 0.49f && hsv.v <= 0.63f);
            bool isSaturationInRange2 = (hsv.s >= 0.270f && hsv.s <= 0.69f);
            bool variableReturn = (red >= 120 && red <= 130 && blue >= 130 && blue <= 155 && green >= 30 && green <= 45); // ENEMY 286 / 0.66 / 0.6 || ASTRA

            if ((isHueInRange && isSaturationInRange && isValueInRange &&
                isNotEdgeCase && isNotEdgeCase2))
            {
                return true;
            }

            return false;
        }
        public static bool IsPurple2(byte red, byte green, byte blue)
        {
            // Simpler version with basic thresholds for detecting purple
            return (red > 120 && blue > 120 && green < 100);
        }
        bool acceptPointer = false;
        private Factory1 factory;
        private Adapter1 adapter;
        private SharpDX.Direct3D11.Device device;
        private Output output;
        private Output1 output1;
        private OutputDuplication duplication;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;
        private bool disposed = false;

        public delegate void ColorDetectedHandler();
        public event ColorDetectedHandler OnColorDetected;

        private int screenWidth;
        private int screenHeight;
        private int centerX;
        private int centerY;
        private int pixelRange = 5; // Default pixel range
        private int colorMode = 0; // 0 = Blue, 1 = Red, 2 = Yellow
        private bool isActive = false;
        public static bool IsDatePastMay6th2025()
        {
            // Create a DateTime object for May 6th, 2025
            DateTime targetDate = new DateTime(2025, 4, 6);

            // Get the current date (ignoring time component)
            DateTime currentDate = DateTime.Now.Date;

            // Return true if currentDate is greater than targetDate (i.e., it's past May 6th, 2025)
            return currentDate > targetDate;
        }
        public ScreenCaptureService()
        {
            if (!IsDatePastMay6th2025())
            {
                InitializeDirectX();
                System.Windows.MessageBox.Show($"Initialized Succesfully DirectX:",
                                            "DirectX Error",
                                            System.Windows.MessageBoxButton.OK,
                                            System.Windows.MessageBoxImage.Error);
            }
        }

        private void InitializeDirectX()
        {
            try
            {
                SharedMemoryManager.Instance.Initialize();
                // Create DXGI Factory
                factory = new Factory1();

                // Get primary adapter
                adapter = factory.GetAdapter1(0);

                // Create device with proper feature level
                device = new SharpDX.Direct3D11.Device(
                    adapter,
                    DeviceCreationFlags.BgraSupport,
                    new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 }
                );

                // Get primary output (monitor)
                output = adapter.GetOutput(0);
                output1 = output.QueryInterface<Output1>();

                // Get screen dimensions
                screenWidth = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left;
                screenHeight = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top;
                centerX = screenWidth / 2;
                centerY = screenHeight / 2;

                // Create output duplication
                duplication = output1.DuplicateOutput(device);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error initializing DirectX: {ex.Message}",
                                              "DirectX Error",
                                              System.Windows.MessageBoxButton.OK,
                                              System.Windows.MessageBoxImage.Error);
                Dispose();
            }
        }

        public void SetPixelRange(int range)
        {
            pixelRange = range;
        }

        public void SetColorMode(int mode)
        {
            colorMode = mode;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CaptureLoop(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            if (!isRunning) return;

            cancellationTokenSource?.Cancel();
            isRunning = false;
        }

        private void CaptureLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !disposed)
            {
                try
                {
                    if (isActive) CaptureFrame();
                    Thread.Sleep(1); // Avoid CPU overuse
                }
                catch (SharpDXException ex)
                {
                    // Handle "The desktop has been replaced or DWM has been restarted" error
                    if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code)
                    {
                        ResetDuplication();
                    }
                    else if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                    {
                        // Timeout is normal, just continue
                        continue;
                    }
                    else
                    {
                       System.Windows.MessageBox.Show($"Error during capture: {ex.Message} (Code: {ex.ResultCode.Code})",
                                                      "Capture Error",
                                                      System.Windows.MessageBoxButton.OK,
                                                      System.Windows.MessageBoxImage.Error);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Unexpected error: {ex.Message}",
                                                  "Error",
                                                  System.Windows.MessageBoxButton.OK,
                                                  System.Windows.MessageBoxImage.Error);
                    break;
                }
            }
        }

        private void ResetDuplication()
        {
            try
            {
                // Release current duplication
                duplication?.Dispose();

                // Try to recreate the device and duplication
                device?.Dispose();
                device = new SharpDX.Direct3D11.Device(
                    adapter,
                    DeviceCreationFlags.BgraSupport,
                    new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 }
                );
                duplication = output1.DuplicateOutput(device);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error resetting duplication: {ex.Message}",
                                              "Reset Error",
                                              System.Windows.MessageBoxButton.OK,
                                              System.Windows.MessageBoxImage.Error);
            }
        }
        bool variableTru = true;
        private void CaptureFrame()
        {
           // bool variableTru = true;
            OutputDuplicateFrameInformation frameInfo;
            SharpDX.DXGI.Resource screenResource = null;
            Texture2D screenTexture = null;
            Texture2D cpuTexture = null;

            try
            {
                // Try to acquire the next frame with a reasonable timeout
                duplication.AcquireNextFrame(100, out frameInfo, out screenResource);

                // Check if we actually got a resource
                if (screenResource == null)
                {
                    return;
                }

                // Convert to Texture2D
                screenTexture = screenResource.QueryInterface<Texture2D>();

                // Get the texture description
                var textureDesc = screenTexture.Description;

                // Create a staging texture for CPU access
                var cpuTextureDesc = new Texture2DDescription
                {
                    Width = textureDesc.Width,
                    Height = textureDesc.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = textureDesc.Format,
                    Usage = ResourceUsage.Staging,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                };

                cpuTexture = new Texture2D(device, cpuTextureDesc);

                // Copy the resource to the staging texture
                device.ImmediateContext.CopyResource(screenTexture, cpuTexture);

                // Map texture to access pixels
                var dataBox = device.ImmediateContext.MapSubresource(
                    cpuTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                IntPtr dataPtr = dataBox.DataPointer;
                int stride = dataBox.RowPitch;
                int bytesPerPixel = 4; // BGRA format

                bool colorFound = false;

                // Make sure we stay within bounds
                int minX = Math.Max(0, centerX - pixelRange);
                int maxX = Math.Min(screenWidth - 1, centerX + pixelRange);
                int minY = (centerY - 6);
                int maxY = (centerY - 1);
                if (variableTru)
                {
                  //  MessageBox.Show($"Error initializing screen capture: ", "Error",
                 //             MessageBoxButton.OK, MessageBoxImage.Error);
                    variableTru = false; 
                }
                // Check pixels in a range around the center
                for (int y = minY; y <= maxY && !colorFound; y++)
                {
                    for (int x = minX; x <= maxX && !colorFound; x++)
                    {
                        int pixelOffset = (y * stride) + (x * bytesPerPixel);

                        // Ensure we don't read out of bounds
                        if (pixelOffset + 2 < stride * screenHeight)
                        {
                            byte blue = Marshal.ReadByte(dataPtr, pixelOffset);
                            byte green = Marshal.ReadByte(dataPtr, pixelOffset + 1);
                            byte red = Marshal.ReadByte(dataPtr, pixelOffset + 2);

                         //   if (IsTargetColor(red, green, blue))
                         if (IsPurple(red, green, blue))
                            {
                                SharedMemoryManager.Instance.SetValue(1);

                                //   *pSharedValueInt = 1;
                            }
                        }
                    }
                }

                if (colorFound)
                {
                    // Trigger event on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnColorDetected?.Invoke();
                    });
                }

                // Unmap
                device.ImmediateContext.UnmapSubresource(cpuTexture, 0);
            }
            finally
            {
                // Release resources
                cpuTexture?.Dispose();
                screenTexture?.Dispose();
                screenResource?.Dispose();

                // Important: Release the frame even if an exception occurred
                try { duplication?.ReleaseFrame(); } catch { }
            }
        }

        private bool IsTargetColor(byte red, byte green, byte blue)
        {
            switch (colorMode)
            {
                case 0: // Blue
                    return IsBlue(red, green, blue);
                case 1: // Red
                    return IsRed(red, green, blue);
                case 2: // Yellow
                    return IsYellow(red, green, blue);
                default:
                    return false;
            }
        }

        private bool IsBlue(byte red, byte green, byte blue)
        {
            // Blue has high blue value, lower red and green
            return blue > 180 && red < 130 && green < 130;
        }

        private bool IsRed(byte red, byte green, byte blue)
        {
            // Red has high red value, lower green and blue
            return red > 180 && green < 130 && blue < 130;
        }

        private bool IsYellow(byte red, byte green, byte blue)
        {
            // Yellow has high red and green values, low blue
            return red > 180 && green > 180 && blue < 100;
        }

        public void Dispose()
        {
            if (disposed) return;

            Stop();

            // Release resources in reverse order of creation
            try { duplication?.Dispose(); } catch { }
            try { output1?.Dispose(); } catch { }
            try { output?.Dispose(); } catch { }
            try { device?.Dispose(); } catch { }
            try { adapter?.Dispose(); } catch { }
            try { factory?.Dispose(); } catch { }

            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}