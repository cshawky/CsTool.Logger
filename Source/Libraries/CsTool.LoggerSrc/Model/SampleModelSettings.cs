namespace CsTool.Logger.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using CsTool.Logger;

    /// <summary>
    /// This is a bit of a story/history and the design dates back to 2012 with some overhang from C,C++ days of 2004.
    /// 
    /// This C# file provides an example of several SettingsModel classes to demonstrate the use of attributes and
    /// shared code to manage the retrieval and saving of properties from an XML file.
    /// 
    /// Attributes are being used to allow gradual abstraction of the CsTool.CoreUtilities feature for importing and
    /// exporting settings to/from file, without undue code writing. The author has not been comfortable with other generic
    /// mechanisms particularly where they require the import of third party packages or the use of built in .Net 
    /// functionality that is not very tolerant to user error. XML was selected many years ago when it was in favour.
    /// 
    /// The architecture assumes the MVVM or VMC design pattern. The Model contains global and instance specific properties
    /// and typically inherits a ModelSettings class that focuses on those properties and functionality related to permanent
    /// data store of the model data. As a minimum, it handles the single XML Settings File for that DLL or application. For
    /// each DLL there exists a single Model implemented using the Singleton Instance technique.
    /// 
    /// Model settings load/save techniques are currently spread across parallel code streams. The current thought pattern is
    /// now to centralise a core interface with CsTool.Logger rather than in the various customer implementations of CoreUtilities.
    /// CoreUtilities would also be merged across implementations but allow applications to be developed with CsTool.Logger and
    /// without CoreUtilities given other programmers might prefer to use their own code base other than the logger functionality
    /// herein.
    /// </summary>
    /// <remarks>
    /// The SampleModelSettings class below demonstrates the use of each custom attribute.
    /// If this is the primary settings class for the DLL, then it should inherit <code>DefaultSettings</code>
    /// which currently resides in CsTool.CoreUtilities.
    /// TODO incorporate the equivalent of DefaultSettings class into CsTool.Logger so that this architecture
    /// will work independent to CoreUtilities. In this interim migration, the settings are handled through
    /// <code>AppDefaults</code> class. As may be seen, that class re introduces methods from CoreUtilities
    /// uplifting them to utilise System.Reflection more extensively.
    /// 
    /// The code would be something like:
    /// <code>SampleModelSettings SampleSettings1 = new SampleModelSettings();</code>
    /// 
    /// This settings class instance is represented in the XML File as follows:
    /// 
    /// <code>
    ///  <?xml...?>
    ///  <Settings version="x.y.x"...>
    ///     <AppDefaults>
    ///       <NameSpace>
    ///         <SampleSettings1 class="SampleModelSettings" lastsaved="date time">
    ///             <StandardString>Standard Value</StandardString>
    ///             <StandardInt>42</StandardInt>
    ///             <StringList count="3">
    ///                 <StringList>Value1</StringList>
    ///                 <StringList>Value2</StringList>
    ///                 <StringList>%ProgramData%\TestFolder</StringList>
    ///             </StringList>
    ///             <DateNow>10/10/2010 10:10:10 PM +10:00</DateNow>
    ///             <ValueList count="5">
    ///                 <ValueList>1</ValueList>
    ///                 <ValueList>2</ValueList>
    ///                 <ValueList>3</ValueList>
    ///                 <ValueList>4</ValueList>
    ///                 <ValueList>5</ValueList>
    ///             </ValueList>
    ///             <AnotherSetting>
    ///                 <StringList2 count="3">
    ///                     <StringList2>Value109</StringList2>
    ///                     <StringList2>Value209</StringList2>
    ///                     <StringList2>%ProgramData%\TestFolder2</StringList2>
    ///                 </StringList2>
    ///                 <ValueList2 count = "5" >
    ///                     < ValueList2 > 91 </ ValueList2 >
    ///                     < ValueList2 > 92 </ ValueList2 >
    ///                     < ValueList2 > 93 </ ValueList2 >
    ///                     < ValueList2 > 94 </ ValueList2 >
    ///                     < ValueList2 > 95 </ ValueList2 >
    ///                 </ ValueList2 >
    ///             </AnotherSetting>
    ///         </SampleSettings1>
    ///       </NameSpace>
    ///     </AppDefaults>
    ///  </Settings>
    /// </code>
    /// 
    /// For arrays in the XML file, the count of records is for information only. The intent is that a user might
    /// manually edit the content of the file, adding or removing elements of the array. A warning will be logged
    /// but the file will be considered valid.
    /// Where properties are missing from the file, that file will be overwritten after loading those properties that
    /// can be loaded. Thus a file should automatically be repaired or upgraded based on change detection.
    /// 
    /// Side note: Notice the Environment variable substitution between the file info and property value. The code
    /// will perform specific environment variable substitution during save, but at present does not reverse it on
    /// load, rather leaving that to the programmer's intentions: <code>MyUtilities.InsertEnvironmentVariables()</code>
    /// 
    /// </remarks>
    [ModelSettingsClass]
    public class SampleModelSettings
    {

        /// <summary>
        /// The Name that uses this configuration settings data set.
        /// This property is always first in the configuration file.
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public string Name { get; set; } = "Settings1";

        /// <summary>
        /// An arbitrary number for the property section. It could be used as an index id
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public int Id { get; set; } = 1;

        /// <summary>
        /// Settings section Description. Appears after name.
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public string Description { get; set; } = "Settings Name 1";

        /// <summary>
        /// Settings section help. Appears after Description
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public string Help { get; set; } = "\nThis is a sample Xml Settings file from CsTool.Logger.\nEnjoy!\n";

        /// <summary>
        /// A standard string settings property.
        /// </summary>
        [ModelSettingsProperty]
        public string StandardString { get; set; } = "Hello World!";

        [ModelSettingsProperty]
        public int StandardInt { get; set; } = 42;

        /// <summary>
        /// An array of strings must be handled through a List()
        /// </summary>
        [ModelSettingsProperty]
        public List<string> StringList { get; set; } = new List<string> { "Value1", "Value2", @"C:\ProgramData\TestFolder" };

        /// <summary>
        /// An array of strings must be handled through a List()
        /// TODO The List code is not working when [ModelSettingsPropertyWithSubstitutions] is used
        /// </summary>
        [ModelSettingsPropertyWithSubstitutions]
        public List<string> StringListSubstituted { get; set; } = new List<string> { "Value1", "Value2", @"C:\ProgramData\TestFolder" };

        [ModelSettingsProperty]
        public DateTimeOffset DateNow { get; set; } = DateTimeOffset.Now;

        [ModelSettingsProperty]
        public List<int> ValueList { get; set; } = new List<int> { 1, 2, 3, 4, 5 };

        /// <summary>
        /// ModelSettingsInstance results in the instance being saved in Group 3 of the section
        /// Use ModelSettingsProperty to include in alphabetical order with the rest of the properties.
        /// </summary>
        [ModelSettingsInstance]
        public AnotherSettingsClass AnotherSetting { get; set; } = new AnotherSettingsClass();
    }

    /// <summary>
    /// This is a separate class typical of one that might be instanced multiple times. Thus each instance
    /// in the parent settings class would be saved separately. This class may also include another settings instance.
    /// </summary>
    [ModelSettingsClass]
    public class AnotherSettingsClass
    {
        /// <summary>
        /// Private properties are not included regardless
        /// </summary>
        private bool _isAnotherSetting = true;

        /// <summary>
        /// This property is not included as no attribute marker is present.
        /// </summary>
        public bool IsAnotherSetting
        {
            get => _isAnotherSetting;
            set => _isAnotherSetting = value;
        }

        /// <summary>
        /// Include as a property. Arrays are supported.
        /// TODO The List code is not working when [ModelSettingsPropertyWithSubstitutions] is used
        /// </summary>
        [ModelSettingsPropertyWithSubstitutions]
        public List<string> StringList2 { get; set; } = new List<string> { "Value21", "Value22", "C:\\ProgramData\\TestFolder2" };

        /// <summary>
        /// Include as a property. Arrays are supported.
        /// </summary>
        [ModelSettingsProperty]
        public List<int> ValueList2 { get; set; } = new List<int> { 21, 22, 23, 24, 25 };
    }
}
