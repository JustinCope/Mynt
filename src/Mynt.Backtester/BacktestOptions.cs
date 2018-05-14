﻿using System;
using System.Collections.Generic;

namespace Mynt.Backtester
{
    public class BacktestOptions
    {
        public decimal StakeAmount { get; set; } = 0.1m;
        public bool OnlyStartNewTradesWhenSold { get; set; } = true;
        public List<string> Coins { get; set; } = new List<string> { };
        public int CandlePeriod { get; set; } = 60;
        public bool UpdateCandles { get; set; } = true;
        public string StartDate { get; set; } = "2018-01-01";
    }
}
