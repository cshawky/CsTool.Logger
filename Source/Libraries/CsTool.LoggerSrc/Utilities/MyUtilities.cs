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
    using System.Xml.Linq;
    using ExtensionMethods;

    public static class MyUtilities
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Environment Variables Substitution

        /// <summary>
        /// Fake environment variable to represent the current directory. Supported by CsTool.CoreUtilities.Utilities.ExpandEnvironmentVariables()
        /// and InsertEnvironmentVariables(). CsTool.Logger needs the same methods but cannot call CoreUtilities. Therefore duplicated here.
        /// </summary>
        public const string CurrentDirectoryEnvVar = "%STARTUPDIR%";
        public const string AppNameEnvVar = "%APPNAME%";
        public const string ExecutablePathEnvVar = "%ExecutablePath%";
        public const string DropboxEnvVar = "%Dropbox%";
        public static string DropboxPath = GetDropboxPath();

        /// <summary>
        /// Environment variables that can be substituted in file paths in priority order
        /// </summary>
        public static string[] SupportedEnvironmentVariables = 
                new string[] {
                    "%TEMP%", "%TMP%", 
                    "%PUBLIC%",
                    ExecutablePathEnvVar,
                    CurrentDirectoryEnvVar,
                    "%ProgramData%", "%ALLUSERSPROFILE%", 
                    "%ProgramFiles(x86)%", "%ProgramFiles%",
                    "%OneDriveCommercial%", "%OneDriveConsumer%", "%OneDrive%", DropboxEnvVar,
                    "%APPDATA%", "%LOCALAPPDATA%",
                    "%USERPROFILE%",
                    AppNameEnvVar,
                    "%USERNAME%",
                    "%COMPUTERNAME%",
                };

        /// <summary>
        /// Replace all environment variables including the internal variable substitutions
        /// with their current values.
        /// The returned path is ideally suited for use with file methods.
        /// </summary>
        /// <param name="compactedPath">The file path with environment variables</param>
        /// <returns>The file path with all environment variables replaced with current values</returns>
        public static string ExpandEnvironmentVariables(string compactedPath)
        {
            string expandedPath = Environment.ExpandEnvironmentVariables(compactedPath);
            expandedPath = expandedPath
                            .Replace(ExecutablePathEnvVar,LogBase.AppDefaultsSystemFilePath)
                            .Replace(CurrentDirectoryEnvVar, Environment.CurrentDirectory)
                            .Replace(DropboxEnvVar, DropboxPath)
                            .Replace(AppNameEnvVar, LogUtilities.MyProcessName)
                            ;

            //Logger.Write("ExpandEnvironmentVariables:\n   {0}\n-> {1}", compactedPath, expandedPath);
            return expandedPath;
        }

        /// <summary>
        /// Insert environment variable names and special internal variables in place of their respective path text.
        /// Please note that this method is case sensitive.
        /// This method is the reverse of MyUtilities.ExpandEnvironmentVariables().
        /// </summary>
        /// <remarks>When manipulating file names and paths, use an internal variable and get/set methods.
        /// The get method would then call MyUtilities.ExpandEnvironmentVariables(sourcePath) whilst
        /// the set method would then call sourcePath = MyUtilities.InsertEnvironmentVariables(newPath) to
        /// store the new path with the environment variables intact.</remarks>
        /// <param name="expandedPath">The expanded file path such as <code>C:\Users\Chris\Desktop\Blah</code></param>
        /// <returns>The path with environment variables inserted such as <code>%USERPROFILE\Desktop\Blah</code></returns>
        public static string InsertEnvironmentVariables(string expandedPath)
        {
            string compactedPath = expandedPath;
            if (string.IsNullOrEmpty(expandedPath)) return expandedPath;

            foreach (string variable in SupportedEnvironmentVariables)
            {
                // Custom variables
                if (variable == CurrentDirectoryEnvVar)
                {
                    compactedPath = compactedPath.ReplaceIgnoreCase(Environment.CurrentDirectory, CurrentDirectoryEnvVar);
                    continue;
                }
                if (variable == AppNameEnvVar)
                {
                    compactedPath = compactedPath.ReplaceIgnoreCase(LogUtilities.MyProcessName, variable);
                    continue;
                }
                if (variable == DropboxEnvVar)
                {
                    compactedPath = compactedPath.ReplaceIgnoreCase(DropboxPath, variable);
                    continue;
                }
                // Replace the destination path as it applies to an environment variable with the individual variable
                string expandedVariable = Environment.ExpandEnvironmentVariables(variable);
                compactedPath = compactedPath.Replace(expandedVariable, variable);
            }
            //Logger.Write("InsertEnvironmentVariables:\n   {0}\n-> {1}", expandedPath, compactedPath);

            return compactedPath;
        }

        /// <summary>
        /// Retrieve the Dropbox path from the info.json file.
        /// </summary>
        /// <returns>The path</returns>
        /// <exception cref="Exception"></exception>
        /// <remarks>
        /// Avoid using Newtonsoft.Json
        /// https://stackoverflow.com/questions/9660280/how-do-i-programmatically-locate-my-dropbox-folder-using-c
        /// </remarks>
        public static string GetDropboxPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] possiblePaths = {
                Path.Combine(appDataPath, "Dropbox", "info.json"),
                Path.Combine(localAppDataPath, "Dropbox", "info.json")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    string dropBoxPath = File.ReadAllText(path).Split('\"')[5].Replace(@"\\", @"\");

                    return dropBoxPath;
                }
            }
            return String.Empty;
        }
        #endregion Environment Variables Substitution

        #region File Utilities

        /// <summary>
        /// Check if the given folder path is writeable.
        /// </summary>
        /// <param name="path">The folder path excluding file name</param>
        /// <returns>True if the path is writeable</returns>
        public static bool IsPathWriteable(string path)
        {
            try
            {
                // Combine the path with a temporary file name
                string tempFilePath = Path.Combine(path, Path.GetRandomFileName());

                // Create and delete the temporary file
                using (FileStream fs = File.Create(tempFilePath, 1, FileOptions.DeleteOnClose))
                {
                    // If we reach here, the path is writeable
                    return true;
                }
            }
            catch
            {
                // If an exception is thrown, the path is not writeable
                return false;
            }
        }
        #endregion File Utilities
    }
}
