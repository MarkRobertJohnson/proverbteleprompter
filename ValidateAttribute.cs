using System;

namespace Eid.Core.Mvvm
{
    /// <summary>
    /// Use this attribute on model properties exposed by the ViewModel 
    /// that need to be validated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ValidateAttribute : Attribute
    {
        public bool RequiresValidation { get; private set; }

        public ValidateAttribute()
        {
            RequiresValidation = true;
        }
    }
}