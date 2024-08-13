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
    using System.Reflection;
    using System.Xml.Linq;
    using ExtensionMethods;
    using Model;

    public static class XmlSettingsParsing
    {
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
        public static bool Add(ref XElement xmlTree, string propertyName, object propertyValue)
        {
            if (xmlTree == null)
                return false;

            if (propertyName.IsNullOrWhiteSpace())
                return false;

            if (propertyValue == null)
                propertyValue = String.Empty;

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
                    var modelSettingsAttribute = propertyValue.GetType().GetCustomAttribute<ModelSettingsClassAttribute>();
                    if (modelSettingsAttribute != null)
                    {
                        // Create child XElement containing all properties of this class instance
                        XElement xList = AddClass(propertyName, propertyValue);
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
                            Add(ref xList, propertyName, element);
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
        /// Add all properties from the class instance that are marked with the ModelSettingsProperty Attribute to the XElement.
        /// </summary>
        /// <param name="xmlTree">XElement to add the Xml Group to</param>
        /// <param name="xmlClassInstanceName">The name of the element group</param>
        /// <param name="classInstance">The Class Instance to extract properties from</param>
        /// <returns>true if successful</returns>
        private static bool AddClass(ref XElement xmlTree, string xmlClassInstanceName, object classInstance)
        {
            if (xmlTree == null)
                return false;

            if (xmlClassInstanceName.IsNullOrWhiteSpace())
                return false;

            if (classInstance == null)
                return false;

            XElement xmlSection = AddClass(xmlClassInstanceName, classInstance);
            if (xmlSection == null)
                return false;

            xmlTree.Add(xmlSection);
            return true;
        }

        /// <summary>
        /// Add all properties from the class instance that are marked with the ModelSettingsProperty Attribute to the XElement.
        /// </summary>
        /// <param name="xmlTree">XElement to add the Xml Group to</param>
        /// <param name="xmlClassInstanceName">The name of the element group</param>
        /// <param name="classInstance">The Class Instance to extract properties from</param>
        /// <returns>Returns an XElement containing all model settings properties, null if no properties are exposed.</returns>
        /// <remarks>
        /// Usage:
        ///     AnotherSettingsClass anotherClass = new AnotherSettingsClass();
        ///     XElement xList = XmlSettingsParsing.AddClass("CsTool.OtherSettings", anotherClass);
        /// 
        /// Example Class definition:
        /// <code>
        ///     [ModelSettingsClass]
        ///     public class ModelSettingsSample
        ///     {
        ///         [ModelSettingsProperty]
        ///         public string[] StringList2 { get; set; } = { "Value1", "Value2", @"C:\ProgramData\TestFolder" };
        /// 
        ///         [ModelSettingsProperty]
        ///         public DateTimeOffset DateNow2 { get; set; } = DateTimeOffset.Now;
        ///
        ///         [ModelSettingsProperty]
        ///         public int[] ValueList2 { get; set; } = { 1, 2, 3, 4, 5 };
        ///         
        ///         [ModelSettingsClass]
        ///         public AnotherSettingsClass AnotherClassInstance { get; set; } = new AnotherSettingsClass();
        ///     }
        /// </code>
        /// </remarks>
        public static XElement AddClass(string xmlClassInstanceName, object classInstance)
        {
            if (xmlClassInstanceName.IsNullOrWhiteSpace())
                return null;

            if (classInstance == null)
                return null;

            Type logBasePropertiesType = classInstance.GetType();
            string typeName = classInstance.GetType().Name;

            XElement xmlSection = new XElement(xmlClassInstanceName
                , new XAttribute("class", typeName)
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
                                    || property.GetCustomAttribute<ModelSettingsInstanceAttribute>() != null;
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
                        Add(ref xmlSection, property.Name, value);
                    }
                    catch (Exception exception)
                    {
                        Log.Write(exception, "Error: CsTool.Logger.AddClass({0}): Property({1}) exception", xmlClassInstanceName, property.Name);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger.AddClass: AddClass({0}) exception", xmlClassInstanceName);
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
        /// Update Properties of a Settings Class from an XElement. Used to read in save values from the settings file.
        /// </summary>
        /// <param name="settingsSection"></param>
        /// <param name="classInstance"></param>
        /// <returns></returns>
        public static bool LoadClassValues(XElement settingsSection, object classInstance)
        {
            if (settingsSection == null) return false;
            if (classInstance == null) return false;

            Type logBasePropertiesType = classInstance.GetType();
            string typeName = classInstance.GetType().Name;
            int errors = 0;
            try
            {
                //
                // Update all of the properties of interest
                //
                PropertyInfo[] properties = logBasePropertiesType.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    // Only include properties that are read/write and not null, public and ModelSettingsPropertyAttribute
                    if (property.PropertyType.IsPublic && property.CanWrite
                        && property.GetCustomAttribute<ModelSettingsPropertyAttribute>() != null)
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
                                newValue = GetPropertyAsType(settingsSection, property, classInstance);
                                property.SetValue(classInstance, newValue);
                            }

                            //
                            // Supporting basic classes with framework to perform special handling on strings
                            //
                            switch (propertyTypeName)
                            {
                                case "String":
                                    // For the moment not replacing environment variables at this location
                                    newValue = GetPropertyAsType(settingsSection, property, classInstance);
                                    //newValue = MyUtilities.InsertEnvironmentVariables(newValue as string);
                                    break;
                                default:
                                    // Try generic conversions
                                    newValue = GetPropertyAsType(settingsSection, property, classInstance);
                                    break;
                            }
                            property.SetValue(classInstance, newValue);
                        }
                        catch (Exception exception)
                        {
                            Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}): Property({1}) exception", typeName, property.Name);
                            errors++;
                        }
                    }
                }
                //
                // Update all of the propertyInfo class instances of interest from the XElement(i.e. file)
                //
                foreach (PropertyInfo property in properties)
                {
                    // Looking for ModelSettingsClass instances to process
                    if (property.PropertyType.IsPublic && property.GetCustomAttribute<ModelSettingsInstanceAttribute>() != null)
                    {
                        try
                        {
                            XElement xmlSubSection = settingsSection.Element(property.Name);
                            if (xmlSubSection == null)
                            {
                                Log.Write("Warning: CsTool.Logger.LoadClassValues: Class({0}) properties not found in settings file", property.Name);
                                continue;
                            }
                            if (!LoadClassValues(xmlSubSection, property.GetValue(classInstance)))
                            {
                                Log.Write("Error: CsTool.Logger.LoadClassValues: Class({0}) properties not set", property.Name);
                                errors++;
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}): Property({1}) exception", typeName, property.Name);
                            errors++;
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger.LoadClassValues({0}) Errors({1})", typeName, errors);
                errors++;
            }
            if (errors == 0) return true;
            return false;
        }

        /// <summary>
        /// Extracts the specified enumeration from the individual XML Element.
        /// Supports basic data types string, int, bool and enum.
        /// </summary>
        /// <param name="settingsSection">XML Section containing the desired element</param>
        /// <param name="propertyInfo">PropertyInfo object for the desired propertyInfo</param>
        /// <param name="classInstance">The class instance that contains the desired propertyInfo.</param>
        /// <returns>New propertyInfo value if successfully, current propertyInfo value otherwise.</returns>
        public static object GetPropertyAsType(XElement settingsSection, PropertyInfo propertyInfo, object classInstance)
        {
            // Object Type
            Type type = propertyInfo.PropertyType;
            object result = propertyInfo.GetValue(classInstance);
            try
            {
                XElement element = settingsSection.Element(propertyInfo.Name);
                if (element == null) return result;

                //
                // Process List<T>
                //
                if (propertyInfo.PropertyType.Name.StartsWith("List"))
                {
                    return GetPropertyAsList(settingsSection, propertyInfo, classInstance);
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
                }
            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger: Failed to extract propertyValue {0} from settings file.", propertyInfo.Name);
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
        public static object GetPropertyAsList(XElement settingsSection, PropertyInfo propertyInfo, object classInstance)
        {
            object result = propertyInfo.GetValue(classInstance);

            if (!propertyInfo.PropertyType.Name.StartsWith("List"))
            {
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
                    }
                }
                return list;

            }
            catch (Exception exception)
            {
                Log.Write(exception, "Error: CsTool.Logger: Failed to extract propertyValue {0} from settings file.", propertyInfo.Name);
            }
            return result;
        }


        #endregion Xml File Read Interface

    }
}
