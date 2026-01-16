namespace CsTool.Logger.Utilities
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

    /// <summary>
    /// Wrapper class that represents the Application's configuration file %APPNAME%.AppDefaults.xml
    /// </summary>
    /// <remarks>
    /// The AppDefaults file contains multiple sections for storing application and model specific configuration.
    /// This interfaces maps properties from a class instance to a section in the file. Thus a single file may
    /// be used across multiple DLLs, models independently.
    /// An instancce of <code>XmlSettingsDocument</code> represents the properties from a single class mapped
    /// to a single section within the AppDefaults.xml file.
    /// Typical file structure is shown below. This sample includes two sections, one for the Logger settings
    /// and one for a hypothetical Model settings.
    ///
    /// An instance of XmlSettingsDocument is created for each section required. e.g. LogBase or Library1Settings.
    ///
    /// Provided that the file is a valid XDocument, the relevant section may be read and written independent to
    /// other sections. When reading a section, all properties found are mapped to the class instance provided.
    /// Each property is identified by an attribute including [ModelSettingsProperty].
    ///
    /// Refer to class <code>ModelSettingsAttribute</code> for a list of supported attributes and their behaviour.
    ///
    /// Refer to class <code>SampleModelSettings.cs</code> for a working example.
    ///
    /// With reference to the sample XML file below the properties are:
    ///     LogBase.DefaultXmlSectionName = "AppDefaults"
    ///     XDocument = The loaded file as an XDocument. The contents of a loaded file is shown below containing two sections.
    ///                 The document structure is defined in XmlSettingsParsing.CreateXmlDocument.
    ///                 The Root Element of XDocument is <Settings>
    ///                 XDocument.Root(Settings) contains an element <AppDefaults> contains all application default settings.
    ///                 This allows insertion of multiple parent sections other than AppDefaults if required in the future.
    ///     XDefaultSection = XElement<AppDefaults> named from LogBase.DefaultXmlSectionName property.
    ///     XNamespace = XElement<CsTool.Logger> named from NameSpaceName property.
    ///                 The Namespace is derived from the Namespace of the ClassInstance provided.
    ///                 The Namespace groups all settings elements with a particular DLL or application name.
    ///                 Thus multiple DLLs may store their settings in the same file without conflict.
    ///                 Within each XNamespace there may be multiple sections representing different class instances.
    ///                 In this release, it is assumed that there is only one section per class as no code is used (it exists)
    ///                 to override and provide a custom ClassName for each instance of the same class. Thus the section name
    ///                 should be unique. 
    ///     XClassSection = XElement<LogBase> named from SectionName property.
    ///                 This is the class instance properties mapped to XML elements.
    ///                 
    /// For an instance of AppDefaultsDocument the instance manages a single section which has the following properties:
    ///     ClassInstance = Instance of LogBase, in particular the singleton Logger.Instance
    ///     ClassName = class="LogBase"
    ///     SectionName = "LogBase" being the name of the class instance provided, or an override name if provided.
    ///                 Note use of sectionName parameter during creation has not been re tested since this module was reworked.
    ///                 Overriding section name is utilised at a lower level when dealing with Lists of class instances.
    ///     NameSpaceName = "CsTool.Logger"
    ///   
    /// 
    /// <code>
    /// <?xml version="1.0" encoding="utf-8" standalone="yes"?>
    /// <Settings version="2.0.0" lastsaved="23/04/2025 2:12:21 PM +10:00">
    ///   <AppDefaults>
    ///     <CsTool.Logger>
    ///       <LogBase class="LogBase" version="1.0.0" lastsaved="14/12/2025 1:55:39 PM +11:00">
    ///         <CountLoggedMessagesMaximum>100000</CountLoggedMessagesMaximum>
    ///         ...
    ///         <LogThresholdMaxLevel-Help usage="LogFatal LogImportantInfo LogCritical LogError LogWarning LogInfo LogDebug LogVerbose" />
    ///         <LogThresholdMaxLevel>LogInfo</LogThresholdMaxLevel>
    ///         # Array support implemented via List<T> In this example T=string.
    ///         <SubstitutionsSupportedT count="19" type="List`1" elementType="System.String">
    ///           <SubstitutionsSupported>%TEMP%</SubstitutionsSupported>
    ///           <SubstitutionsSupported>%TMP%</SubstitutionsSupported>
    ///           ...
    ///         </SubstitutionsSupportedT>
    ///       </LogBase>
    ///     </CsTool.Logger>
    ///     <MyLibs.Library1>
    ///       <Library1Settings class="Library1Settings" version="1.0.0" lastsaved="29/08/2025 1:46:57 PM +10:00">
    ///         <Name>A set of parameters to tune my DLL</Name>
    ///         <Description>Parameters for tuning the Library1 toolset.</Description>
    ///         <ConfigPath>%LOCALAPPDATA%\%APPNAME%\bin\Debug\Resources</ConfigPath>
    ///         <IsAutoLoad>True</IsAutoLoad>
    ///         <SiteIdT count="5" type="List`1" elementType="System.Int32">
    ///           <SiteId>1</SiteId>
    ///           ...
    ///           <SiteId>5</SiteId>
    ///         </SiteIdT>
    ///       </Library1Settings>
    ///       # The following section has a custom SectionName making it unique within the Library1 namespace.
    ///       <Library1Settings2 class="Library1Settings" version="1.0.0" lastsaved="29/08/2025 1:46:57 PM +10:00">
    ///         <Name>A set of parameters to tune my DLL</Name>
    ///         <Description>Parameters for tuning the Library1 toolset.</Description>
    ///         ...
    ///       </Library1Settings2>
    ///     </MyLibs.Library1>
    ///   </AppDefaults>
    /// </Settings>
    /// </code>
    /// </remarks>
    public class AppDefaultsDocument
    {
        //
        // -----------------------------------------------------------------------------------------
        //

        #region Initialisation

        /// <summary>
        /// The constructor creates an instance of XmlSettingsDocument that maps the properties of the provided classInstance
        /// to that instance. A section is created in the AppDefaults.xml file for that class instance if it does not exist or
        /// has missing properties. Existing property values are maintained. When saving the values of the classInstance are
        /// saved to file.
        /// </summary>
        /// <param name="classInstance">The Settings object that contains all settings to be saved for this Class.</param>
        /// <param name="sectionName">Normally null. Override the section name here, however individually named sections will not
        /// be automatically loaded when calling LoadAppDefaults*() methods. Use this parameter only when manually loading custom settings
        /// </param>
        /// <param name="version">The version expected for this section. The section should match or be newer to this version.</param>
        /// <param name="fullFileName">The full filename and path to the settings file</param>
        /// <param name="createIfMissing">If true, the file will be created if it does not exist. Default True</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public AppDefaultsDocument(object classInstance, string sectionName, string version, string fullFileName, bool createIfMissing = true)
        {
            bool argumentException = false;
            try
            {
                string methodName = nameof(AppDefaultsDocument);
                if (fullFileName == null)
                {
                    Logger.SafeWrite(LogPriority.ErrorCritical, "{0}: No file name provided, aborting...", methodName);
                    argumentException = true;
                }
                if (classInstance == null)
                {
                    Logger.SafeWrite(LogPriority.ErrorCritical, "{0}: No class instance provided, aborting...", methodName);
                    argumentException = true;
                }
                if (classInstance.GetType().IsClass == false)
                {
                    Logger.SafeWrite(LogPriority.ErrorCritical, "{0}: classInstance is not a class, aborting...", methodName);
                    argumentException = true;
                }
                if (argumentException)
                {
                    IsLoaded = false;
                    return;
                }

                this.FullFileName = fullFileName;
                FilePath = Path.GetDirectoryName(fullFileName);
                CreateIfMissing = createIfMissing;
                //ClassInstance = classInstance;
                string className = classInstance.GetType().Name;
                string nameSpaceName = classInstance?.GetType().Namespace;
                string instanceSectionName = sectionName ?? className;
                version = version ?? "1.0.0";
#if DEBUGLOGGER2
                Logger.SafeWrite(LogPriority.Verbose, "{0}: Loading application settings <{1}.{2}> from {3}",
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
#endif // DEBUGLOGGER2

                IsLoaded = LoadDocument(classInstance, instanceSectionName, version);

                if (IsLoaded)
                {
                    int valuesUpdated = UpdateClassInstance(classInstance, instanceSectionName, version);
                    // TODO valuesUpdated has no purpose other than debugging right now.
                    IsSaveNeeded = false;
#if DEBUGLOGGER2
                    Logger.SafeWrite(LogPriority.Verbose, "{0}: Loaded XElement<{1}.{2}> from {3}, {4} values updated.",
                        methodName, nameSpaceName, instanceSectionName, FullFileName, valuesUpdated);
#endif // DEBUGLOGGER2
                    if (IsUpgradeRequired)
                    {
                        Logger.SafeWrite(LogPriority.Verbose, "{0}: Upgrade required for settings <{1}.{2}> from {3}",
                            methodName, nameSpaceName, instanceSectionName, FullFileName);
                    }
                    if (IsSaveNeeded)
                    {
                        Logger.SafeWrite(LogPriority.Verbose, "{0}: Save is required for settings <{1}.{2}> from {3}",
                            methodName, nameSpaceName, instanceSectionName, FullFileName);
                    }
                    if (IsUpgradeRequired && CreateIfMissing || IsSaveNeeded)
                    {
                        SaveDocument(classInstance, instanceSectionName, version);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.SafeWrite(exception, "AppDefaultsDocument: Constructor failed");
                argumentException = true;
            }
            finally
            {
                if (argumentException)
                {
                    IsLoaded = false;
                }
            }
        }

        #endregion Initialisation

        //
        // -----------------------------------------------------------------------------------------
        //

        #region Properties

        public string FullFileName { get; private set; } = null;

        public string FilePath { get; private set; } = null;

        /// <summary>
        /// True when property value has been changed since the last save. 
        /// TODO At present this is only detected during load and validation of the XDocument. 
        /// A separate interface is needed to handle updates at runtime e.g. through the user interface. 
        /// In previous releases the property was held within the settings class and updated by each
        /// property setter. This approach has been deprecated but not yet replaced.
        /// </summary>
        public bool IsSaveNeeded { get; set; } = false;

        /// <summary>
        /// True if there is a difference between the class and the XML Document XElement structure 
        /// indicating that an upgrade is required.
        /// </summary>
        private bool IsUpgradeRequired { get; set; } = false;

        /// <summary>
        /// True if the XML Document was successfully loaded from file and the format was valid.
        /// </summary>
        public bool IsLoaded { get; set; } = false;

        /// <summary>
        /// Indicates whether the current user can save the settings file. Normally the AppDefaults file
        /// in the executable path is read only for standard users.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// The AppDefault settings file will be created if it does not exist. Set this to false if you do not
        /// wish to have the file created automatically. Normally this feature is used to have both an settings file
        /// in the programme folder and a user specific settings file in the user's working folder.
        /// </summary>
        public bool CreateIfMissing { get; private set; }

        /// <summary>
        /// The loaded XDocument representing the AppDefaults.xml file.
        /// </summary>
        internal XDocument XDocument { get; private set; } = null;

        /// <summary>
        /// The XElement (section) representing LogBase.DefaultXmlSectionName 
        /// e.g. The Element <AppDefaults>
        /// </summary>
        internal XElement XDefaultSection { get; private set; } = null;

        #endregion Properties

        //
        // -----------------------------------------------------------------------------------------
        //

        #region Methods

        /// <summary>
        /// Load the XML Document from file. The document is loaded for the specific class instance provided
        /// using the optional section name for that instance. By default the section name is the class name.
        /// </summary>
        /// <param name="classInstance">The class instance to load the section for</param>
        /// <param name="sectionName">Optional section name overrides the class name for the XElement</param>
        /// <param name="version">The version expected for this section. The section should match or be newer to this version.</param>
        /// <returns>True if successfully loaded</returns>
        public bool LoadDocument(object classInstance, string sectionName = null, string version = "1.0.0")
        {
            string methodName = nameof(LoadDocument);
            string className = classInstance.GetType().Name;
            string nameSpaceName = classInstance?.GetType().Namespace;
            string instanceSectionName = sectionName ?? className;
            try
            {
                if (File.Exists(FullFileName))
                {
                    XDocument = XDocument.Load(FullFileName);
                    IsReadOnly = !MyUtilities.IsPathWriteable(Path.GetDirectoryName(FullFileName));
                    string readOnlyText = IsReadOnly ? " Read Only" : String.Empty;
#if DEBUG
                    Logger.SafeWrite(LogPriority.Verbose, "{0}: Element({1}.{2}) File Loaded{3}: {4}",
                        methodName, nameSpaceName, instanceSectionName, readOnlyText, FullFileName);
#endif //DEBUG
                }
            }
            catch
            {
                Logger.SafeWrite(LogPriority.Warning, "{0}: Element({1}.{2}): File corrupted or missing: {3}", 
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
            }

            //
            // Read the sections from the XDocument. ValidateXDocument will then check and create any missing elements.
            //
            XDefaultSection = XDocument?.Root.Element(LogBase.DefaultXmlSectionName);

            XElement xNamespace = XDefaultSection?.Element(nameSpaceName);
            XElement xClassSection = xNamespace?.Element(instanceSectionName);

            //
            // Now validate the document structure, creating any missing elements if required.
            //
            bool result = ValidateXDocument(classInstance, instanceSectionName);
            if (result)
            {
                Logger.SafeWrite(LogPriority.Verbose, "{0}: Element({1}.{2}) Successfully validated file: {3}",
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
            }
            else
            {
                Logger.SafeWrite(LogPriority.Warning, "{0}: Element({1}.{2}) Validation failed for file: {3}",
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
            }
            if (IsSaveNeeded)
            {
                Logger.SafeWrite(LogPriority.Warning, "{0}: Element({1}.{2}) XDocument requires saving: {3}",
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
                SaveDocument(classInstance, instanceSectionName, version);
            }
            return result;
        }

        /// <summary>
        /// Save the Class Instance data into the XML Document, create it if necessary.
        /// </summary>
        /// <returns>True if successful</returns>
        public bool SaveDocument(object classInstance, string sectionName = null, string version = "1.0.0")
        {
            string methodName = nameof(SaveDocument);

            if (IsReadOnly)
            {
                Logger.SafeWrite(LogPriority.Verbose, "{0}: Save cancelled. File is read only: {1}", methodName, FullFileName);
                return false;
            }
            //
            // If any portion of the file is missing create it, then save the class instance data to the file
            //
            string className = classInstance.GetType().Name;
            string nameSpaceName = classInstance?.GetType().Namespace;
            string instanceSectionName = sectionName ?? className;
            try
            {
                XElement xNamespace = XDefaultSection?.Element(nameSpaceName);
                XElement xClassSection = xNamespace?.Element(instanceSectionName);
                if (ValidateXDocument(classInstance, instanceSectionName))
                {
                    // Update the XDocument with the latest values from the class instance.
                    xClassSection?.Remove();
                    xClassSection = AddElementsFromClassInstance(classInstance, sectionName, version);
                    xNamespace.Add(xClassSection);
                }
            }
            catch (Exception exception)
            {
                Logger.SafeWrite(exception, "{0}: Save aborted. Could not re construct XElement<{1}> in {2}", 
                    methodName, sectionName, FullFileName);
                return false;
            }

            // Save the document
            try
            {
                XDocument.Save(FullFileName);
                IsSaveNeeded = false;
                IsUpgradeRequired = false;
                Logger.SafeWrite(LogPriority.Verbose, "{0}: Element({1}.{2}) XDocument saved successfully: {3}",
                    methodName, nameSpaceName, instanceSectionName, FullFileName);
            }
            catch (Exception exception)
            {
                string path = Path.GetDirectoryName(FullFileName);
                if (MyUtilities.IsPathWriteable(path))
                {
                    Logger.SafeWrite(exception, "{0}: File create failed: {1}", methodName, FullFileName);
                }
                else
                {
                    // Standard user will not be able to overwrite the corrupted file in the application folder.
                    Logger.SafeWrite(LogPriority.ErrorCritical, "{0}: File create failed. Path is not writeable: {1}", methodName, FullFileName);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Having loaded the XML Document, update the class instance provided with the values found in the file.
        /// </summary>
        /// <returns>The number of property values that were updated from file</returns>
        private int UpdateClassInstance(object classInstance, string sectionName, string version)
        {
            string methodName = nameof(UpdateClassInstance);
            int countChanges = 0;
            string className = classInstance.GetType().Name;
            string nameSpaceName = classInstance?.GetType().Namespace;
            string instanceSectionName = sectionName ?? className;
            try
            {
                XElement xNamespace = XDefaultSection?.Element(nameSpaceName);
                XElement xClassSection = xNamespace?.Element(instanceSectionName);

                // load the required section
                countChanges = XmlSettingsParsing.UpdateClassValues(xClassSection, classInstance, version);
            }
            catch (Exception exception)
            {
                Logger.SafeWrite(exception, "{0}: Error processing XElement<{1}> after {2} value changes: {3}",
                    methodName, instanceSectionName, countChanges, FullFileName);
            }
            return countChanges;
        }

        /// <summary>
        /// Add the configuration properties from the class instance provided to the application defaults file.
        /// For example, in the case of the Logger settings the method creates an XElement <CsTool.Logger></CsTool.Logger>
        /// with the default logging settings from <code>LogBaseProperties</code>.
        /// </summary>
        /// <param name="classInstance">An instance of the ModelSettingsClass</param>
        /// <param name="version">Optional version of the class instance</param>
        /// <returns>The created XElement</returns>
        private static XElement AddElementsFromClassInstance(object classInstance, string sectionName, string version = "1.0.0")
        {
            if (classInstance == null)
            {
                throw new ArgumentNullException("AddElementsFromClassInstance: classInstance is null");
            }
            XElement xList = XmlSettingsParsing.AddClass(classInstance, sectionName, version);
            if (xList != null && xList.Descendants().Count() > 0)
            {
                return xList;
            }
            return null;
        }

        /// <summary>
        /// Validate the XDocument file structure, creating any missing elements if required. The properties are not transferred to
        /// the class instance here. This method is also used to construct a new file if it did not exist.
        /// </summary>
        /// <param name="createIfMissing">The XDocument will be created if the file did not exist. It is not automatically saved to file.</param>
        /// <returns>true if the document was successfully loaded</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// The order of validation and create is constructed carefully to allow an existing document to be loaded without unnecessary
        /// modification, and the relevant section updated or replaced.
        /// </remarks>
        private bool ValidateXDocument(object classInstance, string sectionName = null, string version = "1.0.0")
        {
            string methodName = nameof(ValidateXDocument);
            int countErrors = 0;
            string className = classInstance.GetType().Name;
            string nameSpaceName = classInstance?.GetType().Namespace;
            string instanceSectionName = sectionName ?? className;
            XElement xNamespace = XDefaultSection?.Element(nameSpaceName);
            XElement xClassSection = xNamespace?.Element(instanceSectionName);
            try
            {
                if (XDocument == null)
                {
                    if (!CreateIfMissing) return false;
                    // New file required. Create the lot
                    Logger.SafeWrite(LogPriority.Warning, "{0}: File error, recreating XDocument: {1}", methodName, FullFileName);
                    //
                    // Create new XDocument structure using the default template from XmlSettingsParsing
                    // Creates the Root <Settings> element, with the <AppDefaults> child element.
                    // Creates the Namespace element below that.
                    //
                    XDocument = XmlSettingsParsing.CreateXmlDocument(LogBase.DefaultXmlSectionName, nameSpaceName);
                    //
                    // Select the namespace section so we can add the class section below
                    //
                    XDefaultSection = XDocument.Root.Element(LogBase.DefaultXmlSectionName);
                    xNamespace = XDefaultSection.Element(nameSpaceName);
                    IsUpgradeRequired = true;
                }
                // Check XDefaultSection, normally a file corruption situation (accidental deletion of portions)
                if (XDefaultSection == null)
                {
                    Logger.SafeWrite(LogPriority.Warning, "{0}: Missing DefaultSection XElement<{1}>: {2}", 
                        methodName, LogBase.DefaultXmlSectionName, FullFileName);
                    XDefaultSection = new XElement(LogBase.DefaultXmlSectionName);
                    XDocument.Root.Add(XDefaultSection);
                    xNamespace = new XElement(nameSpaceName);
                    XDefaultSection.Add(xNamespace);
                    IsUpgradeRequired = true;
                }
                // Check XNamespace, normally a file corruption situation (accidental deletion of portions)
                if (xNamespace == null)
                {
                    Logger.SafeWrite(LogPriority.Warning, "{0}: Missing Namespace XElement<{1}>: {2}", 
                        methodName, nameSpaceName, FullFileName);
                    xNamespace = new XElement(nameSpaceName);
                    XDefaultSection.Add(xNamespace);
                    IsUpgradeRequired = true;
                }
                // Check XClassSection, may be missing if first time loading this class instance
                if (xClassSection == null)
                {
                    Logger.SafeWrite(LogPriority.Warning, "{0}: Missing settings XElement<{1}>: {2}",
                        methodName, instanceSectionName, FullFileName);
                    xClassSection = AddElementsFromClassInstance(classInstance, version);
                    xNamespace.Add(xClassSection);
                    IsUpgradeRequired = true;
                }
                countErrors = XmlSettingsParsing.ValidateClassProperties(xClassSection, classInstance, version);
                if (countErrors != 0)
                {
                    Logger.SafeWrite(LogPriority.Warning, "{0}: Element<{1}.{2}> There were {3} property errors: {4}",
                        methodName, nameSpaceName, instanceSectionName, countErrors, FullFileName);
                    IsUpgradeRequired = true;
                }
            }
            catch (Exception exception)
            {
                Logger.SafeWrite(LogPriority.Fatal, exception, "{0}: Element<{1}.{2}> Could not re construct: {3}", methodName, nameSpaceName, instanceSectionName, FullFileName);
            }
            finally
            {
                if (XDocument == null || XDefaultSection == null || xNamespace == null)
                {
                    throw new InvalidOperationException($"{methodName}: Critical Error: The XDocument could not be constructed: {FullFileName}");
                }
            }
            return true;
        }

        /// <summary>
        /// Return the entire XML Document as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return XDocument?.ToString() ?? String.Empty;
        }

        #endregion Methods
    }
}