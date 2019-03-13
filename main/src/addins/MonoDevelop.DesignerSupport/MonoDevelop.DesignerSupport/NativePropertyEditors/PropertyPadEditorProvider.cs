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
using Xamarin.PropertyEditing.Common;
using Xamarin.PropertyEditing.Reflection;
using System.Reflection;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport
{
	class PropertyPadEditorProvider
	: IEditorProvider
	{
		Tuple<object, object[]> currentObject;
		IObjectEditor currentEditor;

		public IReadOnlyDictionary<Type, ITypeInfo> KnownTypes {
			get;
		} = new Dictionary<Type, ITypeInfo> ();

		public Task<IObjectEditor> GetObjectEditorAsync (object item)
		{
			if (item is Tuple<object, object []> tuple) {
				this.currentObject = tuple;
				this.currentEditor = new PropertyPadObjectEditor (tuple); ;
				return Task.FromResult (currentEditor);
			}
			return Task.FromResult<IObjectEditor> (null);
		}

		public Task<object> CreateObjectAsync (ITypeInfo type)
		{
			var realType = Type.GetType ($"{type.NameSpace}.{type.Name}, {type.Assembly.Name}");
			if (realType == null)
				return Task.FromResult<object> (null);
			return Task.FromResult (Activator.CreateInstance (realType));
		}

		public Task<IReadOnlyCollection<IPropertyInfo>> GetPropertiesForTypeAsync (ITypeInfo type)
			=> Task.FromResult<IReadOnlyCollection<IPropertyInfo>> (new List<IPropertyInfo> ());

		public Task<AssignableTypesResult> GetAssignableTypesAsync (ITypeInfo type, bool childTypes)
			=> Task.FromResult (new AssignableTypesResult (new List<ITypeInfo> ()));

		public Task<IReadOnlyList<object>> GetChildrenAsync (object item) 
			=>  Task.FromResult<IReadOnlyList<object>> (Array.Empty<object> ());
	}
}

#endif