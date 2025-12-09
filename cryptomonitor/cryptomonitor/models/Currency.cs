using System;
using System.Text.Json.Serialization;

namespace cryptomonitor.models
{
    public class Currency
    {
        // 反序列化时，将 JSON 键 "usd" 映射到 Usd 属性
        [JsonPropertyName("usd")]
        public decimal Usd { get; set; }
    }
}
