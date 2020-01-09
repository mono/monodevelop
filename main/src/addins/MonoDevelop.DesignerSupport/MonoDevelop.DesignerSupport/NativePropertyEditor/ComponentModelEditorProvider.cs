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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.PropertyEditing;

namespace MonoDevelop.DesignerSupport
{
	class ComponentModelEditorProvider
	: IEditorProvider, IDisposable
	{
		public event EventHandler<EventArgs> PropertyChanged;

		public IReadOnlyDictionary<Type, ITypeInfo> KnownTypes {
			get;
		} = new Dictionary<Type, ITypeInfo> ();

		ComponentModelTarget target;
		ComponentModelObjectEditor currentEditor;

		Task<IObjectEditor> IEditorProvider.GetObjectEditorAsync(object item)
		{
			target = item as ComponentModelTarget;
			if (target == null) {
				throw new ArgumentException (string.Format ("Cannot get editor for type {0} only ComponentModelObjectEditor types are allowed", item.GetType ().Name));
			}

			if (currentEditor != null) {
				currentEditor.PropertyChanged -= CurrentEditor_PropertyChanged;
			}

			currentEditor = new ComponentModelObjectEditor (target);
			currentEditor.PropertyChanged += CurrentEditor_PropertyChanged;	

			//on each editor we store current used providers
			return Task.FromResult<IObjectEditor>(currentEditor);
		}

		public void Clear ()
		{
			if (currentEditor != null) {
				currentEditor.Dispose ();
				currentEditor = null;
			}

			target = null;
		}

		void CurrentEditor_PropertyChanged (object sender, EventArgs e)
			=> PropertyChanged?.Invoke (this, e);

		public Task<object> CreateObjectAsync (ITypeInfo type)
		{
			var realType = GetRealType (type);
			if (realType == null)
				return Task.FromResult<object> (null);
			return Task.FromResult (Activator.CreateInstance (realType));
		}

		public Task<IReadOnlyCollection<IPropertyInfo>> GetPropertiesForTypeAsync (ITypeInfo type)
		{
			var prov = target?.Providers ?? Array.Empty<object> ();
			var providers = (IReadOnlyCollection<IPropertyInfo>)GetPropertiesForProviders (prov);
			return Task.FromResult (providers);
		}

		public static IReadOnlyList<DescriptorPropertyInfo> GetPropertiesForProviders (object[] providers)
		{
			var collection = new List<DescriptorPropertyInfo> ();

			foreach (object propertyProvider in providers) {
				//we want all property descriptors for this propertyProvider
				var propertyDescriptors = System.ComponentModel.TypeDescriptor.GetProperties (propertyProvider);

				foreach (System.ComponentModel.PropertyDescriptor propertyDescriptor in propertyDescriptors) {
					if (propertyDescriptor.IsBrowsable) {
						var propertyInfo = CreatePropertyInfo (propertyDescriptor, propertyProvider);
						collection.Add (propertyInfo);
					}
				}
			}
			return collection.AsReadOnly ();
		}

		protected static DescriptorPropertyInfo CreatePropertyInfo (System.ComponentModel.PropertyDescriptor propertyDescriptor, object propertyProvider)
		{
			var typeDescriptorContext = new TypeDescriptorContext (propertyProvider, propertyDescriptor);

			var valueSources = ValueSources.Default;
			if (propertyDescriptor.PropertyType.IsEnum) {
				if (propertyDescriptor.PropertyType.IsDefined (typeof (FlagsAttribute), inherit: false))
					return new FlagDescriptorPropertyInfo (typeDescriptorContext, valueSources);
				return new EnumDescriptorPropertyInfo (typeDescriptorContext, valueSources);
			}

			if (propertyDescriptor.PropertyType.IsAssignableFrom (typeof (Core.FilePath))) {
				//var isDirectoryPropertyDescriptor = GetPropertyDescriptor (propertyDescriptor.GetChildProperties (), nameof (Core.FilePath.IsDirectory));
				//if (isDirectoryPropertyDescriptor != null && (bool)isDirectoryPropertyDescriptor.GetValue (propertyItem)) {
				//	return new DirectoryPathPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
				//}
				return new FilePathPropertyInfo (typeDescriptorContext, valueSources);
			}

			if (HasStandardValues (propertyDescriptor)) {
				var standardValues = propertyDescriptor.Converter.GetStandardValues (typeDescriptorContext);
				if (standardValues.Count > 0) {
					return new StringStandardValuesPropertyInfo (standardValues, typeDescriptorContext, valueSources);
				}
			}

			return new DescriptorPropertyInfo (typeDescriptorContext, valueSources);
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

		public void Dispose ()
		{
			if (currentEditor != null) {
				currentEditor.PropertyChanged -= CurrentEditor_PropertyChanged;
			}
		}
	}
}

#endif