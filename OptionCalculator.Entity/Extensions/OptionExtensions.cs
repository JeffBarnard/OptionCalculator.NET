//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//public static class OptionModelExtensions
//{
//    public static IEnumerable<Option> ToOptionLocal(this.IEnumerable<Option> source)
//    {
//        return from s in source
//               select new PINPoint.OIM.Advanced.WebService.Models.Certification
//                {
//                    CertificationID = s.CertificationId,
//                    Description = s.Description,
//                    Name = s.Name,
//                    CertificationItemList = s.CertificationItems.ToCertificationItemLocal().ToList(),
//                    CertificationItemResponseList = s.CertificationResponseGroup == null ? null : s.CertificationResponseGroup.CertificationResponses.ToCertificationResponseLocal().ToList(),
//                    CertificationTypeName = s.CertificationType.Name                       
//                };
//    }
//}
