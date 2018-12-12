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

		//private readonly List<ReflectionPropertyInfo> properties = new List<ReflectionPropertyInfo> ();
		//private readonly List<ReflectionEventInfo> events = new List<ReflectionEventInfo> ();

		public PropertyPadObjectEditor (Tuple<object, object []> target)
		{
			this.target = target.Item1;
			foreach (object propertyProvider in target.Item2) {
				var props = GetProperties (propertyProvider, null);

				for (int i = 0; i < props.Count; i++) {
					var prop = props [i] as PropertyDescriptor;
					if (prop.IsBrowsable) {
						properties.Add (PropertyPadPropertyInfoFactory.PropertyInfo (prop, propertyProvider));
					}
				}
			}
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

		public Task<AssignableTypesResult> GetAssignableTypesAsync (IPropertyInfo property, bool childTypes)
		{
			return GetAssignableTypes (property.RealType, childTypes);
		}

		internal static Task<AssignableTypesResult> GetAssignableTypes (ITypeInfo type, bool childTypes)
		{
			return Task.Run (() => {
				var types = AppDomain.CurrentDomain.GetAssemblies ().SelectMany (a => a.GetTypes ()).AsParallel ()
					.Where (t => t.Namespace != null && !t.IsAbstract && !t.IsInterface && t.IsPublic && t.GetConstructor (Type.EmptyTypes) != null);

				Type realType = ReflectionEditorProvider.GetRealType (type);
				if (childTypes) {
					var generic = realType.GetInterface ("ICollection`1");
					if (generic != null) {
						realType = generic.GetGenericArguments () [0];
					} else {
						realType = typeof (object);
					}
				}

				types = types.Where (t => realType.IsAssignableFrom (t));

				return new AssignableTypesResult (types.Select (t => {
					string asmName = t.Assembly.GetName ().Name;
					return new TypeInfo (new AssemblyInfo (asmName, isRelevant: asmName.StartsWith ("Xamarin")), t.Namespace, t.Name);
				}).ToList ());
			});
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

			var info = property as DescriptorPropertyInfo;
			if (info == null)
				throw new ArgumentException ();

			T value = await info.GetValueAsync<T> (this.target);

			return new ValueInfo<T> {
				Source = ValueSource.Local,
				Value = value
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

		public async Task SetValueAsync<T> (IPropertyInfo property, ValueInfo<T> value, PropertyVariation variations = null)
		{
			if (property == null)
				throw new ArgumentNullException (nameof (property));

			if (property is DescriptorPropertyInfo info && info.CanWrite) {
				info.SetValue (this.target, value.Value);
				OnPropertyChanged (info);
			}			
			throw new ArgumentException ($"Property should be a writeable {nameof (DescriptorPropertyInfo)}.", nameof (property));
		}

		protected virtual void OnPropertyChanged (IPropertyInfo property)
		{
			PropertyChanged?.Invoke (this, new EditorPropertyChangedEventArgs (property));
		}
	}
}

#endif