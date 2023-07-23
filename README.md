# Introduction

CsTool.Logger is a compact, fast, basic multi-threaded logger for C# .NET Core and .NET Framework.

It is licenced under Apache License Version 2.0, January 2004 http://www.apache.org/licenses/

CsTool.Logger is a simple adaptation of a very old C/C++ logger I ported to C# then simplified and optimised for good performance and simplicity and inclusion in Customer applications.

## Build environment
The project is supported by [Visual Studio 2022](https://visualstudio.microsoft.com/de/vs/community/). It was last tested extensively using Framework 4.8.0 with basic testing under Core 3.1.

Performance testing was conducted against NLog and Serilog but those test projects have been excluded from this release to avoid the need for downloading NuGet packages you won't need as you would be using CsTool.Logger instead of NLog or Serilog.

CsTool.Logger will operate alongside NLog and Serilog but only if full namespaces are used to differentiate between "Logger" classes. CsTool.Logger supports multiple logging streams.

## Version Information and Caveat

CsTool.Logger.dll	Version 2.0.0 Beta Release

The toolset DLL is labelled Version 2 as this project is a consolidation/simplification of earlier logging implementations (v1.x) to a form suitable for sharing and caring. This is the author's first sizeable project release to GitHub. The author only codes part time, more so as a hobby than anything else. Use in the work environment is limited, though an earlier synchronous release (v1) of this code has been included in critical realtime applications that have been operating 24x7 quite successfully for many years dating back to .NET 2.0. The asynchronous queueing was not present.

V2 was created and bench marked against both NLog and Serilog as an exercise to determine if it was productive to continue supporting one's own logger library or adopting a 3rd party library. The conclusion was to maintain one's own library (due to simplicity and good performance) but massage it into a form that might be re useable by others. Deep inspection of the source code may identify the odd inconsistency in functional comments, i.e. referencing a feature that does not appear to exist in this release. This is a consequence of migrating legacy code, simplifying and removing code bloat et al to create this simpler implementation.

From a GitHub release perspective, please consider it a Beta Release and conduct some testing of your own. The author has tested this with single and multiple log producers during performance load tests but does not consider this testing definitive. Testing was conducted with .NET Framework 4.8 and Core 3.1 only. The current release has been updated to use the latest .NET runtimes 4.8.1 and 7.0 repectively.

Others are welcome to contribut, though the author requests that changes be kept simple. If you want a complex, feature rich logger, look elsewhere or build over CsTool.LoggerSrc to expand it's capabilities. Both NLog and Serilog appear to be excellent functionally rich loggers.

# Implementation

CsTool.ogger may be included into you Visual Studio project in a number of ways:
* Directly into your C# application directly by including CsTool.LoggerSrc using VS code shareing and instantiating the logger in Program.cs
* By including CsTool.Logger.dll in your .NET Framework project (C#, VB, ASP etc)
* By including CsTool.Core.Logger.dll in your .NET Core project (C#, VB, ASP etc)

CsTool.logger is intended to be simple and very efficient causing minimal distruption to process work flow. Only a basic understanding of C# is needed to use this library as a DLL or inline.

The implementation manages programme folder locations for .NET Framework and will locate a suitable writeable location should the application be called from a read only location. 
At this time .NET core code for checking writeability of the folder location is not implemented. So ensure the log folder location used is writeable. The best way to achieve this
is to start the application with the start up folder being a location other than Program Files. The logger will then create a subfolder called log under the startup location.
Multiple instances of an application may be run concurrently provided that either the application start up folder is unique or the Application selects a unique logger folder.

This logger was tested against Serilog and NLog and for the use case I tested, was faster than both when used in Async mode, primarily due to its simplicity.

For example on 24th May 2020 i7 @4.7GHz 6 core/12 threads the Single producer test appication achieved approximately 330000 log events per second (a string + a value).

Unlike NLog and Serilog, CsTool.Logger does not implement complex logging interfaces, rather leaving that to the application. It may however, be extended as needs be.
Pre processing log information prior to calling the LogMessage/Write functions is simple but least efficient. Implementing class specific handlers will provide the most
efficient solution but a little more effort and will necessitate inclusion of the source into your own logging DLL or application.

The solution contains multiple projects as described in each of the solution sections below:
	
### Tests\Test.Core.Logger2
.NET Core Test Application using CsTool.Core.LoggerSrc directly without needing the DLL
The test code itself is a shared project Test.LoggerSrc
	
## Framework
These projects demonstrate/test usage of CsTool.Logger DLL (or CsTool.LoggerSrc shred source)  with .NET Framework. The DLL has been used extensively with .NET Framework console and WPF applications and DLLs.

### Libraries\CsTool.Logger
.NET Framework DLL: Include this DLL in your .NET core project. See Tests\Test.Core.Logger1 for useage.
Alternatively, for advanced users and for extending logger functionality the shared source may be utilised. See Tests\Test.Core.Logger2 for useage.

### Tests\Test.Logger1
.NET Framework Test Application using CsTool.Core.Logger DLL
The test code itself is a shared project Test.LoggerSrc
	
### Tests\Test.Logger2
.NET Framework Test Application using CsTool.Core.LoggerSrc directly without needing the DLL
The test code itself is a shared project Test.LoggerSrc

## Core
These projects demonstrate/test usage of CsTool.Logger DLL (or CsTool.LoggerSrc shred source) with .NET Core. Log file location safety checks are not fully implemented. The code assumes the location is writeable.

### Libraries\CsTool.Core.Logger
.NET Core DLL: Include this DLL in your .NET core project. See Tests\Test.Core.Logger1 for useage.
Alternatively, for advanced users and for extending logger functionality the shared source may be utilised. See Tests\Test.Core.Logger2 for useage.

### Tests\Test.Core.Logger1
.NET Core Test Application using CsTool.Core.Logger DLL
The test code itself is a shared project Test.LoggerSrc

## Source
### Libraries\CsTool.LoggerSrc
This is the source code for the logger class.

### Tests\Test.LoggerSrc
This is the example Test application source code. Shared source is used to ensure that at all times the implementation supports both Core and Framework .NET implementations. The test applications provided use the same test source code but demonstrate different ways of linking or including the logger class.

# Usage

The critical code components for utilising CsTool.Logger are:

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
				// will be active. This is OK, just potentially messy,

				// Set the logging level. This can be changed at any time
				// Default is DebugThresholdLevel.Everything
				Logger.LogThresholdMaxLevel = DebugThresholdLevel.Info;

				// Optionally you can tune it
				Logger.IsLoseMessageOnBufferFull = true; 		// Default false
				Logger.CountLoggedMessagesMaximum = 10000;		// Default 100000
				Logger.CountOldLogFilesToKeep = 2;				// Default 20

				// Optionally tailor the log file. If set FilePrepend and FileNameDateFilter
				// are used as follows:
				// 	string dateText = DateTimeOffset.Now.ToString(FileNameDateFilter);
				// 	name = string.Format("{0} {1}{2}", FilePrepend, dateText, LogFileExtension);
				Logger.FilePrepend = "MyLogs";
				Logger.FileNameDateFilter = "yyyy-MM-dd";

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

CsTool.Logger as it stands 

	# Release Summary

	## Version 
	July 2023
		First upload to GitHub as a private repository. All previous releases were part of other solutions.