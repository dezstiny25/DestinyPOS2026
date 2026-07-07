using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        // pairing file path
        string pairingFile = string.Empty;

        // If a path was provided as an argument, use it
        if (args.Length > 0)
        {
            if (File.Exists(args[0])) pairingFile = args[0];
            else
            {
                Console.WriteLine("Provided pairing file does not exist: " + args[0]);
                return 1;
            }
        }

        // Try to find pairing_url.txt by walking up from the current working directory
        var cwd = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(cwd, "pairing_url.txt");
            if (File.Exists(candidate))
            {
                pairingFile = candidate;
                break;
            }
            var parent = Directory.GetParent(cwd);
            if (parent == null) break;
            cwd = parent.FullName;
        }

        if (string.IsNullOrEmpty(pairingFile))
        {
            Console.WriteLine("pairing_url.txt not found. Run the WPF app first.");
            return 1;
        }

        var url = File.ReadAllText(pairingFile).Trim();
        if (!url.StartsWith("http://"))
        {
            Console.WriteLine("Invalid pairing URL: " + url);
            return 1;
        }

        var uri = new Uri(url);
        var token = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("token") ?? string.Empty;

        if (uri.Scheme == "http" || uri.Scheme == "https")
        {
            var client = new System.Net.Http.HttpClient();
            var payload = $"{{\"token\": \"{token}\", \"barcode\": \"TEST12345\"}}";
            var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
            try
            {
                Console.WriteLine("POSTing to: " + uri.AbsoluteUri);
                var resp = await client.PostAsync(uri, content);
                var respText = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                Console.WriteLine(respText);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }
        else if (uri.Scheme == "ws" || uri.Scheme == "wss")
        {
            var wsUrl = "ws://" + uri.Authority + "/ws";
            Console.WriteLine("Connecting to: " + wsUrl);
            using var ws = new ClientWebSocket();
            try
            {
                await ws.ConnectAsync(new Uri(wsUrl), System.Threading.CancellationToken.None);
                var payload = $"{{\"token\": \"{token}\", \"barcode\": \"TEST12345\"}}";
                var bytes = Encoding.UTF8.GetBytes(payload);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                Console.WriteLine("Sent payload: " + payload);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }
        else
        {
            Console.WriteLine("Unsupported URI scheme: " + uri.Scheme);
            return 1;
        }

        return 0;
    }
}
