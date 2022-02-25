using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using OptionCalculator.Models;
using Telerik.Windows.Controls.Charting;

namespace OptionCalculator
{
    /// <summary>
    /// Interaction logic for StackChart.xaml
    /// </summary>
    public partial class StackChart : UserControl
    {
        private readonly GraphViewModel _model;

        public StackChart()
        {
            _model = new GraphViewModel();
            InitializeComponent();           
        }

        public void SetData(IList<OptionValueGraphSeries> data, List<OptionGraphDeviations> deviations, List<OptionBarriers> barriers)
        {   
            double smallThickness = (double)data.Count / (double)200; // scaled line thickness
            double largeThickness = (double)data.Count / (double)500;

            #region Deviation lines

            var mz = new MarkedZone();
            var mzmin = new MarkedZone();
            var mzmax = new MarkedZone();
            this.RadChart1.DefaultView.ChartArea.Annotations.Clear();

            foreach (var set in deviations)
            {
                byte alpha = 30;    
                foreach (var dev in set.Deviations)
                {
                    // same as mean, don't draw
                    if (dev.Item1 != set.Mean)
                    {
                        mz = new MarkedZone();
                        mzmin = new MarkedZone();
                        mzmax = new MarkedZone();
                        mzmin.StrokeThickness = 1;
                        mzmax.StrokeThickness = 1;

                        ToolTipService.SetToolTip(mz, "hello");

                        if (set.Type == OptionValueTypeEnum.Put)
                        {
                            mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 255, 0, 0));
                            mzmin.Background = new SolidColorBrush(Color.FromArgb(200, 229, 111, 128));
                            mzmax.Background = new SolidColorBrush(Color.FromArgb(200, 229, 111, 128)); 
                        }
                        if (set.Type == OptionValueTypeEnum.Call)
                        {
                            mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 192, 255));
                            mzmin.Background = new SolidColorBrush(Color.FromArgb(200, 0, 192, 255));
                            mzmax.Background = new SolidColorBrush(Color.FromArgb(200, 0, 192, 255));
                        }
                        if (set.Type == OptionValueTypeEnum.Total)
                        {
                            mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 64, 64, 255));
                            mzmin.Background = new SolidColorBrush(Color.FromArgb(200, 64, 64, 255));
                            mzmax.Background = new SolidColorBrush(Color.FromArgb(200, 64, 64, 255));
                        }

                        // find index of strike matching our numerical value
                        //var item = data.Where(d => d.Strike == Math.Round(dev.Item1)).FirstOrDefault();
                        OptionValueGraphSeries item = FindClosest(data, dev.Item1);
                        var index = data.IndexOf(item);
                        if (index >= 0)
                        {
                            mz.StartX = index + 1;
                            mzmin.StartX = index + 1;
                            mzmin.EndX = index + 1 + largeThickness;
                            this.RadChart1.DefaultView.ChartArea.Annotations.Add(mzmin);
                        }

                        item = FindClosest(data, dev.Item2);
                        index = data.IndexOf(item);
                        if (index == -1)
                        {
                            index = data.Count;
                            //mz.EndX = index + 1 + (double)0.4;
                        }
                        else
                        {
                            mzmax.StartX = index + 1;
                            mzmax.EndX = index + 1 - largeThickness;
                            mz.EndX = index + 1;

                            this.RadChart1.DefaultView.ChartArea.Annotations.Add(mzmax);
                        }

                        this.RadChart1.DefaultView.ChartArea.Annotations.Add(mz);

                        alpha -= 3;
                    }
                }

                // mean line
                mz = new MarkedZone();
                if (set.Type == OptionValueTypeEnum.Put)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 255, 90, 90));                                                  
                if (set.Type == OptionValueTypeEnum.Call)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 0, 192, 225));
                if (set.Type == OptionValueTypeEnum.Total)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 64, 64, 255));  

                var meanindex = FindClosest(data, set.Mean); // data.Where(d => d.Strike == Math.Round(set.Mean)).FirstOrDefault();
                var meandataindex = data.IndexOf(meanindex);
                mz.StartX = meandataindex + 1;
                mz.EndX = meandataindex + 1 + smallThickness;

                this.RadChart1.DefaultView.ChartArea.Annotations.Add(mz);
            }

            #endregion

            #region Barriers

            foreach (var barrier in barriers)
            {
                byte alpha = 200;
                foreach (var bar in barrier.Barriers)
                {
                    mz = new MarkedZone();
                    if (barrier.Type == OptionValueTypeEnum.Put)
                        mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 235, 0, 235)); // magenta
                    
                    if (barrier.Type == OptionValueTypeEnum.Call)
                        mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 255, 0));
                    
                    OptionValueGraphSeries item = FindClosest(data, bar);
                    var index = data.IndexOf(item) + 1;
                    mz.StartX = index - smallThickness;
                    mz.EndX = index + smallThickness;
                    this.RadChart1.DefaultView.ChartArea.Annotations.Add(mz);
                    alpha -= 80;
                }
            }
            #endregion

            _model.CollectionData = data;
            
            RadChart1.DefaultView.ChartArea.DataSeries.Clear();            
            RadChart1.Rebind();
            RadChart1.ItemsSource = _model.CollectionData;            
            RadChart1.Rebind();
            RadChart1.UpdateLayout();
            RadChart1.DefaultView.ChartArea.InvalidateVisual();
                        
        }

        private OptionValueGraphSeries FindClosest(IEnumerable<OptionValueGraphSeries> numbers, double value)
        {
            //Math.Round(dvalue, 1, MidpointRounding.AwayFromZero);
            //Math.Round(value / 0.5) * 0.5; // round to nearest .5
            var rounded = (double)Round((decimal)value, 1);
            
            return  (from number in numbers
                    let difference = Math.Abs(number.Strike - rounded) 
                    let max = numbers.Select(s=>s.Strike).Max()
                    let min = numbers.Select(s=>s.Strike).Min()
                    orderby difference, Math.Abs(number.Strike) descending
                    where rounded <= max && rounded >= min
                    select number)
                    .FirstOrDefault();

            //return numbers.Select(number => new { number, difference = Math.Abs(number.Strike - value) })
            //             .Select(x1 => new { x1, max = numbers.Select(s => s.Strike).Max() })
            //             .Select(y => new { y, min = numbers.Select(s => s.Strike).Min() })
            //             .OrderBy(z => z.y.x1.difference)
            //             .OrderByDescending(z => Math.Abs(z.y.x1.number.Strike))
            //             .Where(z => value <= z.y.max && value >= z.min)
            //             .Select(z => z.y.x1.number)
            //             .FirstOrDefault();
        }      
       
        /// <summary>
        /// Rounds the decimal value to round to a specified number of decimals.
        /// Does not use Bankers Rounding.
        /// </summary>
        /// <param name="value">The value to round</param>
        /// <param name="digits">The number of digits to round to</param>
        /// <returns>The rounded value</returns>
        public static decimal Round(decimal value, int digits) {
            return System.Math.Round(value + Convert.ToDecimal(
            System.Math.Sign(value)/System.Math.Pow(10, digits+1)), digits);
        }
    
    }
}
