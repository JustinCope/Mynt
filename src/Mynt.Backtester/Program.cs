﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Mynt.Backtester.Models;
using Mynt.Core.Configuration;
using Mynt.Core.Enums;
using Mynt.Core.Exchanges;
using Mynt.Core.Interfaces;
using Mynt.Core.Strategies;

namespace Mynt.Backtester
{
    class Program
    {
        private static BackTestRunner _backTester;
        private static DataRefresher _dataRefresher;

        public static IConfiguration Configuration { get; set; }

        public static BacktestOptions BacktestOptions { get; set; }


        static void Main(string[] args)
        {
            try
            {
                Init();

                WriteIntro();
                Console.WriteLine();
                Console.WriteLine();

                if (!_dataRefresher.CheckForCandleData())
                {
                    WriteColoredLine("\tNo data present. Please retrieve data first.", ConsoleColor.Red);
                    Console.WriteLine();
                }

                PresentMenuToUser();
            }
            catch (Exception ex)
            {
                WriteColoredLine($"\t{ex.Message}", ConsoleColor.Red);
                Console.ReadLine();
            }
        }

        private static void Init()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true);
            Configuration = builder.Build();

            var exchangeOptions = Configuration.Get<ExchangeOptions>();
            BacktestOptions = Configuration.Get<BacktestOptions>();

