using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace MonoDevelop.Gui.Components
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
	public class LocalizedPropertyAttribute : Attribute
	{
		string name        = String.Empty;
		string description = String.Empty;
		string category    = String.Empty;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		public string Category {
			get {
				return category;
			}
			set {
				category = value;
			}
		}
		
		public LocalizedPropertyAttribute(string name)
		{
			this.name = name;
		}
		
	}
}
