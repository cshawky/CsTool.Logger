using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsTool.Logger;
using CsTool.Logger.Model;

namespace Test.LoggerXml
{
    [ModelSettingsClass]
    public class AppSettings : SampleModelSettings
    {
        public AppSettings() 
        {
            Logger.Instance.LoadAppDefaults(this);
        }

    }
}
