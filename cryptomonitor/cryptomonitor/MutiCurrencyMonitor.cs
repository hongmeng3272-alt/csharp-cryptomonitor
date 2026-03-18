using cryptomonitor.models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace cryptomonitor
{
    public class MutiCurrencyMonitor
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "https://api.coingecko.com/api/v3/";

        private static readonly string apitoken = "your token";
        private readonly ConcurrentDictionary<string, MonitorSet> _targets = new ConcurrentDictionary<string, MonitorSet>();

        public void AddMonitor(string coinId, MonitorSet monitorSet) => _targets[coinId] = monitorSet;

        private const string TelegramBotToken = "your bot Token"; // replace your bot Token
        private const long TelegramChatId = 1L; // replace to your chat id

        //private static readonly ITelegramBotClient botClient = new TelegramBotClient(TelegramBotToken);

        public MutiCurrencyMonitor(HttpClient client = null)
        {
            _client = client ?? new HttpClient();
        }

        public async Task StartGlobalPollingAsync(CancellationToken ct)
        {

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    string allIds = string.Join(",", _targets.Keys);
                    string apiUrl = $"{BaseUrl}simple/price?ids={allIds}&vs_currencies=usd&x_cg_demo_api_key={apitoken}";
                    var response1 = await _client.GetAsync(apiUrl);
                    if (response1.IsSuccessStatusCode)
                    {
                        var priceData = JsonConvert.DeserializeObject<Dictionary<string, Currency>?>(await response1.Content.ReadAsStringAsync());


                        if (priceData == null)
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WARN] API return empty data。");
                            return;
                        }

                        Parallel.ForEach(_targets, async pair =>
                        {
                            if (priceData.TryGetValue(pair.Key, out var current) && current.Usd >= pair.Value.AlertThreshold && !pair.Value.AlertSent)
                            {
                                TriggerAlert(pair.Key, current.Usd, pair.Value.AlertThreshold);
                                //await SendTelegramAlert(pair.Key, current.Usd, pair.Value.AlertThreshold);
                                pair.Value.AlertSent = true; // Marked as alert sent.
                            }
                            else if (priceData.TryGetValue(pair.Key, out var currentLow) && currentLow.Usd < pair.Value.AlertThreshold && pair.Value.AlertSent)
                            {
                                // After the price falls, the warning status will be reset.
                                pair.Value.AlertSent = false;
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {pair.Key.ToUpper()} The price has fallen back ${pair.Value.AlertThreshold:N2} below，warning has reseted。");
                            }
                        });

                    }
                    await Task.Delay(60000, ct);
                }
                catch (JsonException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [FATAL] Unable to parse the API response. Please check the data model.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] occur error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        private void TriggerAlert(string coinId, decimal price, decimal threshold)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Beep();
            Console.WriteLine("*************************************************");
            Console.WriteLine($"!! 💥 price warning：{coinId.ToUpper()} reach or over ${threshold:N2}. current price: ${price:N2}");
            Console.WriteLine("*************************************************");
            Console.ResetColor();
        }
        /*
        private async Task SendTelegramAlert(string coinId, decimal price, decimal threshold)
        {
            string message =
                $"💥 price Warning：{coinId.ToUpper()} reach or over ${threshold:N2}！\n" +
                $"current price: ${price:N2}\n" +
                $"monitoring time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            try
            {
                // Send a text message
                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    text: message,
                    parseMode: ParseMode.Html
                );
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ALERT SENT] Telegram message has sent to Chat ID: {TelegramChatId}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] Telegram sent failed: {ex.Message}");
                Console.ResetColor();
            }
        }
        */
    }
}
