namespace CsTool.Logger.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Mime;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using ExtensionMethods;
    using Model;

    public static class XmlSettingsParsing
    {
        /// <summary>
        /// Version reference for the application properties file. Format "X.Y.Z".
        /// </summary>
        /// <remarks>
        /// When the format is like "X.Y.Z Beta", the file will be read then overwritten each time the programme is run.
        /// This is a helpful mechanism during software development to ensure the file is always using the latest format.
        /// Versions where X.Y are the same are not automatically upgraded.
        /// </remarks>
        public readonly static string XmlSettingsDocumentVersion = "2.0.0";

        /// <summary>
        /// Create a blank XML document with the root element "Settings" and the version attribute set. The first Element group "Default" is added.
        /// Only use this method when creating a new settings file.
        /// Use MergeXmlDocument to merge the new settings with the existing settings.
        /// </summary>
        /// <param name="defaultSectionName">The name of the first section to add to the settings file.
        /// For the application defaults file this would be DefaultXmlSectionName = "AppDefaults".</param>
        /// <returns>The empty XmlDocument, ready to add settings groups to</returns>
        public static XDocument CreateXmlDocument(string defaultSectionName, string nameSpace)
        {
            XDocument xDocument = null;
            XElement xRoot = null;
            XElement xDefaultSection = null;
            XElement xNamespace = null;
            try
            {
                xDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
                xRoot = new XElement("Settings"
                    , new XAttribute("version", XmlSettingsDocumentVersion)
                    , new XAttribute("lastsaved", DateTimeOffset.Now.ToString()));
                xRoot.Add(new XElement(defaultSectionName));
                xDocument.Add(xRoot);
                xDefaultSection = xDocument.Root.Element(defaultSectionName);
                if (!string.IsNullOrWhiteSpace(nameSpace))
                {
                    xNamespace = new XElement(nameSpace);
                    xDefaultSection.Add(xNamespace);
                }
            }
            catch (Exception exception)
            {
                // Only useful for program debugging during development
                Log.Write(exception, "Error: CsTool.Logger: Failed to create application defaults from settings file.");
                return null;
            }
            return xDocument;
        }
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Xml File Write Interface

        /// <summary>
        /// Adds the propertyInfo to the XElement.
        /// If the propertyInfo is an enumeration a Helper element is added to describe the enumerations.
        /// If a string array, then multiple sub elements are added to the XElement.
        /// 
        /// </summary>
        /// <param name="xmlTree">An existing xml section.</param>
        /// <param name="propertyName">The base name of the propertyValue.</param>
        /// <param name="propertyValue">The enumeration value to write</param>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// Example results
        /// <code>
        ///     <CountOldLogFilesToKeep>20</CountOldLogFilesToKeep>
        ///     <LogThresholdMaxLevel-Help usage="LogFatal LogImportantInfo LogCritical LogError LogWarning LogInfo LogDebug LogVerbose" />
        ///     <LogThresholdMaxLevel>LogInfo</LogThresholdMaxLevel>
        ///     <StringList count = "3">
        ///         <StringList>Value1</StringList>
        ///         <StringList>Value2</StringList>
        ///         <StringList>Value3</StringList>
        ///     </StringList>
        /// </code>
        /// </remarks>
        public static bool AddProperty(ref XElement xmlTree, string propertyName, object propertyValue, string version)
        {
            if (xmlTree == null)
                return false;

            if (string.IsNullOrWhiteSpace(propertyName))
                return false;

            if (propertyValue == null)
                propertyValue = String.Empty;

            if (propertyName == "MyLogger" || propertyName == "SampleSettings")
            {
                Logger.Write("Warning: CsTool.Logger.AddProperty: Property({0}) is not supported", propertyName);
            }
            try
            {
                Type type = propertyValue?.GetType();
                string typeName = type.Name;
                //
                // A class is saved as a sub section
                //
                if (type.IsClass)
                {
                    // get attributes from class
                    var modelSettingsAttribute1 = propertyValue.GetType().GetCustomAttribute<ModelSettingsClassAttribute>();
                    var modelSettingsAttribute2 = propertyValue.GetType().GetCustomAttribute<ModelSettingsInstanceAttribute>();
                    if (modelSettingsAttribute1 != null || modelSettingsAttribute2 != null)
                    {
                        // Create child XElement containing all properties of this class instance
                        XElement xList = AddClass(propertyName, propertyValue, version);
                        if (xList != null && xList.Descendants().Count() > 0)
                        {
                            xmlTree.Add(xList);
                            return true;
                        }
                        return false;
                    }
                }
                //
                // Provide special handling for enumerations and string arrays
                //
                if (type.IsEnum)
                {
                    var names = Enum.GetNames(type);
                    string usage = string.Join(" ", names);
                    xmlTree.Add(
                        new XElement(propertyName + "-Help", new XAttribute("usage", usage)),
                        new XElement(propertyName, propertyValue.ToString())
                    );
                    return true;
                }

                //
                // Export the List<T> as a series of elements
                if (type.Name.StartsWith("List"))
                {
                    Int32 i = 0;
                    int count = 0;
                    try
                    {
                        var propertyValueList = propertyValue as IEnumerable;
                        foreach (object element in propertyValueList)
                        {
                            count++;
                        }
                        var elementType = propertyValueList.GetType().GetGenericArguments()[0];
                        XElement xList = new XElement(propertyName
                            , new XAttribute("count", count)
                            , new XAttribute("type", propertyValueList.GetType().Name)
                            , new XAttribute("elementType",elementType.Name));

                        foreach (object element in propertyValueList)
                        {
                            // select value from propertyValueList
                            AddProperty(ref xList, propertyName, element, version);
                        }
                        xmlTree.Add(xList);
                        return true;
                    }
                    catch (Exception exception)
                    {
                        // Log the exception
                        Log.Write(exception, "Exception at element[{0},{1}]", propertyName, i);
                    }
                    return false;
                }
                //
                // Else, add XElement based on the propertyType
                //
                switch (typeName)
                {
                    case "String":
                        string elementValue = MyUtilities.InsertEnvironmentVariables(propertyValue as string);
                        xmlTree.Add(new XElement(propertyName, elementValue));
                        break;
                    default:
                        elementValue = propertyValue.ToString();
                        xmlTree.Add(new XElement(propertyName, elementValue));
                        break;
                }
                return true;
            }
            catch (Exception exception)
            {
                Logger.Write(exception, "Exception at element[" + propertyName + "]");
            }
            return false;
        }

        /// <summary>
        /// Add all properties from a class instance that are marked with the ModelSettingsProperty Attribute to the XElement.
        /// </summary>
        /// <remarks>
        /// Usage:
        ///     MySettingsClass mySettings = new MySettingsClass();
        ///     XElement xList = XmlSettingsParsing.AddClass("MyModule.MySettings", mySettings);
        /// 
        /// It is customary to use the namespace of the class as the xmlElementGroupName. The settings are all marshalled into one settings class
        /// where that class may contain instances of other classes. Use [ModelSettingsClass] attribute to create sub sections in your XElement.
        /// 
        /// Example Class definition:
        /// 
        /// <code>
        ///     using CsTool.Logger.Model;
        ///
        ///     [ModelSettingsClass]
        ///     public class MySettingsClass
        ///     {
        ///         [ModelSettingsProperty]
        ///         public string[] StringList1 { get; set; } = { "Value1", "Value2", @"C:\ProgramData\TestFolder" };
        /// 
        ///         [ModelSettingsProperty]
        ///         public DateTimeOffset DateNow1 { get; set; } = DateTimeOffset.Now;
        ///
        ///         [ModelSettingsProperty]
        ///         public int[] ValueList1 { get; set; } = { 1, 2, 3, 4, 5 };
        ///         
        ///         [ModelSettingsClass]
        ///         public MoreSettingsClass MoreInfo { get; set; } = new MoreSettingsClass();
        ///     }
        ///     
        ///     [ModelSettingsClass]
        ///     public class MoreSettingsClass
        ///     {
        ///         [ModelSettingsProperty]
        ///         public string[] InfoList2 { get; set; } = { "Value4", "Value5", "Value6" };
        ///     }
        ///
        /// Example XElement produced:
        /// 
        /// <CsTool.Logger>
        ///     <LogBase version="1.0.0" lastsaved="28/09/2024 4:12:56 PM +10:00">
        ///         <CountLoggedMessagesMaximum>100000</CountLoggedMessagesMaximum>
        ///         <CountOldLogFilesToKeep>20</CountOldLogFilesToKeep>
        ///         <FileNameDateFormat></FileNameDateFormat>
        ///         <IsConsoleLoggingEnabled>False</IsConsoleLoggingEnabled>
        ///         <IsLoseMessageOnBufferFull>False</IsLoseMessageOnBufferFull>
        ///         <IsShowMessagesEnabledByDefault>True</IsShowMessagesEnabledByDefault>
        ///         <LogFilePath>%STARTUPDIR%\Logs</LogFilePath>
        ///         <LogThresholdMaxLevel-Help usage = "LogFatal LogImportantInfo LogCritical LogError LogWarning LogInfo LogDebug LogVerbose" />
        ///         < LogThresholdMaxLevel > LogInfo </ LogThresholdMaxLevel >
        ///     </LogBase>
        /// </ CsTool.Logger >
        /// 
        /// </code>
        /// </remarks>
        /// <param name="xmlElementGroupName">Optional. The name of the XElement group that the properties will be added to.
        /// If null the Class Name will be used</param>
        /// <param name="classInstance">The Class Instance to extract properties from</param>
        /// <param name="version">The version of this settings file section</param>
        /// <returns>Returns an XElement containing all properties with the [ModelSettings*] attributes, null if no properties are exposed.</returns>
        public static XElement AddClass(string xmlElementGroupName, object classInstance, string version)
        {
            if (classInstance == null)
                return null;

            Type logBasePropertiesType = classInstance.GetType();
            string typeName = classInstance.GetType().Name;
            if (string.IsNullOrWhiteSpace(xmlElementGroupName))
                xmlElementGroupName = typeName;

            XElement xmlSection = new XElement(xmlElementGroupName
                                    , new XAttribute("class", typeName)
                                    /*, new XAttribute("name", "")*/
                                    , new XAttribute("version", version)
                                    , new XAttribute("lastsaved", DateTimeOffset.Now.ToString())
                                    );
            try
            {
                //
                // Sort all of the properties of interest
                //
                SortedDictionary<string, PropertyInfo> propertyList = new SortedDictionary<string, PropertyInfo>();
                PropertyInfo[] properties = logBasePropertiesType.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    // Only include properties that are read/write and not null, public and ModelSettingsPropertyAttribute
                    bool isSetting = property.GetCustomAttribute<ModelSettingsPropertyAttribute>() != null
                                    || property.GetCustomAttribute<ModelSettingsInstanceAttribute>() != null
                                    || property.GetCustomAttribute<ModelSettingsPropertyWithSubstitutionsAttribute>() != null;
                    if (property.PropertyType.IsPublic && isSetting)
                    {
                        propertyList.Add(property.Name, property);
                    }
                }

                foreach (KeyValuePair<string, PropertyInfo> item in propertyList)
                {
                    PropertyInfo property = item.Value;
                    try
                    {
                        object value = property.GetValue(classInstance);
                        if (property.GetCustomAttribute<ModelSettingsPropertyWithSubstitutionsAttribute>() != null)
                        {
                            value = MyUtilities.InsertEnvironmentVariables(value as string);
                        }
                        //if ( property.Name == "Name" && value.ToString().Length > 0)
                        //{
                        //    xmlSection.SetAttributeValue("name", value);
                        //}
                        AddProperty(ref xmlSection, property.Name, value, version);
                    }
                    catch (Exception exception)
                    {
                        Log.Write(exception, "Error: CsTool.Logger.AddClass({0}): Property({1}) exception", xmlElementGroupName, property.Name);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger.AddClass: AddClass({0}) exception", xmlElementGroupName);
            }
            int count = xmlSection.Descendants().Count();
            if (count == 0)
                return null;
            return xmlSection;
        }


        #endregion Xml File Write Interface

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Xml File Read Interface
        /// <summary>
        /// Update Properties of a Settings Class from an XElement. Used to read in saved values from the settings file.
        /// </summary>
        /// <param name="settingsSection">The XElement being comprising of this section</param>
        /// <param name="classInstance">The class instance that defines and receives the properties from the XElement</param>
        /// <returns>The number of errors detected whilst loading properties</returns>
        public static int LoadClassValues(XElement settingsSection, object classInstance, string version)
        {
            int errors = 0;
            if (settingsSection == null) return -1;
            if (classInstance == null) return -1;

            Type logBasePropertiesType = classInstance.GetType();
            string typeName = classInstance.GetType().Name;
            try
            {
                //
                // Update all of the properties of interest
                //
                PropertyInfo[] properties = logBasePropertiesType.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (!property.PropertyType.IsPublic) continue;

                    // Only include properties that are read/write and not null, public and ModelSettings*Attribute
                    if (property.CanWrite
                        && (
                            property.GetCustomAttribute<ModelSettingsPropertyAttribute>() != null
                            || property.GetCustomAttribute<ModelSettingsPropertyWithSubstitutionsAttribute>() != null
                            ))
                    {
                        try
                        {
                            Type propertyType = property.PropertyType;
                            string propertyTypeName = propertyType.Name;
                            object oldValue = property.GetValue(classInstance);
                            object newValue = null;

                            if (propertyType.IsArray)
                            {
                                Log.Write("Warning: CsTool.Logger.LoadClassValues: Property({0}) Type({1}) currently not supported",
                                    property.Name, propertyTypeName);
                                continue;
                            }

                            //
                            // Supporting List<T> in lieu of Array
                            //
                            if (propertyTypeName.StartsWith("List"))
                            {
                                newValue = GetPropertyAsType(settingsSection, property, classInstance, ref errors);
                                property.SetValue(classInstance, newValue);
                            }

                            //
                            // Supporting basic classes with framework to perform special handling on strings
                            //
                            switch (propertyTypeName)
                            {
                                case "String":
                                    newValue = GetPropertyAsType(settingsSection, property, classInstance, ref errors);
                                    if (property.GetCustomAttribute<ModelSettingsPropertyWithSubstitutionsAttribute>() != null)
                                    {
                                        // Replace environment variables with current values
                                        newValue = MyUtilities.ExpandEnvironmentVariables(newValue as string);
                                    }
                                    break;
                                default:
                                    // Try generic conversions
                                    newValue = GetPropertyAsType(settingsSection, property, classInstance, ref errors);
                                    break;
                            }
                            property.SetValue(classInstance, newValue);
                        }
                        catch (Exception exception)
                        {
                            errors++;
                            Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}): Property({1}) exception", typeName, property.Name);
                        }
                        continue;
                    }
                    // Looking for ModelSettingsClass instances to process
                    if (property.PropertyType.IsPublic && property.GetCustomAttribute<ModelSettingsInstanceAttribute>() != null)
                    {
                        try
                        {
                            XElement xmlSubSection = settingsSection.Element(property.Name);
                            if (xmlSubSection == null)
                            {
                                Log.Write("Warning: CsTool.Logger.LoadClassValues: Class({0}) properties not found in settings file", property.Name);
                                errors++;
                                continue;
                            }
                            int subErrors = LoadClassValues(xmlSubSection, property.GetValue(classInstance), version);
                            if (subErrors != 0)
                            {
                                errors += Math.Abs(subErrors);
                                Log.Write("Error: CsTool.Logger.LoadClassValues: Class({0}) properties not set", property.Name);
                            }
                        }
                        catch (Exception exception)
                        {
                            errors++;
                            Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}): Property({1}) exception", typeName, property.Name);
                        }
                        continue;
                    }
                }
            }
            catch (Exception exception)
            {
                errors++;
                Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}) Errors({1})", typeName, errors);
            }
            return errors;
        }

        /// <summary>
        /// Extracts the specified enumeration from the individual XML Element.
        /// Supports basic data types string, int, bool and enum.
        /// </summary>
        /// <param name="settingsSection">XML Section containing the desired element</param>
        /// <param name="propertyInfo">PropertyInfo object for the desired propertyInfo</param>
        /// <param name="classInstance">The class instance that contains the desired propertyInfo.</param>
        /// <returns>New propertyInfo value if successfully, current propertyInfo value otherwise.</returns>
        public static object GetPropertyAsType(XElement settingsSection, PropertyInfo propertyInfo, object classInstance, ref Int32 errors)
        {
            // Object Type
            Type type = propertyInfo.PropertyType;
            object result = propertyInfo.GetValue(classInstance);
            try
            {
                XElement element = settingsSection.Element(propertyInfo.Name);
                if (element == null)
                {
                    errors++;
                    return result;
                }

                //
                // Process List<T>
                //
                if (propertyInfo.PropertyType.Name.StartsWith("List"))
                {
                    return GetPropertyAsList(settingsSection, propertyInfo, classInstance, ref errors);
                }

                //
                // Standard propertyInfo including String, Int, Enum
                //
                string tempString = element.Value.ToString();
                var tmpEnum = TypeDescriptor.GetConverter(type);
                if (tmpEnum.CanConvertFrom(typeof(string)))
                {
                    result = tmpEnum.ConvertFromString(tempString);
                }
                else
                {
                    Log.Write("Error: CsTool.Logger: Unexpected type conversion failure: Property({0})", propertyInfo.Name);
                    errors++;
                }
            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger: Failed to extract property({0}) from settings file.", propertyInfo.Name);
                errors++;
            }
            return result;
        }

        /// <summary>
        /// Ditto specifically for List<T> properties.
        /// </summary>
        /// <param name="settingsSection"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="classInstance"></param>
        /// <returns></returns>
        public static object GetPropertyAsList(XElement settingsSection, PropertyInfo propertyInfo, object classInstance, ref Int32 errors)
        {
            object result = propertyInfo.GetValue(classInstance);

            if (!propertyInfo.PropertyType.Name.StartsWith("List"))
            {
                errors++;
                return result;
            }

            try
            {
                XElement element = settingsSection.Element(propertyInfo.Name);
                if (element == null) return result;
                int count = 0;
                count = Convert.ToInt32(element.Attribute("count")?.Value);
                Type elementType = result.GetType().GetGenericArguments()[0];
                var converter = TypeDescriptor.GetConverter(elementType);
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType);

                foreach (XElement subElement in element.Elements(propertyInfo.Name))
                {
                    string tempString3 = subElement.Value.ToString();
                    var convertedValue = converter.ConvertFromString(tempString3);
                    try
                    {
                        list.Add(convertedValue);
                    }
                    catch (Exception exception)
                    {
                        Log.Write(exception, "Error: CsTool.Logger: Failed to add element to list {0}", propertyInfo.Name);
                        errors++;
                    }
                }
                return list;

            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger: Failed to extract propertyValue {0} from settings file.", propertyInfo.Name);
                errors++;
            }
            return result;
        }


        #endregion Xml File Read Interface

    }
}
