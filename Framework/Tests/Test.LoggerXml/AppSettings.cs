using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CsTool.Logger;
using CsTool.Logger.Model;

namespace Test.LoggerXml
{
    [ModelSettingsClass]
    public class AppSettings : SampleModelSettings
    {
        internal string version = "1.0.0";

        public AppSettings()
        {
            Logger.Instance.LoadAppDefaults(this, null, version);
        }

        public override string ToString()
        {
            return Logger.Instance.AppDefaultsDocument.ToString();
        }

        public void Save()
        {
            Logger.Instance.AppDefaultsDocument.SaveDocument(this, null, version);
        }
    }
}
