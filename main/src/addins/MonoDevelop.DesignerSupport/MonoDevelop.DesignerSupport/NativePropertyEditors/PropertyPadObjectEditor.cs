//
// PropertyPadObjectEditor.cs
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
using Xamarin.PropertyEditing.Reflection;
using System.Linq;
using System.ComponentModel;
using System.Collections;

namespace MonoDevelop.DesignerSupport
{
	class PropertyPadObjectEditor
		: IObjectEditor, INameableObject, IObjectEventEditor
	{
		private readonly object target;
		public string Name {
			get;
			private set;
		}

		private readonly List<IPropertyInfo> properties = new List<IPropertyInfo> ();
		private static readonly IObjectEditor [] EmptyDirectChildren = new IObjectEditor [0];
		private readonly List<PropertyDescriptorEventInfo> events = new List<PropertyDescriptorEventInfo> ();

		public object Target => this.target;

		public ITypeInfo TargetType => ToTypeInfo (Target.GetType ());

		public static ITypeInfo ToTypeInfo (Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new TypeInfo (new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}

		public IReadOnlyCollection<IPropertyInfo> Properties => this.properties;

		public IReadOnlyDictionary<IPropertyInfo, KnownProperty> KnownProperties => null;

		public IObjectEditor Parent => throw new NotImplementedException ();

		public IReadOnlyList<IObjectEditor> DirectChildren => EmptyDirectChildren;

		public IReadOnlyCollection<IEventInfo> Events => this.events;

		public event EventHandler<EditorPropertyChangedEventArgs> PropertyChanged;

		public PropertyPadObjectEditor (Tuple<object, object []> target)
		{
			this.target = target.Item1;
			foreach (object propertyProvider in target.Item2) {
				var props = GetProperties (propertyProvider, null);

				for (int i = 0; i < props.Count; i++) {
					var prop = props [i] as PropertyDescriptor;
					if (prop.IsBrowsable) {
						properties.Add (CreatePropertyInfo (prop, propertyProvider));
					}
				}
			}
		}

		bool HasStandardValues (PropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.Converter.GetStandardValuesSupported ()) {
				if (!propertyDescriptor.Converter.GetStandardValuesExclusive () && propertyDescriptor.Converter.CanConvertFrom (typeof (string))) {
					return true;
				}
			}
			return false;
		}

		PropertyDescriptor GetPropertyDescriptor (PropertyDescriptorCollection collection, string name)
		{
			for (int i = 0; i < collection.Count; i++) {
				if (collection[i].Name == name) {
					return collection [i];
				}
			}
			return null;
		}

		protected IPropertyInfo CreatePropertyInfo (PropertyDescriptor propertyDescriptor, object PropertyProvider)
		{
			var valueSources = ValueSources.Local | ValueSources.Default;
			if (propertyDescriptor.PropertyType.IsEnum) {
				return new EnumDescriptorPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			if (propertyDescriptor.PropertyType.IsAssignableFrom (typeof (Core.FilePath))) {
				var isDirectoryPropertyDescriptor = GetPropertyDescriptor (propertyDescriptor.GetChildProperties (), nameof (Core.FilePath.IsDirectory));
				if (isDirectoryPropertyDescriptor != null && (bool)isDirectoryPropertyDescriptor.GetValue (target)) {
					return new DirectoryPathPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
				}
				return new FilePathPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			if (HasStandardValues (propertyDescriptor)) {
				return new StringStandardValuesPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
			}

			return new DescriptorPropertyInfo (propertyDescriptor, PropertyProvider, valueSources);
		}

		public static PropertyDescriptorCollection GetProperties (object component, Attribute [] attributes)
		{
			if (component == null)
				return new PropertyDescriptorCollection (new PropertyDescriptor [] { });
			return TypeDescriptor.GetProperties (component);
		}

		#region NOT SUPORTED

		public Task AttachHandlerAsync (IEventInfo ev, string handlerName)
		{
			throw new NotImplementedException ();
		}

		public Task DetachHandlerAsync (IEventInfo ev, string handlerName)
		{
			throw new NotImplementedException ();
		}

		#endregion

		public async Task<AssignableTypesResult> GetAssignableTypesAsync (IPropertyInfo property, bool childTypes)
		{
			return new AssignableTypesResult (Array.Empty<ITypeInfo> ());
		}

		public Task<IReadOnlyList<string>> GetHandlersAsync (IEventInfo ev)
		{
			if (ev == null)
				throw new ArgumentNullException (nameof (ev));

			if (!(ev is Xamarin.PropertyEditing.Reflection.ReflectionEventInfo info))
				throw new ArgumentException ();

			return Task.FromResult (info.GetHandlers (this.target));
		}

		public Task<string> GetNameAsync ()
		{
			return Task.FromResult (Name);
		}

		public Task<IReadOnlyCollection<PropertyVariation>> GetPropertyVariantsAsync (IPropertyInfo property)
		{
			return Task.FromResult<IReadOnlyCollection<PropertyVariation>> (new PropertyVariation [0]);
		}

		public async Task<ValueInfo<T>> GetValueAsync<T> (IPropertyInfo property, PropertyVariation variations = null)
		{
			if (property == null)
				throw new ArgumentNullException (nameof (property));

			if (!(property is DescriptorPropertyInfo info))
				throw new ArgumentException ();

			T value = await info.GetValueAsync<T> (this.target);

			return new ValueInfo<T> {
				Value = value,
				Source = ValueSource.Local,
				//ValueDescriptor = valueInfoString.ValueDescriptor,
				//CustomExpression = valueString
			};
		}

		public Task RemovePropertyVariantAsync (IPropertyInfo property, PropertyVariation variant)
		{
			return Task.CompletedTask;
		}

		public Task SetNameAsync (string name)
		{
			Name = name;
			return Task.FromResult (true);
		}

		public Task SetValueAsync<T> (IPropertyInfo propertyInfo, ValueInfo<T> valueInfo, PropertyVariation variations = null)
		{
			if (propertyInfo == null)
				throw new ArgumentNullException (nameof (propertyInfo));

			if (propertyInfo is DescriptorPropertyInfo info && info.CanWrite) {
				info.SetValue (this.target, valueInfo.Value);
				OnPropertyChanged (info);
			}

			PropertyChanged?.Invoke (this, new EditorPropertyChangedEventArgs (propertyInfo));
			return Task.CompletedTask;
		}

		protected virtual void OnPropertyChanged (IPropertyInfo property)
		{
			PropertyChanged?.Invoke (this, new EditorPropertyChangedEventArgs (property));
		}
	}
}

#endif