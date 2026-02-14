using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsTool.Logger;
using System.Threading;

namespace Test.LoggerXml
{
    internal class Program
    {
        /// <summary>   
        /// Development testing of the new ModelSettings feature making use of Attributes to automatically add properties
        /// to the settings file.
        /// </summary>
        public static AppSettings Settings { get; set; } = new AppSettings();

        static void Main(string[] args)
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string[] dnsName = Dns.GetHostName().Split('.');
            string hostName = dnsName[0];
            var procInfo = Process.GetCurrentProcess();
            string versionInfo = procInfo.ProcessName + "(" + procInfo.Id + ")"
                + " Version(" + Assembly.GetEntryAssembly().GetName().Version.ToString()
                + ", CsTool.Logger " + Assembly.GetAssembly(typeof(Logger)).GetName().Version.ToString() + ")";

            string message = String.Format("Version Info: {0}, Computer({1}), User({2}), Domain({3})",
            versionInfo, hostName, Environment.UserName, domainName);
            Logger.Write(message);
            Console.WriteLine(message);
            Logger.Write("{0}", Settings.Help);
            Console.WriteLine("{0}", Settings.Help);
            Thread.Sleep(1000);
            string settingsFile = Settings.ToString();
            Logger.WriteRaw("{0}", settingsFile);
            Console.WriteLine("\n{0}\n", settingsFile);
            Console.WriteLine("Bye!");
            Thread.Sleep(1000);
        }
    }
}
