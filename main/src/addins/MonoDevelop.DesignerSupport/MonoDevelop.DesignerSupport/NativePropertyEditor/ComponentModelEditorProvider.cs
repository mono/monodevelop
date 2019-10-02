//
// PropertyPadEditorProvider.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	class ComponentModelEditorProvider
	: IEditorProvider
	{
		public IReadOnlyDictionary<Type, ITypeInfo> KnownTypes {
			get;
		} = new Dictionary<Type, ITypeInfo> ();

		object [] providers;

		public Task<IObjectEditor> GetObjectEditorAsync (object item)
		{
			if (item is ComponentModelTarget propertyPadItem) {
				//on each editor we store current used providers
				providers = propertyPadItem.Providers ?? Array.Empty<object> ();
				return Task.FromResult<IObjectEditor> (new ComponentModelObjectEditor (propertyPadItem));
			}
			throw new ArgumentException (string.Format ("Cannot get editor for type {0} only ComponentModelObjectEditor types are allowed", item.GetType ().Name));
		}

		public Task<object> CreateObjectAsync (ITypeInfo type)
		{
			var realType = Type.GetType ($"{type.NameSpace}.{type.Name}, {type.Assembly.Name}");
			if (realType == null)
				return Task.FromResult<object> (null);
			return Task.FromResult (Activator.CreateInstance (realType));
		}

		public Task<IReadOnlyCollection<IPropertyInfo>> GetPropertiesForTypeAsync (ITypeInfo type)
			=> Task.Run (() => (IReadOnlyCollection<IPropertyInfo>)GetPropertiesForProviders (providers));

		public static IReadOnlyList<DescriptorPropertyInfo> GetPropertiesForProviders (object[] providers)
		{
			var collection = new List<DescriptorPropertyInfo> ();

			foreach (object propertyProvider in providers) {
				//get the current properties for this provider
				var currentType = propertyProvider.GetType ();

				//we want all property descriptors for this propertyProvider type
				var propertyDescriptors = System.ComponentModel.TypeDescriptor.GetProperties (currentType);

				foreach (System.ComponentModel.PropertyDescriptor propertyDescriptor in propertyDescriptors) {
					if (propertyDescriptor.IsBrowsable) {
						collection.Add (CreatePropertyInfo (propertyDescriptor, propertyProvider));
					}
				}
			}
			return collection.AsReadOnly ();
		}

		protected static DescriptorPropertyInfo CreatePropertyInfo (System.ComponentModel.PropertyDescriptor propertyDescriptor, object PropertyProvider)
		{
			var valueSources = ValueSources.Local | ValueSources.Default;
			if (propertyDescriptor.PropertyType.IsEnum) {
				if (propertyDescriptor.PropertyType.IsDefined (typeof (FlagsAttribute), true))
					return new FlagDescriptorPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
				return new EnumDescriptorPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			if (propertyDescriptor.PropertyType.IsAssignableFrom (typeof (Core.FilePath))) {
				//var isDirectoryPropertyDescriptor = GetPropertyDescriptor (propertyDescriptor.GetChildProperties (), nameof (Core.FilePath.IsDirectory));
				//if (isDirectoryPropertyDescriptor != null && (bool)isDirectoryPropertyDescriptor.GetValue (propertyItem)) {
				//	return new DirectoryPathPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
				//}
				return new FilePathPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			if (HasStandardValues (propertyDescriptor)) {
				return new StringStandardValuesPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			return new DescriptorPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
		}

		static bool HasStandardValues (System.ComponentModel.PropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.Converter.GetStandardValuesSupported ()) {
				if (!propertyDescriptor.Converter.GetStandardValuesExclusive () && propertyDescriptor.Converter.CanConvertFrom (typeof (string))) {
					return true;
				}
			}
			return false;
		}

		public ITypeInfo GetRealType<T> (T item)
			=>	DescriptorPropertyInfo.ToTypeInfo(item.GetType ());

		public static Type GetRealType (ITypeInfo type)
			=> Type.GetType ($"{type.NameSpace}.{type.Name}, {type.Assembly.Name}");

		public Task<AssignableTypesResult> GetAssignableTypesAsync (ITypeInfo type, bool childTypes)
			=> ComponentModelObjectEditor.GetAssignableTypes ();

		public Task<IReadOnlyList<object>> GetChildrenAsync (object item) 
			=>  Task.FromResult<IReadOnlyList<object>> (Array.Empty<object> ());
	}
}

#endif