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
    /// Interaction logic for HistoricalChart.xaml
    /// </summary>
    public partial class HistoricalChart : UserControl
    {
        private GraphViewModel model;
        public HistoricalChart()
        {
            model = new GraphViewModel();
            InitializeComponent();           
        }

        public void SetData(IList<OptionValueGraphSeries> data, List<OptionGraphDeviations> deviations)
        {
            // deviation lines
            MarkedZone mz = new MarkedZone();
            MarkedZone mzmin = new MarkedZone();
            MarkedZone mzmax = new MarkedZone();
            this.RadChart1.DefaultView.ChartArea.Annotations.Clear();

            foreach (var set in deviations)
            {
                byte alpha = 30;    
                foreach (var dev in set.Deviations)
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
                        mzmin.Background = new SolidColorBrush(Colors.Red);
                        mzmax.Background = new SolidColorBrush(Colors.Red);
                        
                    }
                    if (set.Type == OptionValueTypeEnum.Call)
                    {
                        mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 255));
                        //mz.Stroke = new SolidColorBrush(Colors.Blue);
                        mzmin.Background = new SolidColorBrush(Colors.Blue);
                        mzmax.Background = new SolidColorBrush(Colors.Blue);
                    }
                    if (set.Type == OptionValueTypeEnum.Total)
                    {
                        mz.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 255, 0));
                        mzmin.Background = new SolidColorBrush(Colors.Green);
                        mzmax.Background = new SolidColorBrush(Colors.Green);
                    }

                    // find index of strike matching our numerical value
                    var item = data.Where(d => d.Strike == Math.Round(dev.Item1)).FirstOrDefault();
                    var index = data.IndexOf(item);
                    if (index >= 0) {                     
                        mz.StartX = index + 1;
                        mzmin.StartX = index + 1;
                        mzmin.EndX = index + 1 + (double)((double)data.Count / (double)500);// dynamic line thickness
                        this.RadChart1.DefaultView.ChartArea.Annotations.Add(mzmin);
                    }

                    item = data.Where(d => d.Strike == Math.Round(dev.Item2)).FirstOrDefault();
                    index = data.IndexOf(item);
                    if (index == -1)
                    {
                        index = data.Count+2;
                    }
                    else
                    {
                        mzmax.StartX = index + 1;
                        mzmax.EndX = index + 1 - (double)((double)data.Count / (double)500);
                        this.RadChart1.DefaultView.ChartArea.Annotations.Add(mzmax);                   
                    }
                    mz.EndX = index + 1;
                    this.RadChart1.DefaultView.ChartArea.Annotations.Add(mz);

                    alpha -= 3;
                }

                // mean line
                mz = new MarkedZone();
                if (set.Type == OptionValueTypeEnum.Put)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 255, 0, 0));                    
                if (set.Type == OptionValueTypeEnum.Call)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 255));                    
                if (set.Type == OptionValueTypeEnum.Total)
                    mz.Background = new SolidColorBrush(Color.FromArgb(200, 0, 255, 0));                    
                                
                var it = data.Where(d => d.Strike == Math.Round(set.Mean)).FirstOrDefault();
                var ind = data.IndexOf(it);
                mz.StartX = ind + 1;
                mz.EndX = ind + 1 + (double)((double)data.Count / (double)200);
                this.RadChart1.DefaultView.ChartArea.Annotations.Add(mz);
            }


            model.CollectionData = data;
            RadChart1.DefaultView.ChartArea.DataSeries.Clear();
            
            RadChart1.Rebind();

            RadChart1.ItemsSource = model.CollectionData;
            
            RadChart1.Rebind();

            this.RadChart1.UpdateLayout();
            this.RadChart1.DefaultView.ChartArea.InvalidateVisual();
            
            //this.RadChart1.InvalidateArrange();
        }

    }
}
