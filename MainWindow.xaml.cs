using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OptionCalculator.Entity;
using OptionCalculator.Models;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OptionCalculator
{
    public partial class MainWindow : Window
    {
        private readonly List<string> _symbolsBs;
        private List<DateTime> _contractsBs;
        private DateTime? _selectedDate;
        private readonly CollectionViewSource _symbolViewSource;
        private readonly CollectionViewSource _contractViewSource;
        private readonly bool _initialized;
        private bool _suppressCalculate;
        private readonly Dictionary<double, double> _totalCallValueByStrike = new Dictionary<double, double>();
        private readonly Dictionary<double, double> _totalPutValueByStrike = new Dictionary<double, double>();
        private readonly Dictionary<double, double> _totalCombinedValueByStrike = new Dictionary<double, double>();

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "Option Calculator " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // find symbols on startup
            using (var context = new OptionModelContainer())
            {
                _symbolsBs = context.Contracts.Select(o => o.Symbol).Distinct().ToList();
                _currentOptionDataDateLabel.Content = context.OptionData.Max(o => o.TimeStamp).ToString("d MMM yyyy");    
            }
            
            _symbolViewSource = (CollectionViewSource)this.FindResource("symbolViewSource");
            _symbolViewSource.Source = _symbolsBs;

            _contractViewSource = (CollectionViewSource)this.FindResource("contractViewSource");

            UpdateContractListForSymbol(_symbolsBs[0]);

            UpdateUI();
            _initialized = true;
        }

        private void UpdateUI()
        {
            if (_maxPainRadio.IsChecked != null && _maxPainRadio.IsChecked.Value)
            {
                datePicker.IsEnabled = false;
                if (findReferenceCheckBox != null)
                findReferenceCheckBox.IsEnabled = false;
            }
            else
            {
                datePicker.IsEnabled = true;
                findReferenceCheckBox.IsEnabled = true;
            }

            if (_currentPainRadio != null && _currentPainRadio.IsChecked.HasValue)
            {
                if (_currentPainRadio.IsChecked.Value && DateTime.Compare((DateTime)_contractViewSource.View.CurrentItem, DateTime.Now) < 0)
                {
                    ExpirationErrorImage.Visibility = Visibility.Visible;
                    calculateButton.IsEnabled = false;
                }
                else
                {
                    ExpirationErrorImage.Visibility = Visibility.Hidden;
                    calculateButton.IsEnabled = true;
                }
            }
        }

        private void _getDataButton_Click(object sender, RoutedEventArgs e)
        {
            YahooQuery yql = new YahooQuery();
            yql.Fetch(null);

            using (var context = new OptionModelContainer())
            {
                _currentOptionDataDateLabel.Content = context.OptionData.Max(o => o.TimeStamp).ToString("d MMM yyyy");
            }
        }
                
        private void symbolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized) 
            { 
                string symbol = string.Empty;
                if (e.AddedItems.Count > 0)
                    symbol = e.AddedItems[0].ToString();
                
                UpdateContractListForSymbol(symbol);               
            }
        }

        private void UpdateContractListForSymbol(string symbol)
        {
            if (_contractViewSource != null)
            {
                using (OptionModelContainer entity = new OptionModelContainer())
                {
                    _contractsBs = entity.Contracts.Where(c => c.Symbol == symbol).OrderByDescending(c => c.Expiration).Select(c => c.Expiration).ToList();
                }
                _contractViewSource.Source = _contractsBs;
            }
        }

        private void contractComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           UpdateUI();
        }
        
        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _selectedDate = (DateTime)e.AddedItems[0];
                this.findReferenceCheckBox.IsChecked = false;
            }
        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            this.datePicker.SelectedDate = null;
            _selectedDate = null;
        }

        private void calculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }
                
        private void CalculateData()
        {
            if (!_suppressCalculate && _initialized)
            {
                Task.Factory.StartNew(()=>
                {
                    this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                    {
                        if (_contractViewSource != null && _symbolViewSource != null)
                        {
                            if (_contractViewSource.View != null && _symbolViewSource.View != null)
                            {
                                if (tabControl1.SelectedIndex == 0)
                                {
                                    // Current
                                    try
                                    {
                                        DateTime latestDataDate = DateTime.Now;
                                        DateTime referenceDate;
                                        // get user input
                                        string symbol = (string)_symbolViewSource.View.CurrentItem;
                                        var contractExpiration = (DateTime)_contractViewSource.View.CurrentItem;

                                        using (var entity = new OptionModelContainer())
                                        {
                                            //latestDataDate = entity.OptionData.Max(d => d.TimeStamp);
                                            var last = entity.StockQuotes.Where(s => s.Stock.Symbol == symbol).OrderByDescending(s => s.TimeStamp).FirstOrDefault();
                                            if (last != null)
                                            {
                                                _stockPriceLabel.Content = last.LastPrice.ToString(".##");
                                                latestDataDate = last.TimeStamp;
                                            }
                                        }

                                        if (_maxPainRadio.IsChecked != null && _maxPainRadio.IsChecked.Value)
                                        {
                                            // default reference date is the date of latest data in database
                                            referenceDate = latestDataDate;

                                            // if we are looking at maxpain for a contract in the past
                                            if (contractExpiration < DateTime.Now)
                                                referenceDate = contractExpiration.AddDays(-2);
                                        }
                                        else
                                        {
                                            if (_selectedDate.HasValue)
                                                _selectedDate = _selectedDate.Value.Date;
                                            else
                                            {
                                                DateTime thirdFriday = GetThirdFriday(DateTime.Now);
                                                // if we have not reached the third friday rollover of current month, reference last months third friday
                                                if (thirdFriday.Day > DateTime.Now.Day)
                                                    thirdFriday = GetThirdFriday(DateTime.Now.AddMonths(-1));
                                                // else use third friday of current month
                                                _selectedDate = thirdFriday;
                                            }
                                            referenceDate = _selectedDate.Value;
                                        }

                                        _referenceDateLabel.Content = referenceDate.ToString("d MMM yyyy");

                                        CalculateValueSeries(symbol, contractExpiration, referenceDate);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                }
                                else
                                {
                                    // Historical
                                }
                            }
                        }
                    });
                });
            }
            
        }

        internal List<OptionBarriers> CalculateBarriers(List<OptionWithDataLocal> options)
        {           
            // top 3 max OI calls
            var maxLatestOICall = (from o in options
                                    where o.Option.Type == "C"
                                   orderby o.Data.OI descending
                                   select o).Take(3);

            // top 3 max OI puts
            var maxLatestOIPut = (from o in options
                                  where o.Option.Type == "P"
                                  orderby o.Data.OI descending
                                  select o).Take(3);

            // top 3 max combined contracts
            var maxLatestOICombined = (from o in options
                                        group o by o.Option.Strike into s
                                        select new { Strike = s.Key, Sum = s.Sum(d => d.Data.OI) }).OrderByDescending(d => d.Sum).Take(3);

            var barriers = new List<OptionBarriers>();

            var isChecked = _putBarrierCheck.IsChecked;
            if (isChecked != null && isChecked.Value)
            {
                barriers.Add(new OptionBarriers()
                {
                    Type = OptionValueTypeEnum.Put,
                    Barriers = new List<double>() { 
                        maxLatestOIPut.First().Option.Strike, 
                        maxLatestOIPut.ElementAtOrDefault(1).Option.Strike, 
                        maxLatestOIPut.ElementAtOrDefault(2).Option.Strike 
                    }
                });
            }

            if (_callBarrierCheck.IsChecked.Value)
            {
                barriers.Add(new OptionBarriers()
                {
                    Type = OptionValueTypeEnum.Call,
                    Barriers = new List<double>() { 
                        maxLatestOICall.First().Option.Strike, 
                        maxLatestOICall.ElementAtOrDefault(1).Option.Strike, 
                        maxLatestOICall.ElementAtOrDefault(2).Option.Strike 
                    }
                });
            }

            // update UI - top 3
            this.PutBarrier1Label.Content = maxLatestOIPut.First().Option.Strike.ToString();
            this.CallBarrier1Label.Content = maxLatestOICall.First().Option.Strike.ToString();
            this.PutBarrier1Label2.Content = maxLatestOIPut.ElementAtOrDefault(1).Option.Strike.ToString();
            this.CallBarrier1Label2.Content = maxLatestOICall.ElementAtOrDefault(1).Option.Strike.ToString();
            this.PutBarrier1Label3.Content = maxLatestOIPut.ElementAtOrDefault(2).Option.Strike.ToString();
            this.CallBarrier1Label3.Content = maxLatestOICall.ElementAtOrDefault(2).Option.Strike.ToString();

            this.highestOILabel.Content = maxLatestOICombined.ElementAtOrDefault(0).Strike.ToString();
            this.highestOIPriceLabel.Content = maxLatestOICombined.ElementAtOrDefault(0).Sum.ToString();
            this.highestOILabel2.Content = maxLatestOICombined.ElementAtOrDefault(1).Strike.ToString();
            this.highestOIPriceLabel2.Content = maxLatestOICombined.ElementAtOrDefault(1).Sum.ToString();
            this.highestOILabel3.Content = maxLatestOICombined.ElementAtOrDefault(2).Strike.ToString();
            this.highestOIPriceLabel3.Content = maxLatestOICombined.ElementAtOrDefault(2).Sum.ToString();

            return barriers;
        }
                
        internal void CalculateValueSeries(string symbol, DateTime expiration, DateTime referenceDate)
        {
            _totalCallValueByStrike.Clear();
            _totalPutValueByStrike.Clear();
            _totalCombinedValueByStrike.Clear();

            using (var entity = new OptionModelContainer())
            {
                var options = entity.Options.GetOptionsForContract(symbol, expiration);

                // get delta between reference date and latest data
                List<OptionWithDataLocal> optionData = OptionCalculator.GetDeltaOptionDataRefDate(symbol, referenceDate, options);

                // filter data by type
                List<StrikeData> callData = (from o in optionData
                                             where o.Option.Type == "C"
                                             select new StrikeData { Strike = o.Option.Strike, Data = o.Data }).ToList();

                List<StrikeData> putData = (from o in optionData
                                            where o.Option.Type == "P"
                                            select new StrikeData { Strike = o.Option.Strike, Data = o.Data }).ToList();

                List<StrikeData> combinedData = (from o in optionData
                                                 group o by o.Option.Strike into g
                                                 select new StrikeData
                                                 {
                                                     Strike = g.Key,
                                                     Data = new OptionDataLocal()
                                                     {
                                                         OI = g.Sum(s => s.Data.OI),
                                                         Timestamp = g.First().Data.Timestamp,
                                                         Volume = g.Sum(r => r.Data.Volume)
                                                     }
                                                 }).ToList();


                // calculate total option value for each strike
                OptionCalculator.CalculateTotalOptionValuesPerStrike(symbol, combinedData, callData, putData, _totalPutValueByStrike, _totalCallValueByStrike);

                // calculate total open interest (volume) by strike
                Dictionary<double, double> callOIbyStrike = callData.ToDictionary(d => d.Strike, d => d.Data.OI);
                Dictionary<double, double> putOIbyStrike = putData.ToDictionary(d => d.Strike, d => d.Data.OI);
                Dictionary<double, double> totalOIbyStrike = combinedData.Distinct().ToDictionary(d => d.Strike, d => d.Data.OI);

                List<OptionValueGraphSeries> combinedList = OptionCalculator.CalculateTotalOpenInterestByStrike(_totalPutValueByStrike, _totalCallValueByStrike, totalOIbyStrike, callOIbyStrike, putOIbyStrike);

                //totalOIbyStrike
                combinedList.OrderBy(d => d.Strike).ToList().ForEach(o => { _totalCombinedValueByStrike[o.Strike] = o.TotalValue; });

                // weighted OI averages and standard deviations
                double callMean = 0,
                       putMean = 0,
                       combinedMean = 0;
                double callStdDev = 0,
                       putStdDev = 0,
                       combinedStdDev = 0;

                if (_callDistributionCheck.IsChecked.Value)
                {
                    callMean = Maths.WeightedAverageOI(callData);
                    callStdDev = Maths.StdDeviationOI(callData);
                }
                else if (_callVOLDistributionCheck.IsChecked.Value)
                {
                    callMean = Maths.WeightedAverageVol(callData);
                    callStdDev = Maths.StdDeviationVol(callData);
                }
                else if (_callValueDistributionCheck.IsChecked.Value)
                {
                    callMean = Maths.WeightedAverageValue(_totalCallValueByStrike);
                }

                if (_putDistributionCheck.IsChecked.Value)
                {
                    putMean = Maths.WeightedAverageOI(putData);
                    putStdDev = Maths.StdDeviationOI(putData);
                }
                else if (_putVOLDistributionCheck.IsChecked.Value)
                {
                    putMean = Maths.WeightedAverageVol(putData);
                    putStdDev = Maths.StdDeviationVol(putData);
                }
                else if (_putValueDistributionCheck.IsChecked.Value)
                {
                    putMean = Maths.WeightedAverageValue(_totalPutValueByStrike);
                }

                if (_totalDistributionCheck.IsChecked.Value)
                {
                    combinedMean = Maths.WeightedAverageOI(combinedData);
                    combinedStdDev = Maths.StdDeviationOI(combinedData);
                }
                else if (_totalVOLDistributionCheck.IsChecked.Value)
                {
                    combinedMean = Maths.WeightedAverageVol(combinedData);
                    combinedStdDev = Maths.StdDeviationVol(combinedData);
                }
                else if (_totalValueDistributionCheck.IsChecked.Value)
                {
                    combinedMean = Maths.WeightedAverageValue(_totalCombinedValueByStrike);
                }

                // build barriers for graphing
                List<OptionBarriers> barriers = CalculateBarriers(optionData);

                // build standard deviations for graphing
                var deviations = new List<OptionGraphDeviations>();
                var halfdeviations = new List<OptionGraphDeviations>();
                if (callMean > 0)
                {
                    deviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(callMean - callStdDev, callMean + callStdDev),
                                new Tuple<double, double>(callMean - (2*callStdDev), callMean + (2*callStdDev)),
                                new Tuple<double, double>(callMean - (3*callStdDev), callMean + (3*callStdDev))
                            },
                        Mean = callMean,
                        Type = OptionValueTypeEnum.Call
                    });
                    halfdeviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(callMean - (0.5*callStdDev), callMean + (0.5*callStdDev)),
                                new Tuple<double, double>(callMean - (1.5*callStdDev), callMean + (1.5 *callStdDev)),
                                new Tuple<double, double>(callMean - (2.5*callStdDev), callMean + (2.5*callStdDev))
                            },
                        Mean = callMean,
                        Type = OptionValueTypeEnum.Call
                    });

                }
                if (putMean > 0)
                {
                    deviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(putMean - putStdDev, putMean + putStdDev),
                                new Tuple<double, double>(putMean - (2*putStdDev), putMean + (2*putStdDev)),
                                new Tuple<double, double>(putMean - (3*putStdDev), putMean + (3*putStdDev))
                        },
                        Mean = putMean,
                        Type = OptionValueTypeEnum.Put
                    });
                    halfdeviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(putMean - (0.5 *putStdDev), putMean + (0.5*putStdDev)),
                                new Tuple<double, double>(putMean - (1.5*putStdDev), putMean + (1.5* putStdDev)),
                                new Tuple<double, double>(putMean - (2.5*putStdDev), putMean + (2.5*putStdDev))
                        },
                        Mean = putMean,
                        Type = OptionValueTypeEnum.Put
                    });
                }

                if (combinedMean > 0)
                {
                    deviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(combinedMean - combinedStdDev, combinedMean + combinedStdDev),
                                new Tuple<double, double>(combinedMean - (2*combinedStdDev), combinedMean + (2*combinedStdDev)),
                                new Tuple<double, double>(combinedMean - (3*combinedStdDev), combinedMean + (3*combinedStdDev))
                            },
                        Mean = combinedMean,
                        Type = OptionValueTypeEnum.Total
                    });
                    halfdeviations.Add(new OptionGraphDeviations
                    {
                        Deviations = new List<Tuple<double, double>>
                            {
                                new Tuple<double, double>(combinedMean - (0.5 * combinedStdDev), combinedMean + (0.5*combinedStdDev)),
                                new Tuple<double, double>(combinedMean - (1.5*combinedStdDev), combinedMean + (1.5*combinedStdDev)),
                                new Tuple<double, double>(combinedMean - (2.5*combinedStdDev), combinedMean + (2.5*combinedStdDev))
                            },
                        Mean = combinedMean,
                        Type = OptionValueTypeEnum.Total
                    });

                }

                //stackChart1.SetData(combinedList.OrderBy(d => d.Strike).ToList(), deviations, barriers);

                // update UI label
                try
                {
                    if (_totalCombinedValueByStrike.Count > 0)
                        this.painLabel.Content =
                            _totalCombinedValueByStrike.Where(
                                t => t.Value == _totalCombinedValueByStrike.Where(d => d.Value != 0).Min(d => d.Value))
                                                       .Select(t => t.Key)
                                                       .First()
                                                       .ToString();

                    if (deviations.Count > 0)
                    {
                        _meanLabel.Content = deviations[0].Mean.ToString("##.##");
                        _minuspoint5StdDevLabel.Content = halfdeviations.Last().Deviations[0].Item1.ToString("##.##");
                        _pluspoint5StdDevLabel.Content = halfdeviations.Last().Deviations[0].Item2.ToString("##.##");
                        _minus1StdDevLabel.Content = deviations.Last().Deviations[0].Item1.ToString("##.##");
                        _plus1StdDevLabel.Content = deviations.Last().Deviations[0].Item2.ToString("##.##");
                        _minus2StdDevLabel.Content = deviations.Last().Deviations[1].Item1.ToString("##.##");
                        _plus2StdDevLabel.Content = deviations.Last().Deviations[1].Item2.ToString("##.##");
                        _minus3StdDevLabel.Content = deviations.Last().Deviations[2].Item1.ToString("##.##");
                        _plus3StdDevLabel.Content = deviations.Last().Deviations[2].Item2.ToString("##.##");
                    }
                }
                catch
                {
                    // log
                }
            }


        }

        private DateTime GetThirdFriday(DateTime date)
        {
            return GetThirdFriday(date.Year, date.Month);
        }

        private DateTime GetThirdFriday(int year, int month)
        {
            var baseDay = new DateTime(year, month, 15);
            int thirdfriday = 15 + ((12 - (int)baseDay.DayOfWeek) % 7);
            return new DateTime(year, month, thirdfriday);
        }

        private void _exclusiveDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _putDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            _suppressCalculate = true;
            _callDistributionCheck.IsChecked = false;
            _totalDistributionCheck.IsChecked = false;
            _suppressCalculate = false;

            CalculateData();
        }

        private void _callDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            _suppressCalculate = true;
            _putDistributionCheck.IsChecked = false;
            _totalDistributionCheck.IsChecked = false;
            _suppressCalculate = false;

            CalculateData();
        }

        private void _totalDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            _suppressCalculate = true;
            _callDistributionCheck.IsChecked = false;
            _putDistributionCheck.IsChecked = false;
            _suppressCalculate = false;

            CalculateData();
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void _putBarrierCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();            
        }

        private void _callBarrierCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _putDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _callDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _totalDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _putBarrierCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _callBarrierCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _maxPainRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_maxcurrentPainLabel != null)
                _maxcurrentPainLabel.Content = "Max Pain:";
            UpdateUI();
        }

        private void _currentPainRadio_Checked(object sender, RoutedEventArgs e)
        {
            
            _maxcurrentPainLabel.Content = "Relative Pain:";
            
            UpdateUI();
        }

        private void _putVOLDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();
            _suppressCalculate = true;
            _callVOLDistributionCheck.IsChecked = false;
            _totalVOLDistributionCheck.IsChecked = false;
            _suppressCalculate = false;
        }

        private void _callVOLDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();
            _suppressCalculate = true;
            _putVOLDistributionCheck.IsChecked = false;
            _totalVOLDistributionCheck.IsChecked = false;
            _suppressCalculate = false;
        }

        private void _totalVOLDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();
            _suppressCalculate = true;
            _callVOLDistributionCheck.IsChecked = false;
            _putVOLDistributionCheck.IsChecked = false;
            _suppressCalculate = false;
        }

        private void _totalValueDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();            
        }

        private void _callValueDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _putValueDistributionCheck_Checked(object sender, RoutedEventArgs e)
        {
            CalculateData();                   
        }

        private void _putVOLDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _callVOLDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _totalVOLDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _putValueDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _callValueDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void _totalValueDistributionCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            CalculateData();
        }

        private void checkBox1_Checked_1(object sender, RoutedEventArgs e)
        {
            _suppressCalculate = true;

            _clearCheckBox.IsChecked = false;
            _callBarrierCheck.IsChecked = false;
            _putBarrierCheck.IsChecked = false;
            _callOpenInterestCheck.IsChecked = false;
            _putOpenInterestCheck.IsChecked = false;
            _totalOpenInterestCheck.IsChecked = false;
            _callVOLDistributionCheck.IsChecked = false;
            _putVOLDistributionCheck.IsChecked = false;
            _totalVOLDistributionCheck.IsChecked = false;
            _callDistributionCheck.IsChecked = false;
            _putDistributionCheck.IsChecked = false;
            _totalDistributionCheck.IsChecked = false;
            _putValueDistributionCheck.IsChecked = false;
            _callValueDistributionCheck.IsChecked = false;
            _totalValueDistributionCheck.IsChecked = false;
            _totalBarrierCheck.IsChecked = false;

            _suppressCalculate = false;

            CalculateData();
        }

        private void _totalBarrierCheck_Checked(object sender, RoutedEventArgs e)
        {
            _putBarrierCheck.IsChecked = true;
            _callBarrierCheck.IsChecked = true;
            CalculateData();
        }

        private void _totalBarrierCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            _putBarrierCheck.IsChecked = false;
            _callBarrierCheck.IsChecked = false;
            CalculateData();
        }
       
    }

}
