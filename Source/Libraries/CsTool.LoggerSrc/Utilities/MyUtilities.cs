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
        private const string CurrentDirectoryEnvVar = "%STARTUPDIR%";

        /// <summary>
        /// Environment variables that can be substituted in file paths.
        /// </summary>
        public static string[] SupportedEnvironmentVariables = new string[] { "%TEMP%", "%TMP%", "%LOCALAPPDATA%", "%OneDrive%", "%USERPROFILE%",
                                                                            "%APPDATA%", "%PUBLIC%",
                                                                            "%ProgramData%", "%ALLUSERSPROFILE%", "%ProgramFiles(x86)%", "%ProgramFiles%",
                                                                            "%STARTUPDIR%", "%USERNAME%", "%COMPUTERNAME%",
                                                                            "%APPNAME%"};

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
            if (expandedPath.IsNullOrEmpty()) return expandedPath;

            foreach (string variable in SupportedEnvironmentVariables)
            {
                string expandedVariable;
                if (variable == CurrentDirectoryEnvVar)
                {
                    expandedVariable = Environment.CurrentDirectory;
                    compactedPath = compactedPath.Replace(Environment.CurrentDirectory, CurrentDirectoryEnvVar);
                    continue;
                }
                if (variable == "%APPNAME%")
                {
                    expandedVariable = LogUtilities.MyProcessName;
                    compactedPath = compactedPath.Replace(expandedVariable, variable);
                    continue;
                }

                // Replace the destination path as it applies to an environment variable with the individual variable
                expandedVariable = Environment.ExpandEnvironmentVariables(variable);
                compactedPath = compactedPath.Replace(expandedVariable, variable);
            }
            return compactedPath;
        }

        #endregion Environment Variables Substitution
    }
}
