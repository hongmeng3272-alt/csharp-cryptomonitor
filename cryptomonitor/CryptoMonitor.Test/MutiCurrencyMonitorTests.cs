using cryptomonitor;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitor.Test
{
    public class MutiCurrencyMonitorTests
    {
        [Fact]
        public async Task RunBatchMonitor_ShouldProcessMultipleCoinsCorrectly()
        {
            // 1. Arrange
            var btcId = "bitcoin";
            var ethId = "ethereum";

            // Mock Response: BTC hits 60k (Alert!), ETH is at 2k (Below Alert 3k)
            var mockPrices = new Dictionary<string, Dictionary<string, decimal>>
        {
            { btcId, new Dictionary<string, decimal> { { "usd", 60000m } } },
            { ethId, new Dictionary<string, decimal> { { "usd", 2000m } } }
        };
            string jsonResponse = JsonConvert.SerializeObject(mockPrices);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse),
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var mutiCurrency = new MutiCurrencyMonitor(httpClient);

            // Add two coins to the coordinator
            mutiCurrency.AddMonitor(btcId, new cryptomonitor.models.MonitorSet() { AlertThreshold = 50000m, AlertSent = true }); // Threshold 50k
            mutiCurrency.AddMonitor(ethId, new cryptomonitor.models.MonitorSet() { AlertThreshold = 3000m, AlertSent = true });  // Threshold 3k

            var cts = new CancellationTokenSource();

            // 2. Act
            // Run the batch monitor and cancel after a short delay
            var runTask = mutiCurrency.StartGlobalPollingAsync(cts.Token);
            await Task.Delay(500); // Give it time to complete one loop
            cts.Cancel();

            // 3. Assert
            // Verify that the HTTP call contained BOTH IDs in the query string
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri.AbsoluteUri.Contains(btcId) &&
                    req.RequestUri.AbsoluteUri.Contains(ethId)),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
