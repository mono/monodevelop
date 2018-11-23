/* 
 * PropertyPad.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.PropertyEditing.Reflection;
using Xamarin.PropertyEditing;
using System.Collections.Specialized;

namespace MonoDevelop.DesignerSupport
{
	class MockBindingProvider
		: IBindingProvider
	{
		public Task<IReadOnlyList<BindingSource>> GetBindingSourcesAsync (object target, IPropertyInfo property)
		{
			return Task.FromResult<IReadOnlyList<BindingSource>> (new [] {
				new BindingSourceInstance (RelativeSelf, $"Bind [{target.GetType().Name}] to itself."),
				StaticResource,
				Ancestor
			});
		}

		public static ITypeInfo ToTypeInfo (Type type, bool isRelevant = true)
		{
			var asm = type.Assembly.GetName ().Name;
			return new TypeInfo (new AssemblyInfo (asm, isRelevant), type.Namespace, type.Name);
		}


		public Task<AssignableTypesResult> GetSourceTypesAsync (BindingSource source, object target)
		{
			return MockEditorProvider.GetAssignableTypes (ToTypeInfo (target.GetType ()), childTypes: false);
		}

		public Task<IReadOnlyList<object>> GetRootElementsAsync (BindingSource source, object target)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			if (source.Type != BindingSourceType.Object && source.Type != BindingSourceType.SingleObject)
				throw new ArgumentException ("source.Type was not Object", nameof (source));

			if (source is BindingSourceInstance instance)
				source = instance.Original;

			if (source == RelativeSelf)
				return Task.FromResult<IReadOnlyList<object>> (new [] { target });
			if (source == StaticResource)
				return MockResourceProvider.GetResourceSourcesAsync (target).ContinueWith (t => (IReadOnlyList<object>)t.Result);

			throw new NotImplementedException ();
		}

		public async Task<ILookup<ResourceSource, Resource>> GetResourcesAsync (BindingSource source, object target)
		{
			var results = await this.resources.GetResourcesAsync (target, CancellationToken.None).ConfigureAwait (false);
			return results.ToLookup (r => r.Source);
		}

		public Task<IReadOnlyList<Resource>> GetValueConverterResourcesAsync (object target)
		{
			return Task.FromResult<IReadOnlyList<Resource>> (Array.Empty<Resource> ());
		}

		private readonly MockResourceProvider resources = new MockResourceProvider ();

		private static readonly BindingSource Ancestor = new BindingSource ("RelativeSource FindAncestor", BindingSourceType.Type);
		private static readonly BindingSource RelativeSelf = new BindingSource ("RelativeSource Self", BindingSourceType.SingleObject);
		private static readonly BindingSource StaticResource = new BindingSource ("StaticResource", BindingSourceType.Resource);

		private class BindingSourceInstance
			: BindingSource
		{
			public BindingSourceInstance (BindingSource original, string description)
				: base (original.Name, original.Type, description)
			{
				Original = original;
			}

			public BindingSource Original {
				get;
			}
		}
	}

	public class MockBinding
	{
		public string Path {
			get;
			set;
		}

		public BindingSource Source {
			get;
			set;
		}

		public object SourceParameter {
			get;
			set;
		}

		public Resource Converter {
			get;
			set;
		}

		public string StringFormat {
			get;
			set;
		}

		public bool IsAsync {
			get;
			set;
		}

		public int TypeLevel {
			get;
			set;
		}
	}
}

#endif