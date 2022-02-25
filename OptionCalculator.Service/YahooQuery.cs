using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using OptionCalculator.Entity;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Configuration;
using PinPoint.Unity.Interfaces;

namespace OptionCalculator
{
    public class YahooQuery
    {
        public void PurgeData()
        {
            // purge data older than 120 days..
            using (var context = new OptionModelContainer())
            {
                var purgeDate = DateTime.Now.Subtract(new TimeSpan(120, 0, 0, 0));
                var contracts = context.Contracts.Where(c => DateTime.Compare(c.Expiration, purgeDate) < 0);
                contracts.ToList().ForEach(c => context.Contracts.DeleteObject(c));
                context.SaveChanges();
            }
        }

        public void Fetch(ILogInfo logInfo)
        {
            PurgeData();

            List<string> symbols = ConfigurationSettings.AppSettings["Symbols"].Split(',').ToList();
            foreach (var sym in symbols)
            {
                try
                {
                    string optionYQL = string.Format("SELECT * FROM yahoo.finance.quotes WHERE symbol='{0}'", sym);
                    string yahooUrl = "http://query.yahooapis.com/v1/public/yql?q=" + optionYQL + "&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";
                    if (logInfo != null)
                        logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Getting stock symbol from yahoo.finance.quotes for symbol: {0}", sym));

                    try
                    {
                        XDocument doc = XDocument.Load(yahooUrl);
                        this.ParseStock(doc);
                    }
                    catch (Exception)
                    {
                        if (logInfo != null)
                            logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Unable to load/parse stock data for symbol {0}", sym));
                    }

                    optionYQL = string.Format("SELECT * FROM yahoo.finance.option_contracts WHERE symbol='{0}'", sym);
                    yahooUrl = "http://query.yahooapis.com/v1/public/yql?q=" + optionYQL + "&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";
                    if (logInfo != null)
                        logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Getting information from yahoo.finance.option_contracts for symbol: {0}", sym));

                    try
                    {
                        XDocument doc = XDocument.Load(yahooUrl);
                        this.ParseContracts(doc);
                    }
                    catch
                    {
                        if (logInfo != null)
                            logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Unable to load/parse contract symbol {0}", sym));
                    }

                    ContractMonth startQueryMonth;

                    // find correct date to query based on available contracts above
                    List<ContractMonth> contractMonths = new List<ContractMonth>();
                    using (var context = new OptionModelContainer())
                    {
                        // get available contract months for symbol
                        var allContractMonths = context.ContractMonths.Where(m => m.Symbol == sym).OrderBy(m => m.Expiration).ToList();

                        // get current month
                        var now = DateTime.Now;
                        var currentContractMonth = allContractMonths.Where(m => m.Expiration.Year == now.Year && m.Expiration.Month == now.Month).FirstOrDefault();
                        var thirdFriday = GetThirdFriday(DateTime.Now);

                        // if we have not reached the third friday rollover
                        if (thirdFriday.Day > DateTime.Now.Day)
                        {
                            // query current month
                            startQueryMonth = currentContractMonth;
                        }
                        else
                        {
                            // else rollover to the next month
                            int i = 0;
                            if (currentContractMonth != null)
                            {
                                foreach (var month in allContractMonths)
                                {
                                    if (month == currentContractMonth)
                                        break;
                                    i++;
                                }
                                int j = i + 1;
                                var nextMonth = allContractMonths.ToList().ElementAt(j);
                                startQueryMonth = nextMonth;
                            }
                            else
                            {
                                var nextMonth = allContractMonths[0];
                                startQueryMonth = nextMonth;
                            }

                        }

                        // get the current month and the subsequent contract month
                        int ind = allContractMonths.IndexOf(startQueryMonth);
                        contractMonths = allContractMonths.GetRange(ind, 2);

                    }

                    foreach (var month in contractMonths)
                    {

                        if (logInfo != null)
                            logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Getting information from yahoo.finance.options for symbol: {0} and expiration: {1}", sym, month.Expiration.ToString("yyyy-MM")));

                        optionYQL = string.Format("SELECT * FROM yahoo.finance.options WHERE symbol='{0}' AND expiration='{1}'", sym, month.Expiration.ToString("yyyy-MM"));
                        yahooUrl = "http://query.yahooapis.com/v1/public/yql?q=" + optionYQL + "&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";

                        try
                        {
                            XDocument doc = XDocument.Load(yahooUrl);
                            this.ParseChain(doc);
                        }
                        catch
                        {
                            if (logInfo != null)
                                logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Unable to load/parse symbol {0}", sym));
                        }
                    }
                }
                catch
                {
                    if (logInfo != null)
                        logInfo.Log("JobScheduler", TraceEventType.Information, string.Format("Error with symbol {0}", sym));                
                }
            }

        }


        private void ParseStock(XDocument doc)
        {
            using (OptionModelContainer context = new OptionModelContainer())
            {

                var quote = from c in doc.Descendants("quote") select c;
                var symbol = quote.Attributes("symbol").FirstOrDefault().Value;
                var lastPrice = quote.Elements("LastTradePriceOnly").FirstOrDefault().Value;
                // create stock and data if not exist
                var stock = context.Stocks.Where(s => s.Symbol == symbol).FirstOrDefault();
                if (stock == null)
                {
                    stock = new Stock()
                    {
                        Id = Guid.NewGuid(),
                        Symbol = symbol,                        
                    };
                    context.Stocks.AddObject(stock);
                    context.SaveChanges();
                }

                DateTime datepart = DateTime.Now.Date;
                var data = context.StockQuotes.Where(s => s.Stock.Symbol == symbol && s.TimeStamp == datepart).FirstOrDefault();
                if (data == null)
                {
                    data = new StockQuote()
                    {
                        Id = Guid.NewGuid(),
                        LastPrice = double.Parse(lastPrice),
                        TimeStamp = DateTime.Now.Date,
                        Stock = stock,
                    };
                    context.StockQuotes.AddObject(data);
                    context.SaveChanges();
                }
            }
        }

