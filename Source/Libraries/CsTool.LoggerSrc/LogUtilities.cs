// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

namespace CsTool.Logger
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
//#if NETFRAMEWORK
    using System.Security.Permissions;
    //#endif
    using ExtensionMethods;

    /// <summary>
    /// LogUtilities static class. Supporting methods for the <code>Logger,Logbase</code> classes.
    /// See also <code>MyUtilities</code> which is an extraction from CsTool.CoreUtilities.Utilities
    /// TODO: merge and consolidate, simplify.
    /// </summary>
    /// <remarks>
    /// Properties: <code>MyProcessName,MyStartupPath,MyAssemblyPath</code>
    /// Methods: <code>GetWriteablePath,BackupOldFiles,IsPathReserved,IsFilePathWritable</code>
    /// </remarks>
    public static class LogUtilities
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Initialisation for class <code>MyLogger</code>
        /// </summary>
        static LogUtilities()
        {
            /// <code>SetCurrentDirectory</code> protects and simplifies application output by
            /// forcing the current directory to a writeable path, if the application startup
            /// path was not set correctly. e.g. Run directly from <code>%ProgramFiles%</code>.
            /// It is recommended that each application executes the next line of code at startup.
            // Directory.SetCurrentDirectory(GetWriteablePath());
        }

        #endregion Initialisation

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Properties

        /// <summary>
        /// Thread safe lock object
        /// </summary>
        private static readonly object padlock = new object();

        /// <summary>
        /// The name of the application that is using this toolset.
        /// </summary>
        private static string myProcessName;

        /// <summary>
        /// The name of the application that is using this toolset.
        /// </summary>
        /// <remarks>TODO Provide .NET core cross platform compatibility</remarks>
        public static string MyProcessName
        {
            get
            {
                if (myProcessName == null)
                {
                    lock (padlock)
                    {
                        myProcessName = Process.GetCurrentProcess().ProcessName
                            .Replace(".vshost", "")
                            .Replace("XDesProc", "MyApplication")
                            .Replace(".exe", "");
                    }
                }
                return myProcessName;
            }
            set
            {
                myProcessName = value;
            }
        }

        /// <summary>
        /// The path that the application startup with
        /// </summary>
        private static string myStartupPath;

        /// <summary>
        /// The path that the application starts with
        /// </summary>
        public static string MyStartupPath
        {
            get
            {
                if (myStartupPath == null)
                {
                    lock (padlock)
                    {
                        try
                        {
                            myStartupPath = System.IO.Directory.GetCurrentDirectory();
                        }
                        catch
                        {
                            myStartupPath = @"C:\ProgramData";
                        }
                    }
                }
                return myStartupPath;
            }
            set
            {
                // Override startup path...?
                myStartupPath = value;
            }
        }

        /// <summary>
        /// The path of this assembly (should be same as parent application as we include all DLLs with the app)
        /// </summary>
        private static string myAssemblyPath;

        /// <summary>
        /// The path of this assembly (should be same as parent application as we include all DLLs with the app)
        /// </summary>
        public static string MyAssemblyPath
        {
            get
            {
                if (myAssemblyPath == null)
                {
                    lock (padlock)
                    {
                        myAssemblyPath = Path.GetDirectoryName(
                            Assembly.GetAssembly(typeof(LogUtilities)).Location
                            );
                    }
                }
                return myAssemblyPath;
            }
            set
            {
                // Override startup path...?
                myAssemblyPath = value;
            }
        }
        #endregion Properties

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Methods

        /// <summary>
        /// Get a suitable writeable path at startup for the application. The default location is {StartupPath}
        /// falling back to %TEMP%\{ProcessName}, {Desktop}\{ProcessName}.
        /// If the path does not exist it will be created.
        /// </summary>
        /// <remarks>The application may be started from different locations, thus we check
        /// to ensure that the startup path is not windows read only application paths
        /// or special reserved paths, or visual studio build paths.
        ///
        /// The order of exclusions and preferences for the writeable path is as follows:
        /// 
        /// - Exclude all Windows programme paths (exclude Windows, Program Files, SysWow64 etc)
        /// - Application startup path (initial working directory)
        /// - User's %TEMP% path: Nominate a sub folder for the application
        /// - User's desktop path: Nominate sub folder for the application
        ///
        /// Use of IsPathReserved(path) also confirms that the path is writeable so that the
        /// sub folder <code>Logs<code> may be created.
        /// </remarks>
        public static string GetWriteablePath(string path = null)
        {
            //
            // Preferred path if not specified is the startup path
            //
            if (string.IsNullOrWhiteSpace(path))
                path = MyStartupPath;
            //Log.Write("GetWriteablePath: Check path {0}", path);

            //
            // Exclude all Visual Studio and Windows programme paths. i.e. all admin and read only paths
            //
            if (IsPathReserved(path))
            {
                path = Environment.GetEnvironmentVariable("TEMP");
                //Log.Write("GetWriteablePath: Check path {0}", path);
                if (IsPathReserved(path))
                {
                    path = Environment.SpecialFolder.DesktopDirectory.ToString();
                    //Log.Write("GetWriteablePath: Check path {0}", path);
                    if (IsPathReserved(path))
                    {
                        throw new DirectoryNotFoundException("The application could not find a suitable log path to write to. Please ensure that the startup path, %TEMP% path or Desktop path are writeable");
                    }
                }
                path += @"\" + MyProcessName;
                //Log.Write("GetWriteablePath: Check path {0}", path);
                if (IsPathReserved(path))
                {
                    // Rare but the path might exist and be read only
                    throw new DirectoryNotFoundException("The application could not find a suitable log path to write to. Please ensure that the startup path, %TEMP% path or Desktop path are writeable");
                }
            }
            //
            // To avoid exceptions in calling code that forgets to create the folder,
            // create the folder where the path does not yet exist and the parent folder is writeable
            //
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                //Log.Write("GetWriteablePath: Created path {0}", path);
            }
            return path;
        }

        /// <ssummary>
        /// Renames old files if they exist prior to creating a new one of the same name.
        /// 
        /// fullFileName = {Path}\{Filename}.{extension}
        /// 
        /// If incrementNameNotExtension == false the backup files are named:
        ///     {Path}\{Filename}.{extension}{optionalExtension}_n
        /// where n = backup number 1..countOldFilesToKeep
        /// 
        /// If incrementNameNotExtension == true:
        ///     {Path}\{Filename}_{optionalExtension}_n.{extension}
        /// 
        /// NOTE: This method is not thread safe.
        /// 
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="countOldFilesToKeep"></param>
        /// <param name="optionalExtension">Optional file extension to append to filename. e.g. ".bak" or "_Save"</param>
        /// <param name="incrementNameNotExtension">True to increment the name and leave the extension unchanged</param>
        public static void BackupOldFiles(string fullFileName, uint countOldFilesToKeep, string optionalExtension = null, bool incrementNameNotExtension = false)
        {
            // Files are renamed only if the active file name exists.
            if (!File.Exists(fullFileName)) return;

            string extension = Path.GetExtension(fullFileName);

            if (string.IsNullOrWhiteSpace(optionalExtension))
                optionalExtension = System.String.Empty;
            else
            {
                if (optionalExtension[0] != '.' && optionalExtension[0] != '_' && !incrementNameNotExtension)
                    optionalExtension = "." + optionalExtension;
            }

            string newExtension = extension + optionalExtension;

            if (countOldFilesToKeep < 1) return;


            for (int i = (int)countOldFilesToKeep - 1; i >= 0; i--)
            {
                string oldFileName = System.String.Empty;
                string olderFileName;
                try
                {

                    if (incrementNameNotExtension)
                    {
                        if (i > 0)
                            oldFileName = fullFileName.Replace(extension, optionalExtension + "_" + i + extension);
                        else
                            oldFileName = fullFileName;
                        olderFileName = fullFileName.Replace(extension, optionalExtension + "_" + (i + 1) + extension);
                    }
                    else
                    {
                        if (i > 0)
                            oldFileName = fullFileName.Replace(extension, newExtension + "_" + i);
                        else
                            oldFileName = fullFileName;
                        olderFileName = fullFileName.Replace(extension, newExtension + "_" + (i + 1));
                    }

                    if (File.Exists(olderFileName))
                    {
                        File.Delete(olderFileName);
                    }
                    if (File.Exists(oldFileName))
                    {
                        if (IsFilePathWritable(oldFileName))
                        {
                            File.Move(oldFileName, olderFileName);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogExceptionMessage(LogPriority.Debug, exception, "Backup Old Log File failed: " + oldFileName);
                }
            }
        }

        /// <summary>
        /// Checks the specific path or current path and returns True if it is a reserved programme path including
        /// Visual Studio output paths and windows programme directories. If this is the case, the recommendation is
        /// to change the desired path to another, such as the Desktop or ProgramData.
        /// If the path does not exist, the parent path is checked. Thus the path need not exist.
        /// </summary>
        /// <remarks>
        /// Path or parent path must exist
        /// Reserved paths:
        ///     Visual Studio bin\debug, bin\release paths
        ///     ?\Windows*
        ///     ?\Program Files*
        ///     *SysWow64*
        ///     The programmes executable path
        /// </remarks>
        /// <param name="path">Optional path to check. Use the Current working directing if null.</param>
        /// <param name="disallowNetworkPath">If True a network path like \\RemoteComputer\FolderShare is considered reserved.</param>
        /// <returns>True if the path is considered a programme path, reserved path or read only.</returns>
        public static bool  IsPathReserved(string path = null, bool disallowNetworkPath = true)
        {
            //Log.Write("IsPathReserved: Requested path {0}", path);
            if (path == null)
            {
                path = MyStartupPath.ToLower();
            }
            while ( !Directory.Exists(path) )
            {
                // Check parent folder that exists. A folder below root must exist.
                path = Directory.GetParent(path).FullName;
                if (path == null) return true;
            }
            //
            // Get the parent path to handle VS paths like
            // ...\Application\bin\Debug\netcoreapp3.1
            //
            string parentPath = Directory.GetParent(path).FullName;
            if (parentPath == null)
                parentPath = path;
            parentPath = parentPath.ToLower();
            path = path.ToLower();
            string appPath = MyAssemblyPath;

            if (path.Contains("program files") || path.Contains("system32") || path.Contains("syswow64") || path.Contains("\\windows")
                || path.Contains("devenv") || path.EndsWith("bin") || path.EndsWith("app")
                || path.StartsWith(appPath) || !IsFilePathWritable(path)
                || (path.StartsWith("\\\\") && disallowNetworkPath )
                || path.EndsWith("debug") || path.EndsWith("release")
                || parentPath.EndsWith("debug") || parentPath.EndsWith("release")
                )
            {
                //Log.Write("IsPathReserved({0}): Path is reserved", path);
                return true;
            }
            //Log.Write("IsPathReserved({0}): Path is not reserved", path);
            return false;
        }

        /// <summary>
        /// Checks if the directory or file path is writeable. 
        /// All working paths for this application must be writeable by the application as the
        /// application avoids using the programme path (with the exception of initialising application defaults).
        /// </summary>
        /// <param name="directoryOrFilePath">The existing path to check.</param>
        /// <returns>True if path is writable.</returns>
        public static bool IsFilePathWritable(string directoryOrFilePath)
        {
            if (directoryOrFilePath == null)
                return false;
            if (directoryOrFilePath.Length == 0)
                return false;
#if NETFRAMEWORK
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);

            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, directoryOrFilePath);
            FileIOPermission appendPermission = new FileIOPermission(FileIOPermissionAccess.Append, directoryOrFilePath);

            permissionSet.AddPermission(writePermission);     // write, replace, delete access
            permissionSet.AddPermission(appendPermission);    // 'Twas not 100% sure if append is needed to create files, so it is included.

            if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
            {
                return true;
            }
            return false;

#elif NETSTANDARD
            DirectoryInfo dInfo = new DirectoryInfo(directoryOrFilePath);
            // TODO How to handle this?
            //DirectorySecurity dSecurity = dInfo.GetAccessControl();
            return true;
#else
            return true;
#endif
        }
        #endregion Methods

    }
}
