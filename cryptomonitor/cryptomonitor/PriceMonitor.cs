using cryptomonitor.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace cryptomonitor
{
    public class PriceMonitor
    {
        public string TargetCoinId { get; set; }
        public decimal AlertPriceThreshold { get; set; }
        public int PollingIntervalSeconds { get; set; } = 60;

        private const string BaseUrl = "https://api.coingecko.com/api/v3/";

        private static readonly string apitoken = "your token";
        private readonly HttpClient _client;

        public PriceMonitor(string TargetCoinId, decimal AlertPriceThreshold, HttpClient client = null)
        {
            this.TargetCoinId = TargetCoinId;
            this.AlertPriceThreshold = AlertPriceThreshold;
            _client = client ?? new HttpClient();
        }

        public async Task StartMonitoringAsync(CancellationToken ct = default)
        {
            Console.WriteLine($"--- C# cryptocurrency ({TargetCoinId}) monitor started ---");
            Console.WriteLine($"Warning price threshold: ${AlertPriceThreshold:N2}");
            Console.WriteLine($"Polling interval: {PollingIntervalSeconds} s");
            Console.WriteLine("------------------------------------------");

            // CoinGecko API interface URL：receive the price of target coin
            string apiUrl = $"{BaseUrl}simple/price?ids={TargetCoinId}&vs_currencies=usd&x_cg_demo_api_key={apitoken}";

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 1. call API
                    var response1 = await _client.GetAsync(apiUrl, ct);
                    //Processing frequency
                    if ((int)response1.StatusCode == 429)
                    {
                        Console.WriteLine("Rate limit hit. Retrying later...");
                        await Task.Delay(10000, ct);
                        continue;
                    }

                    response1.EnsureSuccessStatusCode();

                    if (response1.IsSuccessStatusCode)
                    {
                        var response = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, decimal>>>(await response1.Content.ReadAsStringAsync());

                        if (response != null && response.TryGetValue(TargetCoinId, out var currencies) && currencies.TryGetValue("usd", out decimal currentPrice))
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {TargetCoinId} real price: ${currentPrice:N2}");

                            // 2. check the condition
                            if (currentPrice >= AlertPriceThreshold)
                            {
                                TriggerAlert(currentPrice);
                            }

                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] API request error: {e.Message}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] occur unknowing error: {ex.Message}");
                    Console.ResetColor();
                }

                // 3. pause 
                await Task.Delay(PollingIntervalSeconds * 1000, ct);
            }
        }
        private void TriggerAlert(decimal price)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Beep(); // optional：Emit a notification sound.
            Console.WriteLine("*************************************************");
            Console.WriteLine($"!! 💥 price Warning：{TargetCoinId} reach or over ${AlertPriceThreshold:N2}。current price: ${price:N2}");
            Console.WriteLine("*************************************************");
            Console.ResetColor();
        }
    }
}