            _backTester = new BackTestRunner();
            _dataRefresher = new DataRefresher(exchangeOptions);
        }

        private static List<ITradingStrategy> GetTradingStrategies()
        {
            // Use reflection to get all the instances of our strategies.
            var strategyTypes = Assembly.GetAssembly(typeof(BaseStrategy)).GetTypes()
                                     .Where(type => type.IsSubclassOf(typeof(BaseStrategy)))
                                     .ToList();

            var strategies = new List<ITradingStrategy>();

            foreach(var item in strategyTypes)
            {
                strategies.Add((ITradingStrategy)Activator.CreateInstance(item));
            }

            return strategies;
        }

        #region backtesting

        private static void BackTest(ITradingStrategy strategy)
        {
            var runner = new BackTestRunner();
            var results = runner.RunSingleStrategy(strategy, BacktestOptions.Coins, BacktestOptions.StakeAmount, BacktestOptions.OnlyStartNewTradesWhenSold);

            Console.WriteLine();
            Console.WriteLine($"\t=============== BACKTESTING REPORT {strategy.Name.ToUpper()} ===============");
            Console.WriteLine();
            WriteColoredLine($"\tNote: Profit is based on trading with 0.1 BTC each trade.", ConsoleColor.Cyan);
            Console.WriteLine();
            // Prints the results for each coin for this strategy.
            if (results.Count > 0)
            {
                Console.WriteLine(results
                                  .OrderByDescending(x => x.SuccessRate)
                                  .ToList()
                                  .ToStringTable<BackTestResult>(new string[] { "Market", "# Trades", "# Profitable", "Success Rate", "BTC Profit", "Profit %", "Avg. Duration", "Period" },
                                                                 (x) => x.Market,
                                                                 (x) => x.AmountOfTrades,
                                                                 (x) => x.AmountOfProfitableTrades,
                                                                 (x) => $"{x.SuccessRate:0.00}%",
                                                                 (x) => $"{x.TotalProfit:0.00000000}",
                                                                 (x) => $"{x.TotalProfitPercentage:0.00}%",
                                                                 (x) => $"{(x.AverageDuration):0.00} hours",
                                                                 (x) => $"{x.DataPeriod} days"));
            }
            else
            {
                WriteColoredLine("\tNo backtests results found...", ConsoleColor.Red);
            }

            WriteSeparator();
        }

        private static void BackTestShowTrades(ITradingStrategy strategy)
        {
            var runner = new BackTestRunner();
            var results = runner.RunSingleStrategy(strategy, BacktestOptions.Coins, BacktestOptions.StakeAmount, BacktestOptions.OnlyStartNewTradesWhenSold);

            Console.WriteLine();
            Console.WriteLine($"\t=============== BACKTESTING REPORT {strategy.Name.ToUpper()} ===============");
            Console.WriteLine();
            WriteColoredLine($"\tNote: Profit is based on trading with 0.1 BTC each trade.", ConsoleColor.Cyan);
            Console.WriteLine();
            // Prints the results for each coin for this strategy.

            if (results.Count > 0)
            {
                var trades = new List<BackTestTradeResult>();

                foreach(var result in results)
                {
                    trades.AddRange(result.Trades);
                }

                Console.WriteLine(trades
                                  .OrderBy(x => x.StartDate)
                                  .ToList()
                                  .ToStringTable<BackTestTradeResult>(new string[] { "Market", "Open", "Close", "BTC Profit", "Profit %", "Duration", "Startdate", "Enddate" },
                                                                 (x) => x.Market,
                                                                 (x) => $"{x.OpenRate:0.00000000}",
                                                                 (x) => $"{x.CloseRate:0.00000000}",
                                                                 (x) => $"{x.Profit:0.00000000}",
                                                                 (x) => $"{x.ProfitPercentage:0.00}%",
                                                                 (x) => $"{(x.Duration):0.00} hours",
                                                                 (x) => $"{x.StartDate:dd-MM-yyyy hh:mm}",
                                                                 (x) => $"{x.EndDate:dd-MM-yyyy hh:mm}"));
            }
            else
            {
                WriteColoredLine("\tNo backtests results found...", ConsoleColor.Red);
            }

            WriteSeparator();
        }

        private static void BackTestAll()
        {
            var runner = new BackTestRunner();

            Console.WriteLine();
            Console.WriteLine($"\t=============== BACKTESTING REPORT ===============");
            Console.WriteLine();
            WriteColoredLine($"\tNote: Profit is based on trading with 0.1 BTC each trade.", ConsoleColor.Cyan);
            Console.WriteLine();

            var results = new List<BackTestStrategyResult>();

            foreach (var item in GetTradingStrategies())
            {
                var stratResult = new BackTestStrategyResult() { Strategy = item.Name };
                stratResult.Results.AddRange(runner.RunSingleStrategy(item, BacktestOptions.Coins, BacktestOptions.StakeAmount, BacktestOptions.OnlyStartNewTradesWhenSold));
                results.Add(stratResult);
            }

            // Prints the results for each coin for this strategy.
            if (results.Count > 0)
            {
                Console.WriteLine(results
                                  .OrderByDescending(x => x.SuccessRate)
                                  .ToList()
                                  .ToStringTable<BackTestStrategyResult>(new string[] { "Strategy", "# Trades", "# Profitable", "Success Rate", "BTC Profit", "Profit %", "Avg. Duration", "Max. Period" },
                                                                       (x) => x.Strategy,
                                                                       (x) => x.AmountOfTrades,
                                                                       (x) => x.AmountOfProfitableTrades,
                                                                       (x) => $"{x.SuccessRate:0.00}%",
                                                                       (x) => $"{x.TotalProfit:0.00000000}",
                                                                       (x) => $"{x.TotalProfitPercentage:0.00}%",
                                                                       (x) => $"{(x.AverageDuration):0.00} hours",
                                                                       (x) => $"{x.DataPeriod} days"));
            }
            else
            {
                WriteColoredLine("\tNo backtests results found...", ConsoleColor.Red);
            }

            WriteSeparator();
        }

        #endregion

        #region console bootstrapping

        private static void PresentMenuToUser()
        {
            while (true)
            {
                // Write our menu.
                WriteMenu();

                // Get the option the user picked.
                var result = Console.ReadLine();

                WriteSeparator();

                var strats = GetTradingStrategies().OrderBy(x => x.Name).ToList();

                // Depending on the option, pick a menu item.
                switch (result)
                {
                    case "1":
                        for (int i = 0; i < strats.Count; i++)
                        {
                            Console.WriteLine($"\t\t{i + 1}. {strats[i].Name}");
                        }
                        Console.WriteLine();
                        Console.Write("\tWhich strategy do you want to test? ");
                        var index = Convert.ToInt32(Console.ReadLine());

                        if (index - 1 < strats.Count)
                        {
                            Console.WriteLine();
                            Console.WriteLine("\tBacktesting a single strategy. Starting...");
                            BackTest(strats[index - 1]);
                        }
                        else
                        {
                            WriteColoredLine("\tInvalid strategy choice.", ConsoleColor.Red);
                            PresentMenuToUser();
                        }

                        continue;

                    case "2":
                        for (int i = 0; i < strats.Count; i++)
                        {
                            Console.WriteLine($"\t\t{i + 1}. {strats[i].Name}");
                        }
                        Console.WriteLine();
                        Console.Write("\tWhich strategy do you want to test? ");
                        var indexStrategy = Convert.ToInt32(Console.ReadLine());

                        if (indexStrategy - 1 < strats.Count)
                        {
                            Console.WriteLine();
                            Console.WriteLine("\tBacktesting a single strategy. Starting...");
                            BackTestShowTrades(strats[indexStrategy - 1]);
                        }
                        else
                        {
                            WriteColoredLine("\tInvalid strategy choice.", ConsoleColor.Red);
                            PresentMenuToUser();
                        }

                        continue;

                    case "3":
                        Console.WriteLine("\tBacktesting all strategies takes some time and uses historic data. Starting...");
                        BackTestAll();
                        continue;

                    case "4":
                        Console.WriteLine("\tRefreshing...");
                        _dataRefresher.RefreshCandleData(BacktestOptions.Coins, (x) => WriteColoredLine(x, ConsoleColor.Green), BacktestOptions.UpdateCandles, BacktestOptions.CandlePeriod).Wait();
                        ActionCompleted();
                        continue;

                    case "5":
                    default:
                        Environment.Exit(1);
                        break;
                }
                break;
            }
        }

        private static void WriteIntro()
        {
            Console.WriteLine();

            Console.WriteLine(@"                                       $$\");
            Console.WriteLine(@"                                       $$ |");
            Console.WriteLine(@"    $$$$$$\$$$$\  $$\   $$\ $$$$$$$\ $$$$$$\");
            Console.WriteLine(@"    $$  _$$  _$$\ $$ |  $$ |$$  __$$\\_$$  _|");
            Console.WriteLine(@"    $$ / $$ / $$ |$$ |  $$ |$$ |  $$ | $$ |");
            Console.WriteLine(@"    $$ | $$ | $$ |$$ |  $$ |$$ |  $$ | $$ |$$\");
            Console.WriteLine(@"    $$ | $$ | $$ |\$$$$$$$ |$$ |  $$ | \$$$$  |");
            Console.WriteLine(@"    \__| \__| \__| \____$$ |\__|  \__|  \____/");
            Console.WriteLine(@"                  $$\   $$ |");
            Console.WriteLine(@"                  \$$$$$$  |");
            Console.WriteLine(@"                   \______/");
        }

        public static void WriteColoredLine(string line, ConsoleColor color, bool padded = false)
        {
            Console.ForegroundColor = color;
            if (padded) Console.WriteLine();
            Console.WriteLine(line);
            if (padded) Console.WriteLine();
            Console.ResetColor();
        }

        private static void WriteMenu()
        {
            //TimeSpan cacheAge = GetCacheAge();

            Console.WriteLine("\t1. Run a single strategy");
            Console.WriteLine("\t2. Run a single strategy (show all trades)");
            Console.WriteLine("\t3. Run all strategies");
            Console.WriteLine("\t4. Refresh candle data");
            Console.WriteLine("\t5. Close the tool");
            Console.WriteLine();

            //if (cacheAge == TimeSpan.MinValue)
            //    WriteColoredLine("\tCache is empty. You must refresh (6) or copy example data (7).", ConsoleColor.Red);
            //else
            //Console.WriteLine($"\tCache age: {cacheAge}"); // Ofcourse indivual files could differ in time

            Console.WriteLine();
            Console.Write("\tWhat do you want to do? ");
        }

        private static void WriteColored(string line, ConsoleColor color, bool padded = false)
        {
            Console.ForegroundColor = color;
            if (padded) Console.WriteLine();
            Console.Write(line);
            if (padded) Console.WriteLine();
            Console.ResetColor();
        }

        private static void WriteSeparator()
        {
            Console.WriteLine();
            Console.WriteLine("\t============================================================");
            Console.WriteLine();
        }

        private static void ActionCompleted()
        {
            WriteColoredLine("\tCompleted...", ConsoleColor.DarkGreen);
            WriteColoredLine("\tPress key to continue...", ConsoleColor.Gray);
            Console.ReadKey();
            Console.Clear();
            WriteIntro();
            Console.WriteLine();
            Console.WriteLine();
        }

        #endregion
    }
}
