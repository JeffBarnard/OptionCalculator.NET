﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OptionCalculator.Services.JobScheduler.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60000")]
        public int TimeToWaitForShutdown {
            get {
                return ((int)(this["TimeToWaitForShutdown"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ForceDebugger {
            get {
                return ((bool)(this["ForceDebugger"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OptionCalculator.Scheduler")]
        public string ServiceName {
            get {
                return ((string)(this["ServiceName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OptionCalculator Scheduler Service")]
        public string ServiceDisplayName {
            get {
                return ((string)(this["ServiceDisplayName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OptionCalculator Scheduler Service")]
        public string ServiceDescription {
            get {
                return ((string)(this["ServiceDescription"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfSchedule xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Schedule xsi:type=""DailySchedule"" ProviderType=""OptionCalculator.Service.dll, OptionCalculator.YahooService"" Type=""DAILY"" FromTime=""00:00"" ToTime=""23:58"" DaysToRun=""2 3 4 5 6"" Name=""DailySchedule"" Active=""true"" StartTime=""2011-08-09T16:30:00.000000-04:00"" Interval=""0"">
    <InitializationData />
  </Schedule>
</ArrayOfSchedule>")]
        public global::System.Xml.XmlDocument Schedule {
            get {
                return ((global::System.Xml.XmlDocument)(this["Schedule"]));
            }
        }
    }
}
