# Introduction

CsTool.Logger is a compact, fast, basic multi-threaded, thread safe logger for C# .NET Core and .NET Framework.

It is licenced under Apache License Version 2.0, January 2004 http://www.apache.org/licenses/

CsTool.Logger is a simple adaptation of a very old C/C++ logger I ported to C# then simplified and optimised for good performance and simplicity and inclusion in Customer applications.

It has been performance tested against nlog and serilog for simple logging appearing faster for the simple logging tested.

It is used in real time and critical applications, avoiding the need for too many external libraries.

You might also find the class based xml configuration file interface nice and simple compared to other solutions.

Find more information on [GitHub CShawky CsTool.Logger](https://github.com/cshawky/CsTool.Logger)

Built using Visual Studio 2026 packages for net480, net481, net8, net10.

For support of older frameworks best to download, remove new frameworks, add the older one. You may need to remove or modify the MessageBox interface that I have left there for my old legacy code support.

Alternative, post a request on the repository.

## Basic Usage
```C#
	// Include CsTool.Logger for Framework, or CsTool.Core.Logger for .NET Core
	using CsTool.Logger;

	namespace MyApplication
	{
		public class MyClass
		{
			static int Main(string[] args)
			{
				try
				{
					...
					Logger.Write("Welcome to my app");
					...
					Logger.Write(LogEventLevel.Verbose,"Arguments: {0}", string.Join(" ",args));
					...
					throw new NotImplementedException("Not yet implemented");
				}
				catch ( Exception exception)
				{
					// Log the exception
					Logger.Write(exception, "Bugger");
				}
				finally
				{
					Logger.Write("Bye");
				}
			}
		}
	}
```
NLog and Serilog basic log method syntax are also supported. Such as Debug(), Info(). (proof of concept only)

# Application Defaults
The AppDefaults class provides a generic interface that allows for class based settings to be retrieved and saved
to the AppDefaults file. Abstraction is achieved using Class custom attributes. Very simple to add class based settings to your application. 

```C#
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<Settings version="2.0.0" lastsaved="11/04/2025 4:00:39 PM +10:00">
  <AppDefaults>
    <CsTool.Logger>
      <LogBase class="LogBase" version="1.0.0" lastsaved="11/04/2025 4:00:40 PM +10:00">
        <CountLoggedMessagesMaximum>100000</CountLoggedMessagesMaximum>
        <CountOldLogFilesToKeep>20</CountOldLogFilesToKeep>
        <FileNameDateFormat></FileNameDateFormat>
        <FilePrepend>%APPNAME%</FilePrepend>
        <IsConsoleLoggingEnabled>False</IsConsoleLoggingEnabled>
        <IsLoseMessageOnBufferFull>False</IsLoseMessageOnBufferFull>
        <IsShowMessagesEnabledByDefault>False</IsShowMessagesEnabledByDefault>
        <IsUserNameAppended>True</IsUserNameAppended>
        <LogFilePath>%TEMP%\Logs\%APPNAME%</LogFilePath>
        <LogThresholdMaxLevel-Help usage="LogFatal LogImportantInfo LogCritical LogError LogWarning LogInfo LogDebug LogVerbose" />
        <LogThresholdMaxLevel>LogVerbose</LogThresholdMaxLevel>
      </LogBase>
    </CsTool.Logger>
    <MyApp.MyLibrary.MyModel>
      <MyModelSettings class="MyModelSettings" version="1.0.0" lastsaved="11/04/2025 4:35:14 PM +10:00">
        <Name>Hello</Name>
        <Description>Parameters for tuning this model.</Description>
        <ConfigFileName>Info from a spreadsheet.xlsx</ConfigFileName>
        <ConfigPath>%STARTUPDIR%\Resources</ConfigPath>
        <UserPath>%DROPBOX%\Documents\Stuff</UserPath>
        <IsAutoLoad>True</IsAutoLoad>
        <IsAutoSave>False</IsAutoSave>
        <ItemsT count="5" type="List`1" elementType="System.Int32">
          <Items>1</Items>
          <Items>2</Items>
          <Items>3</Items>
          <Items>4</Items>
          <Items>5</Items>
        </ItemsT>
      </MyModelSettings>
    </MyApp.MyLibrary.MyModel>
  </AppDefaults>
</Settings>
```

## Usage
```C#
[ModelSettingsClass]
    public class SampleModelSettings
    {

        /// <summary>
        /// The Name that uses this configuration settings data set.
        /// This property is always first in the configuration file.
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public string Name { get; set; } = "Hello";

        /// <summary>
        /// An arbitrary number for the property section. It could be used as an index id
        /// Use of this property name is optional.
        /// </summary>
        [ModelSettingsProperty]
        public int Id { get; set; } = 1;

        /// <summary>
        /// An array of strings must be handled through a List()
        /// TODO The List code is not working when [ModelSettingsPropertyWithSubstitutions] is used
        /// </summary>
        [ModelSettingsPropertyWithSubstitutions]
        public List<string> StringListSubstituted { get; set; } = new List<string> { "Value1", "Value2", @"C:\ProgramData\TestFolder" };

        /// <summary>
        /// ModelSettingsInstance results in the instance being saved in Group 3 of the section
        /// Use ModelSettingsProperty to include in alphabetical order with the rest of the properties.
        /// </summary>
        [ModelSettingsInstance]
        public AnotherSettingsClass AnotherSetting { get; set; } = new AnotherSettingsClass();
    }

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
```
