using System;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Including this attribute on a property will force the propertychanged event to be raised.
    /// NOTE: for all public databound properties, include this attribute with "ModelObject" as the dependency.
    /// By doing that, the UI will be reset the first time a new workflow is executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DependsUponAttribute : Attribute
    {
        public string DependancyName { get; private set; }

        public DependsUponAttribute(string propertyName)
        {
            DependancyName = propertyName;
        }
    }
}