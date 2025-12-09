using System;
using System.Collections.Generic;
using System.Text;

namespace cryptomonitor.models
{
    public class MonitorSetting
    {
        public string CoinId { get; set; } = string.Empty;
        public decimal AlertThreshold { get; set; }
        public bool AlertSent { get; set; } = false; // 新增：避免重复发送预警
    }
}
