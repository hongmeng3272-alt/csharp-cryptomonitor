using cryptomonitor.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace cryptomonitor
{
    public class Program
    {
        static void Main(string[] args)
        {
            //testCrypto();
            //Task.Run(() => warningMutiCurrency());
            Console.ReadKey();
        }
        private const string BaseUrl = "https://api.coingecko.com/api/v3/";
        private static readonly HttpClient client = new HttpClient();
        // warning set
        private const int PollingIntervalSeconds = 60;

        private static readonly string apitoken = "your token";

        private static List<MonitorSetting> MonitoringList = new List<MonitorSetting>
        {
            new MonitorSetting { CoinId = "bitcoin", AlertThreshold = 70000 },
            new MonitorSetting { CoinId = "ethereum", AlertThreshold = 3900 },
            new MonitorSetting { CoinId = "solana", AlertThreshold = 200 }
        };

        private const string TelegramBotToken = "your bot Token"; // replace your bot Token
        private const long TelegramChatId = 1L; // replace to your chat id

        private static readonly ITelegramBotClient botClient = new TelegramBotClient(TelegramBotToken);
        /// <summary>
        /// muti currency and sending notifications via Telegram.
        /// </summary>
        private static async Task warningMutiCurrency()
        {
            Console.WriteLine("--- C# The multi-currency cryptocurrency monitor has been activated. ---");
            Console.WriteLine($"current warning {MonitoringList.Count} currencies，polling interval: {PollingIntervalSeconds} 秒");
            Console.WriteLine("-------------------------------------");

            // build ids
            string allCoinIds = string.Join(",", MonitoringList.Select(m => m.CoinId));

            string apiUrl = $"{BaseUrl}simple/price?ids={allCoinIds}&vs_currencies=usd&x_cg_demo_api_key={apitoken}";

            while (true)
            {
                await MonitorPrices(apiUrl);
                await Task.Delay(PollingIntervalSeconds * 1000);
            }
        }
        static async Task MonitorPrices(string apiUrl)
        {
            try
            {

                var response1 = await client.GetAsync(apiUrl);
                if (response1.IsSuccessStatusCode)
                {
                    var priceData = JsonConvert.DeserializeObject<Dictionary<string, Currency>?>(await response1.Content.ReadAsStringAsync());


                    if (priceData == null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WARN] API return empty data。");
                        return;
                    }

                    foreach (var setting in MonitoringList)
                    {
                        if (priceData.TryGetValue(setting.CoinId, out Currency? priceInfo))
                        {
                            decimal currentPrice = priceInfo.Usd;

                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {setting.CoinId.ToUpper()} real price: ${currentPrice:N2}");

                            if (currentPrice >= setting.AlertThreshold && !setting.AlertSent)
                            {
                                TriggerAlert(setting.CoinId, currentPrice, setting.AlertThreshold);
                                await SendTelegramAlert(setting.CoinId, currentPrice, setting.AlertThreshold);
                                setting.AlertSent = true; // Marked as alert sent.
                            }
                            else if (currentPrice < setting.AlertThreshold && setting.AlertSent)
                            {
                                // After the price falls, the warning status will be reset.
                                setting.AlertSent = false;
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {setting.CoinId.ToUpper()} The price has fallen back ${setting.AlertThreshold:N2} below，warning has reseted。");
                            }
                        }
                    }
                }
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

        private static void TriggerAlert(string coinId, decimal price, decimal threshold)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Beep();
            Console.WriteLine("*************************************************");
            Console.WriteLine($"!! 💥 price warning：{coinId.ToUpper()} reach or over ${threshold:N2}. current price: ${price:N2}");
            Console.WriteLine("*************************************************");
            Console.ResetColor();
        }

        /// <summary>
        /// Asynchronously send warning messages to the Telegram account.
        /// </summary>
        private static async Task SendTelegramAlert(string coinId, decimal price, decimal threshold)
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

    }
}
