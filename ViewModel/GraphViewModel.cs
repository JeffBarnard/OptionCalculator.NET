using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;
using OptionCalculator.Models;

namespace OptionCalculator
{
    public class GraphViewModel
    {
        private IList<OptionValueGraphSeries> _data;
        //private List<List<double>> _data;
        private int _itemsCount;
        private int _seriesCount;

        public GraphViewModel()
        {
            //this._data = this.FillSampleChartData(this.SeriesCount, this.ItemsCount);

        }

        public IList<OptionValueGraphSeries> CollectionData
        {
            get
            {                
                return this._data;
            }

            set
            {
                _data = value;
            }
        }
        
        public int ItemsCount
        {
            get
            {
                return _itemsCount;
            }
            set
            {
                _itemsCount = value;
            }
        }

        public int SeriesCount
        {
            get
            {
                return _seriesCount;
            }
            set
            {
                _seriesCount = value;
            }
        }
        
    }

   
    public static class SeriesExtensions
    {
        private static double[,] constsY = new double[3, 12] { {24, 9, 18, 31, 25, 13, 17, 33, 21, 28, 19, 11},
                                                              {6, 19, 28, 11, 15, 31, 27, 14, 19, 21, 30, 15},
                                                              {17, 8, 31, 22, 26, 12, 23, 17, 28, 19, 24, 29}};
        
        public static double[] GetUserData(int numberOfItems, int seriesIndex)
        {
            int num = numberOfItems % 12 == 0 ? 12 : numberOfItems % 12;
            int ind = seriesIndex % 3;
            double[] points = new double[num];

            for (int i = 0; i < num; i++)
                points[i] = constsY[ind, i];

            return points;
        }       
              
    }
}

