using System;
using System.Collections.Generic;
using System.Text;

namespace cryptomonitor.models
{
    public class MonitorSet
    {
        public decimal AlertThreshold { get; set; }
        public bool AlertSent { get; set; } = false;
    }
}
