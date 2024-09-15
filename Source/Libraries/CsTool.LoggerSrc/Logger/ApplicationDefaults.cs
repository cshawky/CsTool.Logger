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
    using System.Runtime.CompilerServices;
    using System.Xml;
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
    ///   <AppDefaults version = "2.0.0" lastsaved="..." xmlns="">
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
    ///   </ AppDefaults >
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
        ///  ApplicationName.AppDefaults.xml
        /// To ensure the file name conforms, use method <code>GetAppDefaultsFileName</code> 
        /// </summary>
        public const string DefaultXmlSectionName = "AppDefaults";

        /// <summary>
        /// If you wish to include logging properties in your application defaults file, you can use this method
        /// to define the correct file name. This enables application specific logging settings.
        /// </summary>
        /// <returns>The Filename and extension for the defaults xml file</returns>
        public static string GetAppDefaultsFileName()
        {
            return LogUtilities.MyProcessName + "." + DefaultXmlSectionName + ".xml";
        }
        #endregion

        /// <summary>
        /// Add the logging configuration properties to your application defaults file.
        /// i.e. Create an XElement <CsTool.Logger></CsTool.Logger> with the default logging settings.
        /// </summary>
        /// <returns>The created XElement</returns>
        public XElement AddLoggingElements(object classInstance, string version = "1.0.0")
        {
            if (classInstance == null) classInstance = this;
            string namespaceName = classInstance.GetType().Namespace;
            XElement xList = XmlSettingsParsing.AddClass(namespaceName, classInstance, version);
            if (xList != null && xList.Descendants().Count() > 0) return xList;
            return null;
        }


        /// <summary>
        /// Load the logging configuration properties from your application defaults file that should exist in the
        /// application installation folder. This method is automatically called during DLL initialisation.
        /// </summary>
        /// <param name="fileName">The name of the file to load. If null, the default application file name is used.</param>
        /// <returns>True if the file was loaded successfully. False if the file requires upgrading</returns>
        public bool LoadAppDefaults(object classInstance, string version = "1.0.0", string fileName = null)
        {
            XDocument xDocument = null;
            XElement xDefaultSection = null;
            try
            {
                if (classInstance == null) classInstance = this;
                string namespaceName = classInstance.GetType().Namespace;

                if ( fileName == null )
                    fileName = GetAppDefaultsFileName();
                string sourcePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                string sourceFile = sourcePath + "\\" + fileName;
                // Check if file exists
                if (!File.Exists(sourceFile))
                {
                    Log.Write("Warning: {0}: Application defaults file not found: {1}", namespaceName, sourceFile);
                    xDocument = XmlSettingsParsing.CreateXmlDocument(DefaultXmlSectionName);
                    xDocument.Save(sourceFile);
                    Log.Write("Warning: {0}: Application defaults file created successfully: {1}", namespaceName, sourceFile);
                }
                // Load the settings file
                try
                {
                    xDocument = XDocument.Load(sourceFile);
                    xDefaultSection = xDocument.Root.Element(DefaultXmlSectionName);
                }
                catch (Exception exception)
                {
                    Log.Write(exception, "Error: {0}: Applications defaults file is corrupted, overwriting", namespaceName);
                }

                if (xDefaultSection == null)
                {
                    Log.Write("Error: {0}: Application defaults Section({1}) not found, invalid format", namespaceName, DefaultXmlSectionName);
                    xDocument = XmlSettingsParsing.CreateXmlDocument(DefaultXmlSectionName);
                    xDocument.Save(sourceFile);
                    xDefaultSection = xDocument.Root.Element(DefaultXmlSectionName);
                    Log.Write("Warning: {0}: Application defaults file created successfully: {1}", namespaceName, sourceFile);
                }
                XElement xMySection = xDefaultSection.Element(namespaceName);
                if (xMySection == null)
                {
                    Log.Write("Warning: {0}: Application defaults Section({1}}) not found", namespaceName, classInstance.GetType().Name);
                    //
                    // Upgrade the file, by adding the missing section
                    //
                    xDefaultSection.Add(AddLoggingElements(classInstance, version));
                    xMySection = xDefaultSection.Element(namespaceName);
                    xDocument.Save(sourceFile);
                }
                int errors = XmlSettingsParsing.LoadClassValues(xMySection, classInstance, version);
                if (errors != 0)
                {
                    Log.Write("Warning: {0}: Application defaults Section({1}) Errors({2}), overwriting section", namespaceName, classInstance.GetType().Name, errors);
                    xMySection.Remove();
                    xDefaultSection.Add(AddLoggingElements(classInstance, version));
                    xDocument.Save(sourceFile);
                }
            }
            catch (Exception exception)
            {
                // Only useful for program debugging during development
                Log.Write(exception, "Error: CsTool.Logger: Failed to load/save application defaults from settings file.");
                return false;
            }
            return true;
        }

    }
}
