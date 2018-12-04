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
using Xamarin.PropertyEditing.Common;
using Xamarin.PropertyEditing.Reflection;
using System.Reflection;

namespace MonoDevelop.DesignerSupport
{
	class MacPropertyPadEditorProvider
	: IEditorProvider
	{
		public MacPropertyPadEditorProvider (IResourceProvider resources = null)
		{
			this.resources = resources;
		}

		object [] propertyProviders = new object [0];
		object currentObject;
		IObjectEditor currentEditor;

		public MacPropertyPadEditorProvider (IObjectEditor editor)
		{
			editorCache.Add (editor.Target, editor);
		}

		public IReadOnlyDictionary<Type, ITypeInfo> KnownTypes {
			get;
		} = new Dictionary<Type, ITypeInfo> ();

		public Task<IObjectEditor> GetObjectEditorAsync (object item)
		{
			if (this.currentObject == item)
				return Task.FromResult (currentEditor);
			this.currentObject = item;

			if (editorCache.TryGetValue (item, out IObjectEditor cachedEditor)) {
				this.currentEditor = cachedEditor;
				return Task.FromResult (cachedEditor);
			}

			var editor = new TestPropertyObjectEditor (item, propertyProviders);
			editorCache.Add (item, editor);
			return Task.FromResult ((IObjectEditor) editor);
		}

		public async Task<IReadOnlyCollection<IPropertyInfo>> GetPropertiesForTypeAsync (ITypeInfo type)
		{
			Type realType = ReflectionEditorProvider.GetRealType (type);
			if (realType == null)
				return Array.Empty<IPropertyInfo> ();

			var properties = new List<ReflectionPropertyInfo> ();
			foreach (var item in realType.GetRuntimeProperties ()) {
				properties.Add (new ReflectionPropertyInfo (item));
			}
			return properties;
		}

		public Task<AssignableTypesResult> GetAssignableTypesAsync (ITypeInfo type, bool childTypes)
		{
			if (type == KnownTypes [typeof (CommonValueConverter)])
				return Task.FromResult (new AssignableTypesResult (new [] { type }));

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
					return new Xamarin.PropertyEditing.TypeInfo (new AssemblyInfo (asmName, isRelevant: asmName.StartsWith ("Xamarin")), t.Namespace, t.Name);
				}).ToList ());
			});
		}

		IObjectEditor ChooseEditor (object item)
		 => new ReflectionObjectEditor (item);

		public Task<object> CreateObjectAsync (ITypeInfo type)
		{
			var realType = Type.GetType ($"{type.NameSpace}.{type.Name}, {type.Assembly.Name}");
			if (realType == null)
				return Task.FromResult<object> (null);

			return Task.FromResult (Activator.CreateInstance (realType));
		}

		public Task<IReadOnlyList<object>> GetChildrenAsync (object item)
		{
			return Task.FromResult<IReadOnlyList<object>> (Array.Empty<object> ());
		}

		public Task<IReadOnlyDictionary<Type, ITypeInfo>> GetKnownTypesAsync (IReadOnlyCollection<Type> knownTypes)
		{
			return Task.FromResult<IReadOnlyDictionary<Type, ITypeInfo>> (new Dictionary<Type, ITypeInfo> ());
		}

		readonly IResourceProvider resources;
		readonly Dictionary<object, IObjectEditor> editorCache = new Dictionary<object, IObjectEditor> ();

		public void SetPropertyProviders (object [] propertyProviders)
		{
			if (this.propertyProviders != null) {
				foreach (var old in this.propertyProviders.OfType<IDisposable> ())
					old.Dispose ();
			}
			this.propertyProviders = propertyProviders;
		}
	}
}

#endif