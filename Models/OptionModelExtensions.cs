using System;
using System.Collections.Generic;
using System.Linq;
using OptionCalculator.Entity;

namespace OptionCalculator
{
    public static class OptionModelExtensions
    {
        public static IQueryable<Entity.Option> GetOptionsForContract(this IQueryable<Option> source, string symbol, DateTime contractExpiration)
        {
            return source.Where(d => d.ContractNav.Symbol == symbol && d.ContractNav.Expiration == contractExpiration);
        }

        public static IEnumerable<Models.OptionLocal> ToOptionLocal(this IQueryable<Option> source)
        {
            return from s in source
                   select new Models.OptionLocal
                    {
                        Strike = (double)s.Strike,
                        Type = s.Type,
                    };
        }

        public static Models.OptionLocal ToOptionLocal(this Option s)
        {
            return new Models.OptionLocal
                   {
                       Strike = (double)s.Strike,
                       Type = s.Type,
                   };
        }

        public static IEnumerable<Models.OptionDataLocal> ToOptionDataLocal(this IQueryable<OptionData> source)
        {
            return from s in source
                   select new Models.OptionDataLocal
                   {
                       OI = (double)s.OpenInterest,
                       Timestamp = s.TimeStamp,
                       Volume = (double)s.Vol,
                   };
        }

        public static Models.OptionDataLocal ToOptionDataLocal(this OptionData s)
        {
            return new Models.OptionDataLocal
                   {
                       OI = (double)s.OpenInterest,
                       Timestamp = s.TimeStamp,
                       Volume = (double)s.Vol,
                   };
        }
    }
}