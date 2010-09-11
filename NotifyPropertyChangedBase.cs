using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace ProverbTeleprompter
{
    
	public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
	{
        
		public event PropertyChangedEventHandler PropertyChanged;

		#region Property Change Helpers

		/// <summary>
		/// Call when a property has been changed
		/// </summary>
		/// <param name="property">THe name of the property that has changed</param>
		protected virtual void Changed(string property)
		{

			VerifyPropertyName(property);

			if (PropertyChanged != null)
			{
				foreach (var propertyName in AllNotifiedProperties(property))
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}


		/// <summary>
		/// Allows for strongly typed property change events (instead of hardcoding strings)
		/// </summary>
		/// <typeparam name="TObject">The type of object the property belongs</typeparam>
		/// <param name="propertySelector">an expression specifying the property (i.e. </param>
		public void Changed<TObject>(Expression<Func<TObject>> propertySelector)
		{
			if (PropertyChanged != null)
			{
				var memberExpression = propertySelector.Body as MemberExpression;

				if (memberExpression != null)
				{
					Changed(memberExpression.Member.Name);
				}
			}
		}


		private IEnumerable<string> DependantProperties(string inputName)
		{
			return from property in GetType().GetProperties()
				   where property.GetCustomAttributes(typeof(DependsUponAttribute), true).Cast<DependsUponAttribute>()
						 .Any(attribute => attribute.DependancyName == inputName)
				   select property.Name;
		}

		private IEnumerable<string> NotifiedProperties(IEnumerable<string> inputs)
		{
			var dependancies = from input in inputs
							   from dependancy in DependantProperties(input)
							   select dependancy;

			return inputs.Union(dependancies).Distinct();
		}

		private IEnumerable<string> AllNotifiedProperties(string inputName)
		{
			IEnumerable<string> results = new[] { inputName };

			while (NotifiedProperties(results).Count() > results.Count())
				results = NotifiedProperties(results);

			return results;
		}



		#region Debugging Aides

		/// <summary>
		/// Warns the developer if this object does not have
		/// a public property with the specified name. This 
		/// method does not exist in a Release build.
		/// </summary>
		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		public void VerifyPropertyName(string propertyName)
		{
			// Verify that the property name matches a real,  
			// public, instance property on this object.
			if (TypeDescriptor.GetProperties(this)[propertyName] == null)
			{
				string msg = "Invalid property name: " + propertyName;

				if (ThrowOnInvalidPropertyName)
					throw new Exception(msg);

				Debug.Fail(msg);
			}
		}

		/// <summary>
		/// Returns whether an exception is thrown, or if a Debug.Fail() is used
		/// when an invalid property name is passed to the VerifyPropertyName method.
		/// The default value is false, but subclasses used by unit tests might 
		/// override this property's getter to return true.
		/// </summary>
		protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

		#endregion // Debugging Aides



		#endregion PropertyChanged helpers
	}
}
