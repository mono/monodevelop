//
// DescriptorPropertyInfo.cs
//
// Author:
//       jmedrano <josmed@microsoft.com>
//
// Copyright (c) 2018 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if MAC

using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Reflection;
using System.Reflection;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport
{
	class PropertyTypeInfo : Xamarin.PropertyEditing.TypeInfo
	{
		public CustomDescriptor customDescriptor;
		public PropertyTypeInfo (CustomDescriptor propertyDescriptor, IAssemblyInfo assembly, string nameSpace, string name) : base (assembly, nameSpace, name)
		{
			this.customDescriptor = propertyDescriptor;
		}
	}

	class DescriptorPropertyInfo
		: IPropertyInfo, IEquatable<DescriptorPropertyInfo>
	{
		readonly PropertyDescriptor propertyInfo;

		static readonly IAvailabilityConstraint [] EmptyConstraints = new IAvailabilityConstraint [0];
		static readonly PropertyVariationOption [] EmptyVariationOptions = new PropertyVariationOption [0];
		CustomDescriptor customDescriptor;
		public DescriptorPropertyInfo (PropertyDescriptor propertyInfo, CustomDescriptor customDescriptor) 
		{
			this.propertyInfo = propertyInfo;
			this.customDescriptor = customDescriptor;
		}

		public string Name => propertyInfo.DisplayName;

		public string Description => propertyInfo.Description;

		public Type Type => propertyInfo.PropertyType;

		public ITypeInfo RealType => ToTypeInfo (customDescriptor, Type);

		public string Category => propertyInfo.Category;

		public bool CanWrite => !propertyInfo.IsReadOnly;

		public ValueSources ValueSources => ValueSources.Local;

		public IReadOnlyList<PropertyVariationOption> Variations => EmptyVariationOptions;

		public IReadOnlyList<IAvailabilityConstraint> AvailabilityConstraints => EmptyConstraints;

		public bool Equals (DescriptorPropertyInfo other)
		{
			if (ReferenceEquals (null, other))
				return false;
			if (ReferenceEquals (this, other))
				return true;

			return this.propertyInfo.Equals (other.propertyInfo);
		}

		public override bool Equals (object obj)
		{
			if (ReferenceEquals (null, obj))
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != this.GetType ())
				return false;

			return Equals ((DescriptorPropertyInfo)obj);
		}

		public override int GetHashCode ()
		{
			return this.propertyInfo.GetHashCode ();
		}

		public static ITypeInfo ToTypeInfo (CustomDescriptor customDescriptor, Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new PropertyTypeInfo (customDescriptor, new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}

		private readonly Lazy<List<TypeConverter>> typeConverter;

		internal Task<T> GetValueAsync<T> (object target)
		{
			string error;
			object value = null;
			try {
				TypeConverter tc = propertyInfo.Converter;
				object cob = propertyInfo.GetValue (customDescriptor);
				if (tc.CanConvertTo (typeof (T))) {
					value = tc.ConvertTo (cob, typeof (T));
				}
				return Task.FromResult ((T)value);
			} catch (Exception ex) {
				error = ex.ToString (); 
			}

			T converted = default (T);
			try {
				if (value != null && !(value is T)) {
					if (typeof (T) == typeof (string)) {
						value = value.ToString ();
					} else {
						value = Convert.ChangeType (value, typeof (T));
					}
				}
				return Task.FromResult ((T)value);

			} catch (Exception ex) {
				error = ex.ToString ();
				Console.WriteLine (error);
				return Task.FromResult (converted);
			}
		}
	}
}

#endif