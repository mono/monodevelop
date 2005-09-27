// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Core.Properties
{
	public delegate void PropertyEventHandler(object sender, PropertyEventArgs e);
	
	public class PropertyEventArgs : EventArgs
	{
		IProperties properties;
		string      key;
		object      newValue;
		object      oldValue;
		
		/// <returns>
		/// returns the changed property object
		/// </returns>
		public IProperties Properties {
			get {
				return properties;
			}
		}
		
		/// <returns>
		/// The key of the changed property
		/// </returns>
		public string Key {
			get {
				return key;
			}
		}
		
		/// <returns>
		/// The new value of the property
		/// </returns>
		public object NewValue {
			get {
				return newValue;
			}
		}
		
		/// <returns>
		/// The new value of the property
		/// </returns>
		public object OldValue {
			get {
				return oldValue;
			}
		}
		
		public PropertyEventArgs(IProperties properties, string key, object oldValue, object newValue)
		{
			this.properties = properties;
			this.key        = key;
			this.oldValue   = oldValue;
			this.newValue   = newValue;
		}
	}
}
