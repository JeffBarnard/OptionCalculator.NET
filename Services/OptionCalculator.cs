using OptionCalculator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionCalculator
{
    internal static class OptionCalculator
    {
        /// <summary>
        /// Calculate graph barriers for option strikes
        /// </summary>
        /// <param name="options"></param>
        internal static void CalculateBarriers(List<OptionWithDataLocal> options)
        {
            // Redacted for GitHub
            //////////////////////////////////////////////
            /////
            ///

            // top 3 max OI calls
           

            // top 3 max OI puts
           

            // top 3 max combined contracts
          

        }

        /// <summary>
        /// Get reference date
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="referenceDate"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static List<OptionWithDataLocal> GetDeltaOptionDataRefDate(string symbol, DateTime referenceDate, IQueryable<Entity.Option> options)
        {
            // Redacted for GitHub
            //////////////////////////////////////////////
            /////
            ///
            ///

            var optionData = (from op in options
                              let latest = (from d in op.OptionData
                                            orderby d.TimeStamp descending
                                            select d).FirstOrDefault() //d.OptionData.OrderByDescending(o => o.TimeStamp).FirstOrDefault()
                              let reference = (from d in op.OptionData
                                               where DateTime.Compare(d.TimeStamp, referenceDate) >= 0
                                               orderby d.TimeStamp
                                               select d).FirstOrDefault() //op.OptionData.Where(o => o.TimeStamp >= referenceDate).OrderBy(o => o.TimeStamp).FirstOrDefault()
                              where (op.Name.Length - symbol.Length) == 15 // filter out errornous yahoo duplicates
                                      && (op.Strike > 1) // filter out errornous strikes
                                      && reference != null
                              select new OptionWithDataLocal
                              {
                                  Option = new OptionLocal()
                                  {
                                      Strike = (double)op.Strike,
                                      Type = op.Type
                                  },
                                  Data = new OptionDataLocal()
                                  {
                                      Timestamp = (reference == null ? new DateTime?() : reference.TimeStamp),
                                      Volume = (double)latest.Vol,
                                      //OpenInterest = reference == null ? 0 : (double)latest.OpenInterest - (((double)reference.OpenInterest == (double)latest.OpenInterest) ? 0 : (double)reference.OpenInterest)
                                      OI = (DateTime.Equals(latest.TimeStamp, reference.TimeStamp)) ? (double)reference.OpenInterest : ((double)latest.OpenInterest - (double)reference.OpenInterest)
                                  }
                              }).OrderBy(d => d.Option.Strike).ToList();
            return optionData;
        }


        /// <summary>
        /// Calculate total option values for each strike price
        /// </summary>
        internal static void CalculateTotalOptionValuesPerStrike(string symbol, List<StrikeData> combinedData, List<StrikeData> callData, List<StrikeData> putData, Dictionary<double, double> totalPutValueByStrike, Dictionary<double, double> totalCallValueByStrike)
        {
            // Redacted for GitHub
            //////////////////////////////////////////////
            /////
            ///

            // foreach strike, descending        
           
               

            // foreach strike, ascending
           
        }

        /// <summary>
        /// Open interest (volume) by strike
        /// </summary>
        /// <param name="totalPutValueByStrike"></param>
        /// <param name="totalCallValueByStrike"></param>
        /// <param name="totalOIbyStrike"></param>
        /// <param name="callOIbyStrike"></param>
        /// <param name="putOIbyStrike"></param>
        /// <returns></returns>
        internal static List<OptionValueGraphSeries> CalculateTotalOpenInterestByStrike(Dictionary<double, double> totalPutValueByStrike, Dictionary<double, double> totalCallValueByStrike, Dictionary<double, double> totalOIbyStrike, Dictionary<double, double> callOIbyStrike, Dictionary<double, double> putOIbyStrike)
        {
            // Redacted for GitHub
            //////////////////////////////////////////////
            /////
            ///

            var combinedList = new List<OptionValueGraphSeries>();


            return combinedList;
        }
    }
}
