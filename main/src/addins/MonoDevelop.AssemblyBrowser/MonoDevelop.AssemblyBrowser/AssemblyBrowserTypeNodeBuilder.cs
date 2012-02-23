// 
// BaseTypeFolderNodeBuilder.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide.Gui.Components;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	abstract class AssemblyBrowserTypeNodeBuilder : TypeNodeBuilder
	{
		internal AssemblyBrowserWidget Widget {
			get; 
			private set; 
		}
		
		protected MonoDevelop.TypeSystem.Ambience Ambience {
			get {
				return Widget.Ambience; 
			}
		}
		
		internal CecilLoader CecilLoader {
			get {
				return Widget.CecilLoader;
			}
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as IUnresolvedEntity;
				var e2 = otherNode.DataItem as IUnresolvedEntity;
				Console.WriteLine (thisNode.DataItem + " --- " + otherNode.DataItem);
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return -1;
				if (e2 == null)
					return 1;
				
				if (e1.EntityType != e2.EntityType)
					return e2.EntityType.CompareTo (e1.EntityType);
				
				return e1.Name.CompareTo (e2.Name);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}

		public AssemblyBrowserTypeNodeBuilder (AssemblyBrowserWidget assemblyBrowserWidget)
		{
			this.Widget = assemblyBrowserWidget;
		}
		
		protected IMember Resolve (ITreeNavigator treeBuilder, IUnresolvedMember member)
		{
			var mainAssembly = (IUnresolvedAssembly)treeBuilder.GetParentDataItem (typeof(IUnresolvedAssembly), true);
			if (mainAssembly == null)
				throw new NullReferenceException ("mainAssembly not found");
			var simpleCompilation = new SimpleCompilation (mainAssembly);
			return member.CreateResolved (new SimpleTypeResolveContext (simpleCompilation.MainAssembly));
		}
		
		protected IType Resolve (ITreeNavigator treeBuilder, IUnresolvedTypeDefinition member)
		{
			var mainAssembly = (IUnresolvedAssembly)treeBuilder.GetParentDataItem (typeof(IUnresolvedAssembly), true);
			if (mainAssembly == null)
				throw new NullReferenceException ("mainAssembly not found");
			var simpleCompilation = new SimpleCompilation (mainAssembly);
			return member.Resolve (new SimpleTypeResolveContext (simpleCompilation.MainAssembly));
		}
	}
}
