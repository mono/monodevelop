// DotNetProjectSubtype.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.Addins;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects.Extensions
{
	public class DotNetProjectSubtypeNode: ExtensionNode
	{
		[NodeAttribute]
		string guid;
		
		[NodeAttribute]
		string type;
		
		[NodeAttribute]
		string import;
		
		Type itemType;

		public string Import {
			get {
				return import;
			}
		}
		
		public Type Type {
			get {
				if (itemType == null) {
					itemType = Addin.GetType (type, true);
					if (!typeof(MonoDevelop.Projects.DotNetProject).IsAssignableFrom (itemType))
						throw new InvalidOperationException ("Type must be a subclass of DotNetProject");
				}
				return itemType;
			}
		}
		
		public bool SupportsType (string guid)
		{
			return string.Compare (this.guid, guid, true) == 0;
		}
		
		public DotNetProject CreateInstance (string language)
		{
			return (DotNetProject) Activator.CreateInstance (Type, language);
		}
		
		public virtual bool CanHandleItem (SolutionEntityItem item)
		{
			return Type.IsAssignableFrom (item.GetType ());
		}
		
		public virtual void InitializeHandler (SolutionEntityItem item)
		{
			MSBuildProjectHandler h = (MSBuildProjectHandler) ProjectExtensionUtil.GetItemHandler (item);
			if (!string.IsNullOrEmpty (import))
				h.TargetImports.AddRange (import.Split (':'));
			h.SubtypeGuids.Add (guid);
		}
	}
}
