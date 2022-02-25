using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionCalculator.Entity;

namespace OptionCalculator.Models
{    

    // local models
    public class OptionLocal
    {    
         public string Type { get; set; }
         public double Strike { get; set; }
    }

    public class OptionDataLocal
    {
        public double Volume { get; set; }
        public double OI { get; set; }
        public double OIOffset
        {          
            get
            {
                // ignore negatives
                if (OI < 0)
                    return 0;
                else
                    return OI;
                //if (OI == 0)
                //    return 0;
                //else
                //    return OI + Math.Abs(Offset);
            }
        }
        public double Offset { get; set; }
        public DateTime? Timestamp { get; set; }    
    }

    public class OptionWithDataLocal
    {
        public OptionLocal Option { get; set; }
        public OptionDataLocal Data { get; set; }
    }

    public class StrikeData
    {
        public double Strike { get; set; }
        public OptionDataLocal Data { get; set; }
    }

    public enum OptionValueTypeEnum
    {
        Put,
        Call,
        Total
    }

    public class OptionGraphDeviations
    {
        public List<Tuple<double, double>> Deviations { get; set; }
        public double Mean { get; set; }
        public OptionValueTypeEnum Type { get; set; }
    }

    public class OptionBarriers
    {
        public List<double> Barriers { get; set; }        
        public OptionValueTypeEnum Type { get; set; }
    }

    // main graph Series
    public class OptionValueGraphSeries 
    {
        public double Strike { get; set; }
        public double TotalValue { get; set; }
        public double PutValue { get; set; }
        public double CallValue { get; set; }
        
        public double TotalOI { get; set; }
        public bool TotalOIVisible { get; set; }
        public double PutOI { get; set; }
        public bool PutOIVisible { get; set; }
        public double CallOI { get; set; }
        public bool CallOIVisible { get; set; }    
    }

    //public class OptionHistory
    //{
    //    public
    //}

    public class OptionHistoryGraphSeries
    {
        public DateTime Date { get; set; }
        public OptionValueTypeEnum Type { get; set; }
        public double Pain { get; set; }
        public List<Tuple<double, double>> Deviations { get; set; }
    }  
   
}
