using KeyAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Threading;

namespace UI_Rivals
{
    public class LoginAuth
    {
        // Fixed class name to follow C# naming conventions (PascalCase for classes)

        public static api KeyAuthApp = new api(
            name: "PublicExodus", // App name
            ownerid: "CIxFtyt6uw", // Account ID
            version: "3.0" // Application version
        );

        private string nameVersion = "public";
        private string encodedhwid2 = "", encodedsessionid2 = "", keyResponse = "", bufferRead = "";
        public string key = "";
        public string textBoxAfterKey = "";

        // Making these properties to follow C# conventions
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ArraySize { get; private set; }
        public int MipLevels { get; private set; }

        private int ParseInteger(string query, string key)
        {
            string keyPattern = key + "=";
            int start = query.IndexOf(keyPattern);

            if (start != -1)
            {
                start += keyPattern.Length;
                int end = query.IndexOf('&', start);
                if (end == -1) end = query.Length;

                string valueStr = query.Substring(start, end - start);
                if (int.TryParse(valueStr, out int result))
                {
                    return result;
                }
            }

            return 0; // Default value if not found or parsing fails
        }

        public string UrlEncode2(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            StringBuilder escaped = new StringBuilder();

            foreach (char c in s)
            {
                // Keep alphanumeric and other accepted characters unchanged
                if (Char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '~')
                {
                    escaped.Append(c);
                }
                else if (c == ' ')
                {
                    escaped.Append('+');
                }
                else
                {
                    // Any other characters are percent-encoded
                    escaped.AppendFormat("%{0:X2}", (int)c);
                }
            }

            return escaped.ToString();
        }

        public async Task CloudFireAsync()
        {
            try
            {
                // Initialize KeyAuth
                await KeyAuthApp.init();

                if (string.IsNullOrEmpty(key))
                {
                    textBoxAfterKey = "Error: License key is empty";
                    return;
                }

                await KeyAuthApp.license(key);

                // Check if user data is available after license verification
                if (KeyAuthApp.response.success)
                {
                    string hwid = KeyAuthApp.user_data.hwid;
                    string sessionid = KeyAuth.api.sessionid;
                    string encodedhwid = UrlEncode2(hwid);
                    string encodedsessionid = UrlEncode2(sessionid);

                    keyResponse = key;
                    encodedhwid2 = encodedhwid;
                    encodedsessionid2 = encodedsessionid;

                    using (HttpClient client = new HttpClient())
                    {
                        string workerUrl = $"https://auth-redirecting.exodusservices0.workers.dev/?key={key}&sessionid={encodedsessionid}&hwid={encodedhwid}&model={nameVersion}";

                        // Send the request
                        HttpResponseMessage response = await client.GetAsync(workerUrl);

                        // Read the response
                        bufferRead = await response.Content.ReadAsStringAsync();

                        // Store the response
                        textBoxAfterKey = bufferRead;
                    }
                }
                else
                {
                    textBoxAfterKey = "License verification failed: " + KeyAuthApp.response.message;
                }
            }
            catch (Exception ex)
            {
                textBoxAfterKey = $"An error occurred: {ex.Message}";
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public async Task SendRequestAsync(string status)
        {
            if (string.IsNullOrEmpty(key))
                return;

            int buildNumber = 7777;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Define base URL
                    string workerUrl = "https://dsbot.exodusservices0.workers.dev/notify-bot";

                    // Construct query parameters properly
                    string workerUrlWithParams = $"{workerUrl}?status={UrlEncode2(status)}&key={UrlEncode2(key)}&build={buildNumber}";

                    // Send the request
                    HttpResponseMessage response = await client.GetAsync(workerUrlWithParams);

                    // Check if request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.Error.WriteLine($"Request failed with status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Request error: {ex.Message}");
                }
            }
        }

        public async Task SendRequestAndPrintResponseAsync()
        {
            if (string.IsNullOrEmpty(keyResponse) || string.IsNullOrEmpty(encodedsessionid2) || string.IsNullOrEmpty(encodedhwid2))
            {
                textBoxAfterKey = "Error: Missing required parameters. Please run CloudFireAsync first.";
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Build the URL with query parameters
                    string workerUrl = $"https://dumpbytes.exodusservices0.workers.dev/?key={keyResponse}&sessionid={encodedsessionid2}&hwid={encodedhwid2}&model={nameVersion}";

                    // Send the request
                    HttpResponseMessage response = await client.GetAsync(workerUrl);

                    // Read the response content
                    bufferRead = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        textBoxAfterKey = bufferRead;
                    }
                    else
                    {
                        // Parse the response
                        Width = ParseInteger(bufferRead, "var1");
                        Height = ParseInteger(bufferRead, "var2");
                        // Commented out variables that were in the original code but not declared
                        // startXServer = ParseInteger(bufferRead, "var3");
                        // startYServer = ParseInteger(bufferRead, "var4");
                        ArraySize = ParseInteger(bufferRead, "var5");
                        MipLevels = ParseInteger(bufferRead, "var6");
                        // CountServer = ParseInteger(bufferRead, "var7");

                        textBoxAfterKey = $"Width: {Width}, Height: {Height}, ArraySize: {ArraySize}, MipLevels: {MipLevels}";
                    }
                }
                catch (Exception ex)
                {
                    textBoxAfterKey = $"Error: {ex.Message}";
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}