        private void ParseContracts(XDocument doc)
        {
            using (OptionModelContainer context = new OptionModelContainer())
            {
                var contractchain = from c in doc.Descendants("option") select c;
                if (contractchain.Any())
                {
                    var tickerSymbol = contractchain.Attributes("symbol").FirstOrDefault().Value;

                    foreach (XElement xmlContract in contractchain.Descendants("contract"))
                    {
                        DateTime expiration = DateTime.Parse(xmlContract.Value);
                        //xmlContract.Value, "yyyyMM", null);

                        // create contract if not exist
                        var contracts = context.ContractMonths.Where(c => c.Symbol == tickerSymbol && c.Expiration == expiration).FirstOrDefault();
                        if (contracts == null)
                        {
                            int maxId = 0;
                            if (context.ContractMonths.Count() > 0)
                                maxId = context.ContractMonths.Max(c => c.Id) + 1;

                            var contract = new ContractMonth()
                            {
                                Id = maxId,
                                Expiration = expiration,
                                Symbol = tickerSymbol
                            };
                            context.ContractMonths.AddObject(contract);
                            context.SaveChanges();
                        };
                    }
                }
            }
        }

        private void ParseChain(XDocument doc)
        {
            using (OptionModelContainer entity = new OptionModelContainer())
            {
                var contractchain = from c in doc.Descendants("optionsChain") select c;
                if (contractchain.Any())
                {
                    var tickerSymbol = contractchain.Attributes("symbol").FirstOrDefault().Value;

                    foreach (XElement xmlOption in contractchain.Descendants("option"))
                    {
                        // parse expiration date
                        string optionName = xmlOption.Attribute("symbol").Value;
                        Regex dateparse = new Regex("[C|P]", RegexOptions.RightToLeft);
                        // SPY7131221C00135000
                        Match datematch = dateparse.Match(optionName);
                        DateTime expiration = DateTime.ParseExact(optionName.Substring(datematch.Index-6, 6), "yyMMdd", null);

                        // create contract if not exist
                        var contract = entity.Contracts.Where(c => c.Symbol == tickerSymbol && c.Expiration == expiration).FirstOrDefault();
                        if (contract == null)
                        {
                            contract = new Contract()
                            {
                                Id = Guid.NewGuid(),
                                Expiration = expiration,
                                Symbol = tickerSymbol
                            };
                            entity.Contracts.AddObject(contract);
                            entity.SaveChanges();
                        };

                        // create option if not exist
                        var option = entity.Options.Where(c => c.Name == optionName).FirstOrDefault();
                        if (option == null)
                        {
                            option = new Option()
                            {
                                id = Guid.NewGuid(),
                                Name = optionName,
                                Type = xmlOption.Attribute("type").Value,
                                Strike = decimal.Parse(xmlOption.Element("strikePrice").Value),
                                ContractNav = contract
                            };
                            entity.Options.AddObject(option);
                            entity.SaveChanges();
                        }

                        // daily data

                        // determine if first reference date
                        var referencedate = !entity.OptionData.Any(o => o.Option == option.id);
                        OptionData optionData = new OptionData()
                        {
                            id = Guid.NewGuid(),
                            Ask = xmlOption.Element("ask").Value == "NaN" ? 0 : decimal.Parse(xmlOption.Element("ask").Value),
                            Bid = xmlOption.Element("bid").Value == "NaN" ? 0 : decimal.Parse(xmlOption.Element("bid").Value),
                            Change = xmlOption.Element("change").Value == "NaN" ? 0 : decimal.Parse(xmlOption.Element("change").Value),
                            OptionNav = option,
                            //LastPrice = xmlOption.Element("lastPrice").Value == "NaN" ? 0 : decimal.Parse(xmlOption.Element("lastPrice").Value),
                            OpenInterest = decimal.Parse(xmlOption.Element("openInt").Value),
                            Vol = xmlOption.Element("vol").Value == "NaN" ? 0 : decimal.Parse(xmlOption.Element("vol").Value),
                            TimeStamp = DateTime.Now.Hour < 16 ? DateTime.Now.AddHours(-24).Date : DateTime.Now.Date,
                            Reference = referencedate
                        };

                        bool exists = false;
                        if (entity.OptionData.Count() > 0)
                            exists = entity.OptionData.Where(o => o.Option == option.id && o.TimeStamp == optionData.TimeStamp).Count() > 0;

                        if (!exists)
                            entity.OptionData.AddObject(optionData);

                    }

                    entity.SaveChanges();
                }
            }
        }

        private DateTime GetThirdFriday(DateTime date)
        {
            return GetThirdFriday(date.Year, date.Month);
        }

        private DateTime GetThirdFriday(int year, int month)
        {
            DateTime baseDay = new DateTime(year, month, 15);
            int thirdfriday = 15 + ((12 - (int)baseDay.DayOfWeek) % 7);
            return new DateTime(year, month, thirdfriday);
        }

        private static decimal? GetDecimal(string input)
        {
            if (input == null) return null;

            input = input.Replace("%", "");

            decimal value;

            if (Decimal.TryParse(input, out value)) return value;
            return null;
        }

        private static DateTime? GetDateTime(string input)
        {
            if (input == null) return null;

            DateTime value;

            if (DateTime.TryParse(input, out value)) return value;
            return null;
        }
    }

}
