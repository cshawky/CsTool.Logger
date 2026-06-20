namespace CsTool.Logger.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using CsTool.Logger;

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
