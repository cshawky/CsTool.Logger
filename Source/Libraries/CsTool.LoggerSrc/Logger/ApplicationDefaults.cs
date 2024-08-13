namespace CsTool.Logger
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using ExtensionMethods;
    using Utilities;


    /// <summary>
    /// Logger settings for the application are stored in the application defaults file.
    /// </summary>
    /// <remarks>
    /// This defaults file is managed by CsTool.CoreUtilities. However, to get early logging to the correct path the
    /// logger attempts to read the application path from the application defaults file.
    /// Class <code>LogBase</code> gets its startup settings from this class. That is those that are
    /// included in the defaults file. Refer to <code>CsTool.CoreUtilities.DefaultSettings</code> class for
    /// more information on the defaults file.
    /// For applications that do not use CsTool.CoreUtilities, the file format required is as follows:
    /// <code>
    /// <?xml version="1.0" encoding="utf-8" standalone="yes"?>
    /// <Settings version = "2.0.0" lastsaved="..." xmlns="">
    ///   <ApplicationDefaults version = "2.0.0" lastsaved="..." xmlns="">
    ///     <Default>
    ///         <DefaultStartupDirectory>blah</DefaultStartupDirectory>
    ///     </Default>
    ///     <CsTool.Logger>
    ///         <DefaultLogDirectory>log path</DefaultLogDirectory>
    ///         <LogThresholdMaxLevel>LogInfo</LogThresholdMaxLevel>
    ///         <IsShowMessagesEnabledByDefault > false </IsShowMessagesEnabledByDefault>
    ///         <CountOldFilesToKeep> 20 </CountOldFilesToKeep>
    ///         <CountLoggedMessagesMaximum> 100000 </CountLoggedMessagesMaximum>
    ///      </CsTool.Logger>
    ///   </ ApplicationDefaults >
    /// </ Settings >
    /// </code>
    /// </remarks>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Application Defaults
        /// <summary>
        /// For CsTool.Logger compatibility, the default application file must be named as follows:
        ///  ApplicationName.ApplicationDefaults.xml
        /// To ensure the file name conforms, use method <code>GetApplicationDefaultsFileName</code> 
        /// </summary>
        public const string DefaultXmlSectionName = "ApplicationDefaults";

        /// <summary>
        /// If you wish to include logging properties in your application defaults file, you can use this method
        /// to define the correct file name. This enables application specific logging settings.
        /// </summary>
        /// <returns>The Filename and extension for the defaults xml file</returns>
        public static string GetApplicationDefaultsFileName()
        {
            return LogUtilities.MyProcessName + "." + DefaultXmlSectionName + ".xml";
        }
        #endregion

        /// <summary>
        /// Add the logging configuration properties to your application defaults file.
        /// Create an XElement <CsTool.Logger></CsTool.Logger> with the default logging settings.
        /// </summary>
        /// <returns>The created XElement</returns>
        public XElement AddLoggingElements()
        {
            XElement xList = XmlSettingsParsing.AddClass("CsTool.Logging", this);
            if (xList != null && xList.Descendants().Count() > 0) return xList;
            return null;
        }

        /// <summary>
        /// Load the logging configuration properties from your application defaults file that should exist in the
        /// application installation folder. This method is automatically called during DLL initialisation.
        /// </summary>
        /// <param name="fileName">The name of the file to load. If null, the default application file name is used.</param>
        private void LoadApplicationDefaults(string fileName = null)
        {
            try
            {
                if ( fileName == null )
                    fileName = GetApplicationDefaultsFileName();
                string sourcePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                string sourceFile = sourcePath + "\\" + fileName;
                // Check if file exists
                if (!File.Exists(sourceFile))
                {
                    Log.Write("Warning: CsTool.Logger: Application defaults file not found: {0}", sourceFile);
                    return;
                }
                XDocument xDocument = XDocument.Load(sourceFile);
                XElement xDefaultSection = xDocument.Root.Element(DefaultXmlSectionName);
                if (xDefaultSection == null)
                {
                    Log.Write("Error: CsTool.Logger: Application defaults section({0}) not found", DefaultXmlSectionName);
                    return;
                }
                XElement xMySection = xDefaultSection.Element("CsTool.Logging");
                if (xMySection == null)
                {
                    xMySection = xDefaultSection;
                    Log.Write("Warning: CsTool.Logger: Application defaults section(CsTool.Logging) not found");
                    // Continue and support the old format where logging settings were part of the main app section...
                }

                XmlSettingsParsing.LoadClassValues(xMySection, this);
            }
            catch (Exception exception)
            {
                // Only useful for programme debugging during development
                Log.Write(exception, "Error: CsTool.Logger: Failed to load application defaults from settings file.");
            }
        }
    }
}
