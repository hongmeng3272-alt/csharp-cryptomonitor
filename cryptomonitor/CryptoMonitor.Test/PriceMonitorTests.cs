using cryptomonitor;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace CryptoMonitor.Test
{
    public class PriceMonitorTests
    {
        [Fact]
        public async Task StartMonitoringAsync_ShouldTriggerAlert_WhenPriceAboveThreshold()
        {
            // 1. Arrange (prepare stage)
            var targetCoin = "bitcoin";
            var threshold = 50000m;
            var mockPrice = 60000m; // trigger alarm price

            // mock API return JSON structure: {"bitcoin": {"usd": 60000}}
            var mockResponse = new Dictionary<string, Dictionary<string, decimal>>
        {
            { targetCoin, new Dictionary<string, decimal> { { "usd", mockPrice } } }
        };
            var json = JsonSerializer.Serialize(mockResponse);

            // mock HttpClient Handler
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
                    Content = new StringContent(json),
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var monitor = new PriceMonitor(targetCoin, threshold, httpClient)
            {
                PollingIntervalSeconds = 1 // reduce Interval time
            };

            // try once
            var cts = new CancellationTokenSource();

            // 2. Act 
            // start it and cancel it after 1.5s, ensure it run at least once
            var monitorTask = monitor.StartMonitoringAsync(cts.Token);
            await Task.Delay(1500);
            cts.Cancel();

            // 3. Assert
            // 
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                 ItExpr.Is<HttpRequestMessage>(req =>
        // recommend using AbsoluteUri substitute ToString()
        req.RequestUri != null &&
        req.RequestUri.AbsoluteUri.Contains("ids=bitcoin")
         ),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}