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
    /// <Settings version="2.0.0" lastsaved="..." xmlns="">
    ///   <AppDefaults>
    ///     <CsTool.Logger>
    ///       <LogBase name="" version="1.0.0" lastsaved="28/09/2024 4:12:56 PM +10:00">
    ///         <DefaultLogDirectory>log path</DefaultLogDirectory>
    ///         <LogThresholdMaxLevel>LogInfo</LogThresholdMaxLevel>
    ///         <IsShowMessagesEnabledByDefault > false </IsShowMessagesEnabledByDefault>
    ///         <CountOldFilesToKeep> 20 </CountOldFilesToKeep>
    ///         <CountLoggedMessagesMaximum> 100000 </CountLoggedMessagesMaximum>
    ///       </LogBase>
    ///     </CsTool.Logger>
    ///   </ AppDefaults >
    /// </ Settings >
    /// </code>
    /// </remarks>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region App Defaults
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
        #endregion App Defaults

        /// <summary>
        /// Add the logging configuration properties to your application defaults file.
        /// i.e. Create an XElement <CsTool.Logger></CsTool.Logger> with the default logging settings.
        /// </summary>
        /// <returns>The created XElement</returns>
        public XElement AddLoggingElements(object classInstance, string version = "1.0.0")
        {
            if (classInstance == null) classInstance = this;
            XElement xList = XmlSettingsParsing.AddClass(null, classInstance, version);
            if (xList != null && xList.Descendants().Count() > 0) return xList;
            return null;
        }

        /// <summary>
        /// Load settings for a class instance from your application defaults file. The application folder is checked first
        /// then the startup folder for localised settings. This method is normally incorporated into a settings class for a DLL
        /// and application. Use LoadSettingsFile() for custom settings in a separate or same file.
        /// </summary>
        /// <param name="classInstance">The Settings object that contains all settings to be saved for this Class.
        /// If null, this class instance will be used.</param>
        /// <param name="version">The version expected for this section. The section should match or be newer to this version.
        /// The use of version has changed and needs re-implementing from the old CoreUtilities code library.</param>
        /// <param name="fileName">The name of the file to load. If null, the default application file name is used. Does not include the path.</param>
        /// <param name="createIfMissing">If true, the file will be created if it does not exist</param>
        /// <param name="updateIfNeeded">If true, the file will be updated if the version is older than the current version
        /// or properties failed to load correctly</param>
        /// <returns>True if the file was loaded successfully. False if the file requires upgrading</returns>
        /// Previously called LoadAppDefaults()
        public bool LoadAppDefaults(object classInstance, string version = "1.0.0", string fileName = null,
            bool createIfMissing = true, bool updateIfNeeded = true)
        {
            if (classInstance == null) classInstance = this;

            if (fileName == null)
                fileName = GetAppDefaultsFileName();
            //
            // Load settings from the application folder
            //
            string sourcePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string sourceFile = sourcePath + "\\" + fileName;

            bool result = LoadSettingsFile(classInstance, null, version, sourceFile, createIfMissing, updateIfNeeded);

            //
            // Load settings from the startup folder
            //
            // set sourcePath to the current directory
            sourceFile = LogUtilities.MyStartupPath + "\\" + fileName;
            result = LoadSettingsFile(classInstance, null, version, sourceFile, createIfMissing, updateIfNeeded);

            return result;
        }

        /// <summary>
        /// Load the settings for the class instance from the specified file. The file may contains settings from
        /// multiple modules (normally DLLs). The section read and updated is determined by the classInstance passed
        /// to this method.
        /// </summary>
        /// <param name="classInstance">The Settings object that contains all settings to be saved for this Class.</param>
        /// <param name="sectionName">Normally null. Override the section name here, however individually named sections will not
        /// be automatically loaded when calling LoadAppDefaults*() methods. Use this parameter only when manually loading custom settings
        /// </param>
        /// <param name="version">The version expected for this section. The section should match or be newer to this version.</param>
        /// <param name="fullFileName">The full filename and path to the settings file</param>
        /// <param name="createIfMissing">If true, the file will be created if it does not exist</param>
        /// <param name="updateIfNeeded">If true, the file will be updated if the version is older than the current version
        /// or properties failed to load correctly</param>
        /// <returns>True if successful</returns>
        public bool LoadSettingsFile(object classInstance, string sectionName, string version, string fullFileName,
            bool createIfMissing = true, bool updateIfNeeded = true)
        {
            if (fullFileName == null)
            {
                Write(LogPriority.ErrorCritical, "LoadSettingsFile: No file name provided, aborting...");
                return false;
            }

            if (classInstance == null)
            {
                Write(LogPriority.ErrorCritical, "LoadSettingsFile: No class instance provided, aborting...");
                return false;
            }
            if (classInstance.GetType().IsClass == false)
            {
                Write(LogPriority.ErrorCritical, "LoadSettingsFile: classInstance is not a class, aborting...");
                return false;
            }

            // XElement NameSpace and Class Name section names
            string nameSpaceName = classInstance.GetType().Namespace;
            string className = string.IsNullOrWhiteSpace(sectionName) ? classInstance.GetType().Name : sectionName;

            // XDocument sections of interest
            XDocument xDocument = null;             // The Settings document
            XElement xDefaultSection = null;        // Always "AppDefaults" to allow future expansion
            XElement xNamespace = null;             // Sub section per DLL or Namespace
            XElement xMyClassSection = null;        // Individual section, normally the class name, but sub sections are named by the property
            bool isSaveNeeded = false;              // Save the file if it requires creation or update, subject to createIfMissing and updateIfNeeded

            // Load the settings file
            try
            {
                if (File.Exists(fullFileName))
                {
                    xDocument = XDocument.Load(fullFileName);
                    xDefaultSection = xDocument.Root.Element(DefaultXmlSectionName);
                    xNamespace = xDefaultSection?.Element(nameSpaceName);
                    xMyClassSection = xNamespace?.Element(className);
                }
            }
            catch
            {
                Write(LogPriority.ErrorCritical, "LoadSettingsFile: Element({0}.{1}): File corrupted or missing: {2}", nameSpaceName, classInstance.GetType().Name, fullFileName);
            }
            //
            // If any portion of the file is missing create it
            //
            try
            {
                if (xDocument == null)
                {
                    if ( !createIfMissing) return false;
                    // New file required. Create the lot
                    Write(LogPriority.Warning, "LoadSettingsFile: File error, recreating: {0}", fullFileName);
                    xDocument = XmlSettingsParsing.CreateXmlDocument(DefaultXmlSectionName, nameSpaceName);
                    xDefaultSection = xDocument.Root.Element(DefaultXmlSectionName);
                    xNamespace = xDefaultSection.Element(nameSpaceName);
                    isSaveNeeded = true;
                }
                // Now check for missing sections
                if (xDefaultSection == null)
                {
                    Write(LogPriority.Warning, "LoadSettingsFile: Missing XElement<{0}>: {1}", DefaultXmlSectionName, fullFileName);
                    xDefaultSection = new XElement(DefaultXmlSectionName);
                    xDocument.Root.Add(xDefaultSection);
                    isSaveNeeded = true;
                }
                if (xNamespace == null)
                {
                    Write(LogPriority.Warning, "LoadSettingsFile: Missing XElement<{0}>: {1}", nameSpaceName, fullFileName);
                    xNamespace = new XElement(nameSpaceName);
                    xDefaultSection.Add(xNamespace);
                    isSaveNeeded = true;
                }
                if (xMyClassSection == null)
                {
                    Write(LogPriority.Warning, "LoadSettingsFile: Missing XElement<{0}>: {1}", classInstance.GetType().Name, fullFileName);
                    xMyClassSection = AddLoggingElements(classInstance, version);
                    xNamespace.Add(xMyClassSection);
                    isSaveNeeded = true;
                }
            }
            catch
            {
                Write(LogPriority.Fatal, "LoadSettingsFile: Could not re create: {0}", fullFileName);
            }
            try
            {
                if ( isSaveNeeded && ( createIfMissing || updateIfNeeded) )
                {
                    xDocument.Save(fullFileName);
                }
            }
            catch (Exception exception)
            {
                // Standard user will not be able to overwrite the corrupted file in the application folder.
                Write(exception, "LoadSettingsFile: File create failed. Path might not be writeable: {0}", fullFileName);
            }

            try
            {
                int errors = XmlSettingsParsing.LoadClassValues(xMyClassSection, classInstance, version);
                if (errors != 0)
                {
                    if (updateIfNeeded)
                    {
                        Write(LogPriority.Warning, "LoadSettingsFile: Element({0}.{1}) Load Errors({2}), overwriting section: {3}", nameSpaceName, classInstance.GetType().Name, errors, fullFileName);
                        xMyClassSection?.Remove();
                        xNamespace.Add(AddLoggingElements(classInstance, version));
                        xDocument.Save(fullFileName);
                    }
                    else
                    {
                        Write(LogPriority.ErrorCritical, "LoadSettingsFile: Element({0}.{1}) Load Errors({2}), aborting: {3}", nameSpaceName, classInstance.GetType().Name, errors, fullFileName);
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                // Only useful for program debugging during development
                Write(exception, "LoadSettingsFile: Element({0}.{1}): File create failed: {2}", nameSpaceName, classInstance.GetType().Name, fullFileName);
                return false;
            }
            return true;
        }
    }
}
