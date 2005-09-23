using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Reflection;

using MonoDevelop.Services;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui.Components
{
	/// <summary>
	/// LocalizedPropertyDescriptor enhances the base class bay obtaining the display name for a property
	/// from the resource.
	/// </summary>
	public class LocalizedPropertyDescriptor : PropertyDescriptor
	{
		static StringParserService stringParserService = Runtime.StringParserService;
		
		PropertyDescriptor basePropertyDescriptor; 
		
		string localizedName        = String.Empty;
		string localizedDescription = String.Empty;
		string localizedCategory    = String.Empty;
		
		public override bool IsReadOnly {
			get {
				return this.basePropertyDescriptor.IsReadOnly;
			}
		}

		public override string Name {
			get {
				return this.basePropertyDescriptor.Name;
			}
		}

		public override Type PropertyType {
			get {
				return this.basePropertyDescriptor.PropertyType;
			}
		}
		
		public override Type ComponentType {
			get {
				return basePropertyDescriptor.ComponentType;
			}
		}
		
		public override string DisplayName {
			get  {
				return stringParserService.Parse(localizedName);
			}
		}
		
		public override string Description {
			get {
				return stringParserService.Parse(localizedDescription);
			}
		}
		
		public override string Category {
			get {
				return stringParserService.Parse(localizedCategory);
			}
		}
		
		public LocalizedPropertyDescriptor(PropertyDescriptor basePropertyDescriptor) : base(basePropertyDescriptor)
		{
			LocalizedPropertyAttribute localizedPropertyAttribute = null;
			
			foreach (Attribute attr in basePropertyDescriptor.Attributes) {
				localizedPropertyAttribute = attr as LocalizedPropertyAttribute;
				if (localizedPropertyAttribute != null) {
					break;
				}
			}
			
			if (localizedPropertyAttribute != null) {
				localizedName        = localizedPropertyAttribute.Name;
				localizedDescription = localizedPropertyAttribute.Description;
				localizedCategory    = localizedPropertyAttribute.Category;
			} else {
				localizedName        = basePropertyDescriptor.Name;
				localizedDescription = basePropertyDescriptor.Description;
				localizedCategory    = basePropertyDescriptor.Category;
			}
			
			this.basePropertyDescriptor = basePropertyDescriptor;
		}

		public override bool CanResetValue(object component)
		{
			return basePropertyDescriptor.CanResetValue(component);
		}
		
		public override object GetValue(object component)
		{
			return this.basePropertyDescriptor.GetValue(component);
		}
		public override void ResetValue(object component)
		{
			this.basePropertyDescriptor.ResetValue(component);
		}

		public override bool ShouldSerializeValue(object component)
		{
			return this.basePropertyDescriptor.ShouldSerializeValue(component);
		}

		public override void SetValue(object component, object value)
		{
			this.basePropertyDescriptor.SetValue(component, value);
		}
	}
}
