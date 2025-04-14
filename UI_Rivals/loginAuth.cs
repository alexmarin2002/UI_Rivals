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
    public class loginAuth
    {




        public static api KeyAuthApp = new api(
    name: "PublicExodus", // App name
    ownerid: "CIxFtyt6uw", // Account ID
    version: "3.0" // Application version. Used for automatic downloads see video here https://www.youtube.com/watch?v=kW195PLCBKs
                   //path: @"Your_Path_Here" // (OPTIONAL) see tutorial here https://www.youtube.com/watch?v=I9rxt821gMk&t=1s
);
        string nameVersion = "public";

        string encodedhwid2 = "", encodedsessionid2 = "", keyResponse = "", bufferRead = "";

        public string key = "";


        public int Width, Height, ArraySize, MipLevels;
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
                return int.Parse(valueStr);
            }

            return 0; // Default value if not found
        }
        public string UrlEncode2(string s)
        {
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

       public string textBoxAfterKey = "";
        public async Task CloudFireAsync()
        {
            // Initialize KeyAuth
            await KeyAuthApp.init();
            await KeyAuthApp.license(key);

            string hwid = KeyAuthApp.user_data.hwid;
            string sessionid = KeyAuth.api.sessionid;
            string encodedhwid = UrlEncode2(hwid);
            string encodedsessionid = UrlEncode2(sessionid);

            keyResponse = key;
            encodedhwid2 = encodedhwid;
            encodedsessionid2 = encodedsessionid;


            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string workerUrl = $"https://auth-redirecting.exodusservices0.workers.dev/?key={key}&sessionid={encodedsessionid}&hwid={encodedhwid}&model={nameVersion}";

                    // Send the request
                    HttpResponseMessage response = await client.GetAsync(workerUrl);

                    // Read the response
                    bufferRead = await response.Content.ReadAsStringAsync();

                    // Process the response - note that the original code does the same thing in both branches
                    textBoxAfterKey = bufferRead;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
            // Equivalent to Sleep(19000) which is commented out in the original

            public async Task SendRequestAsync(string status)
        {

            int buildNumber = 7777;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Define base URL
                    string workerUrl = "https://dsbot.exodusservices0.workers.dev/notify-bot";

                    // Construct query parameters properly
                    string workerUrlWithParams = $"{workerUrl}?status={status}&key={key}&build={buildNumber}";

                    // Send the request
                    HttpResponseMessage response = await client.GetAsync(workerUrlWithParams);

                    // Check if request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        Environment.Exit(0);
                    }
                }
                catch (Exception)
                {
                    Environment.Exit(0);
                }
            }
        }

            public async Task SendRequestAndPrintResponseAsync()
        {


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
                     //   startXServer = ParseInteger(bufferRead, "var3");
                     //   startYServer = ParseInteger(bufferRead, "var4");
                        ArraySize = ParseInteger(bufferRead, "var5");
                        MipLevels = ParseInteger(bufferRead, "var6");
                      //  CountServer = ParseInteger(bufferRead, "var7");
                    }

                    // The commented-out print statements from the original code
                    /*
                    Console.WriteLine($"captureWidthServer: {captureWidthServer}");
                    Console.WriteLine($"screenHeightServer: {screenHeightServer}");
                    Console.WriteLine($"startXServer: {startXServer}");
                    Console.WriteLine($"startYServer: {startYServer}");
                    Console.WriteLine($"ArraySizeServer: {ArraySizeServer}");
                    Console.WriteLine($"MipLevelsServer: {MipLevelsServer}");
                    Console.WriteLine($"CountServer: {CountServer}");
                    */
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

    }
}
