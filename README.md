# Introduction

CsTool.Logger is a compact, fast, basic multi-threaded logger for C# .NET Core and .NET Framework.

It is licenced under Apache License Version 2.0, January 2004 http://www.apache.org/licenses/

CsTool.Logger is a simple adaptation of a very old C/C++ logger I ported to C# then simplified and optimised for good performance and simplicity and inclusion in Customer applications.

## Build environment
The project is supported by [Visual Studio 2022](https://visualstudio.microsoft.com/de/vs/community/). It was last tested extensively using Framework 4.8.0 with basic testing under Core 3.1.

Performance testing was conducted against NLog and Serilog but those test projects have been excluded from this release to avoid the need for downloading NuGet packages you won't need as you would be using CsTool.Logger instead of NLog or Serilog.

CsTool.Logger will operate alongside NLog and Serilog but only if full namespaces are used to differentiate between "Logger" classes. CsTool.Logger supports multiple logging streams.

## Version Information and Caveats

CsTool.Logger.dll	Version 2.0.0 Beta (Stable)

Download binaries from https://github.com/cshawky/CsTool.Logger/releases/tag/V2.0.0-Beta.0

The latest release is currently only available by downloading the project and compiling it. NuGet packaging is on the list of things to do.

The toolset DLL is labelled Version 2 as this project is a consolidation/simplification of earlier logging implementations (v1.x) to a form suitable for sharing and caring. This is the author's first sizeable project release to GitHub. The author only codes part time, more so as a hobby than anything else. Use in the work environment is limited, though an earlier synchronous release (v1) of this code has been included in critical real-time applications that have been operating 24x7 quite successfully for many years dating back to .NET 2.0. The asynchronous queueing was not present. An even earlier release would support C, C++ on GCC, windows and embedded systems. As such the underlying framework as simple as it is should be mature. The author uses this logger library in every single project.

V2 is stable and was created and bench marked against both NLog and Serilog as an exercise to determine if it was productive to continue supporting one's own logger library or adopting a 3rd party library. The conclusion was to maintain one's own library (due to simplicity and good performance) but massage it into a form that might be re useable by others. Deep inspection of the source code may identify the odd inconsistency in functional comments, i.e. referencing a feature that does not appear to exist in this release. This is a consequence of migrating legacy code, simplifying and removing code bloat et al to create this simpler implementation.

From a GitHub release perspective, this is my first release so I am not sure what issues might arise from others downloading and using it. Please consider it a Beta Release and conduct some testing of your own. The author has tested this with single and multiple log producers during performance load tests but does not consider this testing definitive. Testing was conducted with .NET Framework 4.8 and Core 3.1 only. The current release has been updated to use the latest .NET runtimes 4.8.1 and 7.0 respectively.

Others are welcome to contribute, though the author requests that changes be kept simple. If you want a complex, feature rich logger, look elsewhere or build over CsTool.LoggerSrc to expand it's capabilities. Both NLog and Serilog appear to be excellent functionally rich loggers.

# Implementation

CsTool.Logger may be included into you Visual Studio project in a number of ways:
* Directly into your C# application directly by including CsTool.LoggerSrc using VS code sharing and instantiating the logger in Program.cs
* By including CsTool.Logger.dll in your .NET Framework project (C#, VB, ASP etc)
* By including CsTool.Core.Logger.dll in your .NET Core project (C#, VB, ASP etc)

CsTool.logger is intended to be simple and very efficient causing minimal disruption to process work flow. Only a basic understanding of C# is needed to use this library as a DLL or inline.

The implementation manages programme folder locations for .NET Framework and will locate a suitable writeable location should the application be called from a read only location. 
At this time .NET core code for checking writability of the folder location is not implemented. So ensure the log folder location used is writeable. The best way to achieve this
is to start the application with the start up folder being a location other than Program Files. The logger will then create a subfolder called log under the startup location.
Multiple instances of an application may be run concurrently provided that either the application start up folder is unique or the Application selects a unique logger folder.

This logger was tested against Serilog and NLog and for the use case I tested, was faster than both when used in Asynchronous mode, primarily due to its simplicity.

For example on 24th May 2020 i7 @4.7GHz 6 core/12 threads the Single producer test application achieved approximately 330000 log events per second (a string + a value).

Unlike NLog and Serilog, CsTool.Logger does not implement complex logging interfaces, rather leaving that to the application. It may however, be extended as needs be.
Pre processing log information prior to calling the LogMessage/Write functions is simple but least efficient. Implementing class specific handlers will provide the most
efficient solution but a little more effort and will necessitate inclusion of the source into your own logging DLL or application.

The structure is such that it should be possible to expand the logger capability, though I have not tried this. If you have difficulty or suggestions on how the class structure should be revised, please assist.

The solution contains multiple projects as described in each of the solution sections below:
	
### Tests\Test.Core.Logger2
.NET Core Test Application using CsTool.Core.LoggerSrc directly without needing the DLL
The test code itself is a shared project Test.LoggerSrc
	
## Framework
These projects demonstrate/test usage of CsTool.Logger DLL (or CsTool.LoggerSrc shred source)  with .NET Framework. The DLL has been used extensively with .NET Framework console and WPF applications and DLLs.

### Libraries\CsTool.Logger
.NET Framework DLL: Include this DLL in your .NET core project. See Tests\Test.Core.Logger1 for usage.
Alternatively, for advanced users and for extending logger functionality the shared source may be utilised. See Tests\Test.Core.Logger2 for usage.

### Tests\Test.Logger1
.NET Framework Test Application using CsTool.Core.Logger DLL
The test code itself is a shared project Test.LoggerSrc
	
### Tests\Test.Logger2
.NET Framework Test Application using CsTool.Core.LoggerSrc directly without needing the DLL
The test code itself is a shared project Test.LoggerSrc

## Core
These projects demonstrate/test usage of CsTool.Logger DLL (or CsTool.LoggerSrc shred source) with .NET Core. Log file location safety checks are not fully implemented. The code assumes the location is writeable.

### Libraries\CsTool.Core.Logger
.NET Core DLL: Include this DLL in your .NET core project. See Tests\Test.Core.Logger1 for usage.
Alternatively, for advanced users and for extending logger functionality the shared source may be utilised. See Tests\Test.Core.Logger2 for usage.

### Tests\Test.Core.Logger1
.NET Core Test Application using CsTool.Core.Logger DLL
The test code itself is a shared project Test.LoggerSrc

## Source
### Libraries\CsTool.LoggerSrc
This is the source code for the logger class.

### Tests\Test.LoggerSrc
This is the example Test application source code. Shared source is used to ensure that at all times the implementation supports both Core and Framework .NET implementations. The test applications provided use the same test source code but demonstrate different ways of linking or including the logger class.

# NuGet
This is on the TODO list. I am getting ready to update the project for multiple frameworks before creating my first NuGet package. Any assistance would be welcome.
At present I have a CsTool.Core.Logger implementation and CsTool.Logger for Framework. That latter has a few simple Windows.Forms add ons for messaging. Simple to #define out
or incorporate into a separate package. Other than that it should be fine for Core and Framework, all code existing in a shared source project barring the ShowMessage()...

# Usage

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

# Log priority
The Logger supports three conventions for log priority. Maximum options for priority are available when
using the native LogPriority levels.
1. My Logger historical syntax: LogPriority.{Level}
```C#
   public enum LogPriority : Int32
    {
        // Lowest value
        Always = 0,             // Log requests with this priority will always log
        Fatal = 1,              // Programme cannot continue
        ImportantInfo = 2,      // Equivalent to Info except will be logged if LogPriority.ErrorCritical is enabled
        ErrorCritical = 3,      // Exceptions and bad processing errors likely to require code modification
        ErrorProcessing = 4,    // Normal processing/data errors
        Warning = 5,            // Data processing or warnings (high priority debug messages)
        Info = 6,               // General information
        Debug = 7,              // More detail on processing activities
        Verbose = 8,            // Extensive detail on processing where used
        Never = 9999            // If requests were to use this level, it would never log
        // Highest value
    }
```
2. Serilog compatible levels. This is a subset of LogPriority.
```c#
    public enum LogEventLevel : Int32
    {
        Verbose = LogPriority.Verbose,
        Debug = LogPriority.Debug,
        Information = LogPriority.Info,
        Warning = LogPriority.Warning,
        Error = LogPriority.ErrorCritical,
        Fatal = LogPriority.Fatal
    }
```
3. Nlog compatibility is limited but may be easily extended by expanding the Logger class or migrate to
 CsTool.Logger or Serilog formats.
```C#
	Logger.Info("For info: {0}...",...);
	Logger.Debug("Some details: {0}...",...);
	Logger.Fatal(exception,"An exception: {0}",...);
	Logger.Log(LogLevel.Info,"For info: {0}...",...);
```
In addition, the level at which logs are written may be set using the Logger.LogThresholdMaxLevel property. This may be set at any time and will affect all log messages written after the change.

```C#
    public enum DebugThresholdLevel : Int32
    {
        LogNothing = (-1),
        // Lowest enumeration - most important
        LogFatal = LogPriority.Fatal,
        LogImportantInfo = LogPriority.ImportantInfo,
        LogCritical = LogPriority.ErrorCritical,
        LogError = LogPriority.ErrorProcessing,
        LogWarning = LogPriority.Warning ,
        LogInfo = LogPriority.Info,
        LogDebug = LogPriority.Debug,
        LogVerbose  = LogPriority.Verbose,
        LogEverything = (LogPriority.Never - 1)
    }

	// Default log level:
	Logger.LogThresholdMaxLevel = DebugThresholdLevel.LogEverything;
```
## More Options
There are more options available. Some are exposed directly in Logger.*, whilst others are only available via
Logger.Instance. Some legacy features have not yet been ported or exposed.

The log file path may be modified at any time, but is best done well before your application start processing. The
path and file prepend details may be changed at any time. The log file is closed and reopened when the path is changed.

The File name convention supports: File Prepend text, enable user name, date and time.

```C#
	// Include CsTool.Logger
	using CsTool.Logger;

	namespace MyApplication
	{
		public class MyClass
		{
			public void DoSomething()
			{

				// Perform as much tuning right at the start of your programme BEFORE
				// logging occurs. If the Logger is used prior to tuning, system defaults
				// will be active and the initial log entries will be written to the default location.
				// This is OK, just potentially messy for application debugging on start up.

				//
				// Default file name is {ApplicationName}.log
				// Customisable to: {FilePrepend}_{UserName}_{DateTime}.log
				//
				Logger.Instance.EnableUserNamePrepend = true;
				Logger.FilePrepend = "MyLogs";
				Logger.FileNameDateFilter = "yyyy-MM-dd";


				// Tailor where the log files are stored. The default location is the startup
				// folder. Specifically {StartupFolder}\Logs. If the folder is not writeable, the
				// log files will be stored in the user's AppData\Local\Temp folder.
				// Environment variables are supported.
				Logger.Instance.SetLogDirectory("%SPECIALLOGS%");


				// Set the logging level. This can be changed at any time
				// Default is DebugThresholdLevel.Everything
				Logger.LogThresholdMaxLevel = DebugThresholdLevel.LogWarning;

				// Optionally you can tune it
				Logger.IsLoseMessageOnBufferFull = true; 		// Default false
				Logger.CountLoggedMessagesMaximum = 10000;		// Default 100000
				Logger.CountOldLogFilesToKeep = 2;				// Default 20


				// If the logger DLL is compiled with DEBUG defined, you can also manipulate 
				// Queueing and Async. This is only really useful when including the shared source
				// and is not intended for users, only Logger developers.
				Logger.Instance.IsSynchronousLogger = false;		// Default true
				Logger.Instance.IsQueueAsyncEnabled = true;			// Default true
				Logger.Instance.AddMessageMaxTime = 0;				// Debug thingy for testing performance

				// Use it
				ulong arg1 = 0x1ffff0000;
				string arg2 = "Hello World";
				bool arg3 = true;

				// Use of Write, WriteRaw, LogMessage etc are interchangeable, provided for historical reasons
				// Please refer to Logger.cs for all public methods
				Logger.Write( LogPriority.Info, 
					"Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);

				// Serilog log priority and Write method compatibility (basic)
				Logger.Write( LogEventLevel.Info,
					"Information Test Message ULong({0}) String({1}) Bool({2})", arg1, arg2, arg3);

				// NLog compatible
				Loger.Info("Pre formatted message string");

				try
				{
					...
				}
				catch ( Exception exception)
				{
					// Log the exception: Write, WriteRaw, LogMessage etc are interchangeable
					Logger.Write(exception, "Well Bugger Me: {0}", arg1);
				}

				// Monitor the extent of logging using: 
				uint iDisplayCurrentFileCount = Logger.CountLoggedMessages;
				ulong lDisplayTotalCount = Logger.CountLoggedMessagesTotal;
				ulong lLostDueToQueueOverflow = Logger.lCountLostMessagesTotal;
				// Not useful as the logger is extremely fast				
				int iHowManyProcessingInBackground = Logger.LogQueueCount;

				// Optionally flush at critical points. Otherwise flushing will occur at 2 second
				// intervals as defined in partial class LogBase
				Logger.CloseAndFlush();

				// If you would like to see the latest log file
				Logger.DisplayLogFile();
			}

			public void DoSomethingElse()
			{
				// Multiple Logger interfaces

				LogBase logger1 = new LogBase("Logger1");
				LogBase logger2 = new LogBase("Logger2");
				logger1.Write("Hello World");
				logger2.Write("Hello World");
			}
		}
	}
```

CsTool.Logger has its own log priorities but also supports Serilog priorities. The log levels should be self explanatory as provided by the code snippets below.

Enumerations are used to set the threshold level. For example:
```C#
{
 	Logger.Write(LogPriority.Warning,"This is a warning");
}
```
will be logged if 
```C#
{
		Logger.LogThresholdMaxLevel >= DebugThresholdLevel.LogInfo;
}
```
	// i.e.
```c#
		if ( LogPriority.Warning < Logger.LogThresholdMaxLevel ) Logger.Write("This is logged");
```
The enumerations are described below as excerpt from LogPriority.cs
```C#
	// CsTool.Logger standard enumerations
    public enum LogPriority : Int32
    {
        // Lowest value
        Always = 0,             // Log requests with this priority will always log
        Fatal = 1,              // Programme cannot continue
        ImportantInfo = 2,      // Equivalent to Info except will be logged if 
								// LogPriority.ErrorCritical is enabled
        ErrorCritical = 3,      // Exceptions and bad processing errors likely to 
								// require code modification
        ErrorProcessing = 4,    // Normal processing/data errors
        Warning = 5,            // Data processing or warnings (high priority debug messages)
        Info = 6,               // General information
        Debug = 7,              // More detail on processing activities
        Verbose = 8,            // Extensive detail on processing where used
        Never = 9999            // If requests were to use this level, it would never log
        // Highest value
    }

	// Serilog compatibility:
	public enum LogEventLevel : Int32
    {
        Verbose = LogPriority.Verbose,
        Debug = LogPriority.Debug,
        Information = LogPriority.Info,
        Warning = LogPriority.Warning,
        Error = LogPriority.ErrorCritical,
        Fatal = LogPriority.Fatal
    }
	
	public enum DebugThresholdLevel : Int32
    {
        LogNothing = (-1),
        // Lowest enumeration - most important
        LogFatal = LogPriority.Fatal,
        LogImportantInfo = LogPriority.ImportantInfo,
        LogCritical = LogPriority.ErrorCritical,
        LogError = LogPriority.ErrorProcessing,
        LogWarning = LogPriority.Warning ,
        LogInfo = LogPriority.Info,
        LogDebug = LogPriority.Debug,
        LogVerbose  = LogPriority.Verbose,
        LogEverything = (LogPriority.Never - 1)
    }
```

# TODO List

To name a few:
* NuGet packaging for simpler support on multiple dotnet frameworks
* It looks like some of the Logger tuning parameters have not been exposed properly so you may not be able to change the log priority. Might be fixed.
* Log counters do not generated GUI events.

# Release Summary

2004
* C, C++ implementation of Logger. Not thread safe.

2011
* C# implementation but embedded within a common utilities library. Partially thread safe.

2014
* Duplicated sections of code into client applications as common client library.
* Separate personal DLL thread safe, fast.

2023
* First upload to GitHub as a private repository. All previous releases were part of other solutions.

2024
* Restored some old Windows Forms dialogue box support to allow old projects to utilise the latest library. The CsTool.Wpf project is not ready for release as a full Wpf replacement to any forms interfaces.
* Added an exception if a new logger is initialised incorrectly. i.e. Do not do: Logger myLogger2 = new Logger("Logger2"); Instead use LogBase myLogger2 = new LogBase("Logger2");
* Registered on NuGet, looking into packaging, but would like to make this multi framework supportable in the build.
