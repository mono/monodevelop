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
using ICSharpCode.Decompiler.TypeSystem;
using System;
using Mono.Cecil;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace MonoDevelop.AssemblyBrowser
{
	abstract class AssemblyBrowserTypeNodeBuilder : TypeNodeBuilder
	{
		internal AssemblyBrowserWidget Widget {
			get; 
			private set; 
		}
		readonly static CSharpAmbience ambience = new CSharpAmbience ();

		protected CSharpAmbience Ambience {
			get {
				return ambience; 
			}
		}
		
		internal AssemblyLoader GetCecilLoader (ITreeNavigator navigator)
		{
			return navigator.GetParentDataItem<AssemblyLoader> (false);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as IUnresolvedEntity;
				var e2 = otherNode.DataItem as IUnresolvedEntity;
				
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return -1;
				if (e2 == null)
					return 1;
				
				if (e1.SymbolKind != e2.SymbolKind)
					return e2.SymbolKind.CompareTo (e1.SymbolKind);
				
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
		
		protected static bool IsFromAssembly (ITreeNavigator treeBuilder)
		{
			return treeBuilder.GetParentDataItem (typeof(AssemblyLoader), true) != null;
		}

		protected IUnresolvedAssembly GetMainAssembly (ITreeNavigator treeBuilder)
		{
			var loader = (AssemblyLoader)treeBuilder.GetParentDataItem (typeof(AssemblyLoader), true);
			if (loader != null)
				return loader.UnresolvedAssembly;
			return null;
		}

		protected ITypeResolveContext GetContext (ITreeNavigator treeBuilder)
		{
			var mainAssembly = GetMainAssembly (treeBuilder);
			if (mainAssembly != null) {
				var simpleCompilation = new SimpleCompilation (mainAssembly);
				return new SimpleTypeResolveContext (simpleCompilation.MainAssembly);
			}
			// TODO: roslyn port ?
			// var project = (Project)treeBuilder.GetParentDataItem (typeof(Project), true);
			// var compilation = TypeSystemService.GetCompilation (project);
			// return new SimpleTypeResolveContext (compilation.MainAssembly);
			return null;
		}
		
		protected IMember Resolve (ITreeNavigator treeBuilder, IUnresolvedMember member, ITypeDefinition currentType = null)
		{
			var ctx = GetContext (treeBuilder);
			return member.CreateResolved (currentType != null ? ctx.WithCurrentTypeDefinition (currentType) : ctx);
		}
		
		protected IType Resolve (ITreeNavigator treeBuilder, IUnresolvedTypeDefinition type)
		{
			var mainAssembly = GetMainAssembly (treeBuilder);
			if (mainAssembly != null) {
				var simpleCompilation = new SimpleCompilation (mainAssembly);
				return type.Resolve (new SimpleTypeResolveContext (simpleCompilation.MainAssembly));
			}
			// TODO: roslyn port ?
			// var project = (Project)treeBuilder.GetParentDataItem (typeof(Project), true);
			// var ctx = TypeSystemService.GetCompilation (project);
			// return ctx.MainAssembly.GetTypeDefinition (type.Namespace, type.Name, type.TypeParameters.Count);
			return null;
		}
	}
}
