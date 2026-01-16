namespace CsTool.Logger
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// <code>ModelSettingsClassAttribute</code> attribute is assigned to a class that contains one or more properties
    /// that will be read and written from a ModelSettings xml file.
    /// </summary>
    /// <remarks>
    /// Originally all properties to be stored and retrieved from a configuration file were collected in a single class 
    /// per DLL or application whereby that class inherited CommonSettings and DefaultSettings to streamline loading and
    /// saving of the properties. The concept is now expanded to abstract the process through the use of attributes and
    /// reflection. With the presence of attributes System.Reflection may be harnessed to automatically determine which
    /// properties need to be saved and loaded.
    /// Since the ModelSettings class may contain properties that are themselves classses there is a little more to it.
    /// Hence we have the following attributes:
    ///     ModelSettingsClass      This attribute identifies the class as an export/import property. it is applied at the
    ///                             class definition level for any class that might be used as a settings property to another
    ///                             class.
    ///                             All class instances are represented by a single XML Element within the xml file under the
    ///                             Root Element. The Root Element defines the application or DLL associated with the settings.
    ///     
    ///     ModelSettingsInstance   This attribute is assigned to an instance of a class that should be treated as a setting.
    ///                             Properties with this attribute assigned generate an XML Element then each settings property
    ///                             within this instance are then saved or retrieve from the XML file.
    ///                             
    ///     ModelSettingsProperty   This attribute is assigned to a property that should be saved and loaded. The property
    ///                             may also be a class instance (String is a class) hence why there is a need to use a specific
    ///                             attribute to distinguish between a literal property versus a group of properties.
    ///                             
    /// Please refer to the class <code>SampleModelSettings</code> for an example of how to use these attributes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelSettingsClassAttribute : Attribute
    {
    }

    /// <summary>
    /// The <code>ModelSettingsInstance</code> attribute is assigned to an instance of a class that contains properties
    /// for export/import to/from a ModelSettings xml file.
    /// Typical examples of this property would be a custom class like <code>SampleModelSettings</code> that contains
    /// multiple ModelSettingsProperty tagged properties. Each instance of <code>SampleModelSettings</code> would be
    /// tagged with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelSettingsInstanceAttribute : Attribute
    {
    }

    /// <summary>
    /// The <code>ModelSettingsProperty</code> attribute is assigned to a property that will be saved and loaded from the
    /// ModelSettings xml file directly. Typical examples of this property would be String, List<String>, int, List<int>, DateTime,
    /// Boolean and enumerated types. Most notably LogPriority.
    /// Arrays are not supported directly, please use List<T>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelSettingsPropertyAttribute : Attribute
    {
        public ModelSettingsPropertyAttribute()
        {
        }
    }

    /// <summary>
    /// Then the <code>ModelSettingsVariableSubstitution</code> attribute is assigned to a property variable substitution will be
    /// performed on load and save.
    /// Use this attribute instead of <code>ModelSettingsProperty</code>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelSettingsPropertyWithSubstitutionsAttribute : Attribute
    {
        public ModelSettingsPropertyWithSubstitutionsAttribute()
        {
        }
    }

    /// <summary>
    /// TESTING: Trying the use of attributes to identify the order of columns in a data table.
    /// Apply this attribute to any public property in a class to provide a display order for the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ViewPropertyColumnAttribute : Attribute
    {
        private int index;
        public ViewPropertyColumnAttribute(int index = 0)
        {
            this.index = index;
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }
    }

    /// <summary>
    /// ModelPropertyColumn identifies the property is being imported and exported between internal classes 
    /// and a data table, typically related to import/export to Excel using ClosedXml Tables or other file or database formats.
    /// In addition the Index property provides the same functionality as the ColumnOrder attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelPropertyColumnAttribute : ViewPropertyColumnAttribute
    {
        public ModelPropertyColumnAttribute(int index = 0) : base(index)
        {
        }
    }
}
