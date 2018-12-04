//
// MockEditorProvider.cs
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
	public class DescriptorPropertyInfo
		: IPropertyInfo, IEquatable<DescriptorPropertyInfo>
	{
		private readonly PropertyDescriptor propertyInfo;

		private static readonly IAvailabilityConstraint [] EmptyConstraints = new IAvailabilityConstraint [0];
		private static readonly PropertyVariationOption [] EmtpyVariationOptions = new PropertyVariationOption [0];

		public DescriptorPropertyInfo (PropertyDescriptor propertyInfo) 
		{
			this.propertyInfo = propertyInfo;
		}

		public string Name => propertyInfo.Name;

		public string Description => propertyInfo.DisplayName;

		public Type Type => propertyInfo.PropertyType;

		public ITypeInfo RealType => ToTypeInfo (Type);

		public string Category => propertyInfo.Category;

		public bool CanWrite => !propertyInfo.IsReadOnly;

		public ValueSources ValueSources => ValueSources.Local;

		public IReadOnlyList<PropertyVariationOption> Variations => EmtpyVariationOptions;

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

		public static ITypeInfo ToTypeInfo (Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new Xamarin.PropertyEditing.TypeInfo (new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}
	}
}

#endif