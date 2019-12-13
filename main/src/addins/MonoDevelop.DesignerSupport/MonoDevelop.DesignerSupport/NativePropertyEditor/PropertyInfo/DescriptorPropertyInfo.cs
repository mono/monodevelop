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
using System.Collections;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	class TypeDescriptorContext : ITypeDescriptorContext
	{
		public TypeDescriptorContext (object propertyProvider, PropertyDescriptor propertyDescriptor)
		{
			Instance = propertyProvider;
			PropertyDescriptor = propertyDescriptor;
		}

		public IContainer Container => null;

		public object Instance { get; }

		public PropertyDescriptor PropertyDescriptor { get; private set; }

		public object GetService (Type serviceType)
		{
			return null;
		}

		public void OnComponentChanged ()
		{
			//TODO ITypeDescriptorContext.OnComponentChanging
		}

		public bool OnComponentChanging ()
		{
			//TODO ITypeDescriptorContext.OnComponentChanging
			return true;
		}
	}

	class DescriptorPropertyInfo
		: IPropertyInfo, IEquatable<DescriptorPropertyInfo>
	{
		public object PropertyProvider => typeDescriptorContext.Instance;
		protected readonly TypeDescriptorContext typeDescriptorContext;

		public PropertyDescriptor PropertyDescriptor => typeDescriptorContext.PropertyDescriptor;

		static readonly IAvailabilityConstraint [] EmptyConstraints = Array.Empty<IAvailabilityConstraint> ();
		static readonly PropertyVariationOption [] EmptyVariationOptions = Array.Empty<PropertyVariationOption> ();

		public DescriptorPropertyInfo (TypeDescriptorContext typeDescriptorContext, ValueSources valueSources) 
		{
			this.typeDescriptorContext = typeDescriptorContext;
			this.ValueSources = valueSources;
		}

		public string Name => PropertyDescriptor.DisplayName;

		public string Description => PropertyDescriptor.Description;

		public virtual Type Type => PropertyDescriptor.PropertyType;

		public ITypeInfo RealType => ToTypeInfo (Type);

		public string Category => PropertyDescriptor.Category;

		public bool CanWrite => !PropertyDescriptor.IsReadOnly;

		public ValueSources ValueSources { get; }

		public IReadOnlyList<PropertyVariationOption> Variations => EmptyVariationOptions;

		public IReadOnlyList<IAvailabilityConstraint> AvailabilityConstraints => EmptyConstraints;

		public bool IsUncommon => false;

		public bool Equals (DescriptorPropertyInfo other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals (this, other))
				return true;

			return PropertyDescriptor.Equals (other.PropertyDescriptor);
		}

		public override bool Equals (object obj)
		{
			return obj is DescriptorPropertyInfo info && Equals (info);
		}

		public override int GetHashCode ()
		{
			return PropertyDescriptor.GetHashCode ();
		}

		public static Type GetCollectionItemType (Type colType)
		{
			foreach (var member in colType.GetDefaultMembers ()) {
				var prop = member as PropertyInfo;
				if (prop != null && prop.Name == "Item" && prop.PropertyType != typeof (object))
					return prop.PropertyType;
			}
			return null;
		}

		public static ITypeInfo ToTypeInfo (Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new Xamarin.PropertyEditing.TypeInfo (new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}

		internal virtual T GetValue<T> (object target)
		{
			var converted = default (T);
			object value = null;
			bool canConvert = false;

			try {

				value = PropertyDescriptor.GetValue (PropertyProvider);
				var currentType = typeof (T);
				var underlyingType = Nullable.GetUnderlyingType (currentType);

				//proppy has some editors based in nullable types
				if (underlyingType != null) {
					var converterNullable = new NullableConverter (currentType);
					if (converterNullable.CanConvertFrom (underlyingType)) {
						object notNullableValue = Convert.ChangeType (value, underlyingType);
						return (T)converterNullable.ConvertFrom (notNullableValue);
					}
				}

				var tc = PropertyDescriptor.Converter;
				canConvert = tc.CanConvertTo (currentType);
				if (canConvert) {
					converted = (T)tc.ConvertTo (value, currentType);
				} else {
					converted = (T)Convert.ChangeType (value, currentType);
				}
			
			} catch (Exception ex) {
				LogInternalError ($"Error trying to get and convert value:'{value}' canconvert: {canConvert} T:{typeof (T).FullName} ", ex);
			}
			return converted;
		}

		internal virtual void SetValue<T> (object target, T value)
		{
			try {
				
				var currentType = typeof (T);

				//TODO: Proppy in Boolean types uses bool? to handle it, but this will fail using converters
				//thats because we need ensure take the underlying type
				currentType = Nullable.GetUnderlyingType (currentType) ?? currentType;

				object notNullableValue;
				var tc = PropertyDescriptor.Converter;
				if (tc.CanConvertFrom (currentType)) {
					notNullableValue = tc.ConvertFrom (value);
				} else {
					notNullableValue = Convert.ChangeType (value, currentType);
				}

				PropertyDescriptor.SetValue (PropertyProvider, notNullableValue);

			} catch (Exception ex) {
				LogInternalError ($"Error trying to set and convert a value: {value} T:{typeof (T).FullName}", ex);
			}
		}

		protected void LogInternalError (string message, Exception ex = null) =>
			LoggingService.LogInternalError (string.Format ("[{0}][{1}] {2}", Name, GetType ().Name, message), ex);

	}
}

#endif