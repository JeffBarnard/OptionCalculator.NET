using System;
using System.Collections;
using System.Collections.Generic;
using OptionCalculator.Models;
using System.Linq;


namespace OptionCalculator
{
   
    public class Maths
    {
        public static double WeightedAverageOI(IEnumerable<StrikeData> data)
        {
            return data.Sum(c => c.Data.OIOffset * c.Strike) / data.Sum(c => c.Data.OIOffset);
        }

        public static double WeightedAverageValue(Dictionary<double, double> data)
        {
            return data.Sum(c => c.Value * c.Key) / data.Sum(c => c.Value);
        }

        public static double WeightedAverageVol(IEnumerable<StrikeData> data)
        {
            return data.Sum(c => c.Data.Volume * c.Strike) / data.Sum(c => c.Data.Volume);
        }

        //public static double WeightedAverage(Dictionary<double, double> data)
        //{
        //    return data.Sum(c => c.Value * c.Key) / data.Sum(c => c.Value);
        //}

        public static double SumOfSquaresOI(IEnumerable<StrikeData> data, double mean)
        {
            return data.Sum(c => (Math.Pow(c.Strike - mean, 2)) * c.Data.OIOffset);
        }

        public static double SumOfSquaresValue(Dictionary<double, double> data, double mean)
        {
            return data.Sum(c => (Math.Pow(c.Key - mean, 2)) * c.Value);
        }

       

        public static double StdDeviationOI(IEnumerable<StrikeData> data)
        {
            double mean = WeightedAverageOI(data);
            double sumSqr = Maths.SumOfSquaresOI(data, mean);
            double stdDev = Math.Sqrt(sumSqr / (data.Sum(c => c.Data.OIOffset) > 0 ? data.Sum(c => c.Data.OIOffset) : 1));
            return stdDev;
        }

        public static double StdDeviationVol(IEnumerable<StrikeData> data)
        {
            double mean = WeightedAverageVol(data);
            double sumSqr = Maths.SumOfSquaresVol(data, mean);
            double stdDev = Math.Sqrt(sumSqr / (data.Sum(c => c.Data.Volume) > 0 ? data.Sum(c => c.Data.Volume) : 1));
            return stdDev;
        }

        public static double StdDeviationValue(Dictionary<double, double> data)
        {
            double mean = WeightedAverageValue(data);
            double sumSqr = Maths.SumOfSquaresValue(data, mean);
            double stdDev = Math.Sqrt(sumSqr / (data.Sum(c => c.Value) > 0 ? data.Sum(c => c.Value) : 1));
            return stdDev;
        }

               
        //public static double StandardDeviation(ArrayList num)
        //{
        //    double SumOfSqrs = 0;
        //    double avg = Average(num);
        //    for (int i = 0; i < num.Count; i++)
        //    {
        //        SumOfSqrs += Math.Pow(((double)num[i] - avg), 2);
        //    }
        //    double n = (double)num.Count;
        //    return Math.Sqrt(SumOfSqrs / (n - 1));
        //}

        //public static double StandardDeviation(double[] num)
        //{
        //    double Sum = 0.0, SumOfSqrs = 0.0;
        //    for (int i = 0; i < num.Length; i++)
        //    {
        //        Sum += num[i];
        //        SumOfSqrs += Math.Pow(num[i], 2);
        //    }
        //    double topSum = (num.Length * SumOfSqrs) - (Math.Pow(Sum, 2));
        //    double n = (double)num.Length;
        //    return Math.Sqrt(topSum / (n * (n - 1)));
        //}
     
        //public static double Average(double[] num)
        //{
        //    double sum = 0.0;
        //    for (int i = 0; i < num.Length; i++)
        //    {
        //        sum += num[i];
        //    }
        //    double avg = sum / System.Convert.ToDouble(num.Length);

        //    return avg;
        //}
    
        //public static double Average(int[] num)
        //{
        //    double sum = 0.0;
        //    for (int i = 0; i < num.Length; i++)
        //    {
        //        sum += num[i];
        //    }
        //    double avg = sum / System.Convert.ToDouble(num.Length);

        //    return avg;
        //}

        //public static double Average(ArrayList num)
        //{
        //    double sum = 0.0;
        //    for (int i = 0; i < num.Count; i++)
        //    {
        //        sum += (double)num[i];
        //    }
        //    double avg = sum / System.Convert.ToDouble(num.Count);

        //    return avg;
        //}

        /// <summary>
        /// Calculates Normal Distribution or Probability Density given the mean, and standard deviation
        /// </summary>
        /// <param name=”x”>The value for which you want the distribution.</param>
        /// <param name=”mean”>The arithmetic mean of the distribution.</param>
        /// <param name=”deviation”>The standard deviation of the distribution.</param>
        /// <returns>Returns the normal distribution for the specified mean and standard deviation.</returns>
        public static double NormalDistribution(double x, double mean, double deviation)
        {
            return NormalDensity(x, mean, deviation);
        }

        private static double NormalDensity(double x, double mean, double deviation)
        {
            return Math.Exp(-(Math.Pow((x - mean) / deviation, 2) / 2)) / Math.Sqrt(2 * Math.PI) / deviation;
        }
        public static double SumOfSquaresVol(IEnumerable<StrikeData> data, double mean)
        {
            return data.Sum(c => (Math.Pow(c.Strike - mean, 2)) * c.Data.Volume);
        }

        private static double GetStandardDeviation(List<double> doubleList)
        {
            // square root of (sum of (each_value-average)^2) / (count of values  - 1)

            double average = Average(doubleList);
            double sumOfsquares = 0;

            foreach (double value in doubleList)
            {
                sumOfsquares += Math.Pow((value - average), 2);
            }

            double sumOfSquaresAverage = sumOfsquares / (doubleList.Count - 1);

            return Math.Sqrt(sumOfSquaresAverage);
        }

        public static double Average(List<double> doubleList)
        {
            double sum = 0.0;

            foreach (double num in doubleList)
            {
                sum += num;
            }

            double avg = sum / doubleList.Count;

            return avg;
        } 
    }
}