using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace cryptomonitor.models
{
    public class CoinPrice
    {
        [JsonPropertyName("bitcoin")]
        public Currency? Bitcoin { get; set; }
    }
}
