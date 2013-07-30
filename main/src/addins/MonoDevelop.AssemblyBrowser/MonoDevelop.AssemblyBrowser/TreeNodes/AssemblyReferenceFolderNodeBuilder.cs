// 
// AssemblyReferenceFolderNodeBuilder.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System;
using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;
using System.IO;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyReferenceFolderNodeBuilder : AssemblyBrowserTypeNodeBuilder
	{
		public AssemblyReferenceFolderNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override Type NodeDataType {
			get { return typeof(AssemblyReferenceFolder); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "References";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = MonoDevelop.Core.GettextCatalog.GetString ("References");
			nodeInfo.Icon = Context.GetIcon (Stock.OpenReferenceFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			var referenceFolder = (AssemblyReferenceFolder)dataObject;
			var wrapper = (AssemblyLoader)ctx.GetParentDataItem (typeof (AssemblyLoader), false);
			
			foreach (AssemblyNameReference assemblyNameReference in referenceFolder.AssemblyReferences) {
				try {
					string assemblyFile = wrapper.LookupAssembly (assemblyNameReference.FullName);
					if (assemblyFile != null && System.IO.File.Exists (assemblyFile)) {
						ctx.AddChild (assemblyNameReference);
					} else {
						ctx.AddChild (new Error (MonoDevelop.Core.GettextCatalog.GetString ("Can't load:") + assemblyNameReference.FullName));
					}
				} catch (Exception) {
					//	ctx.AddChild (new Error (MonoDevelop.Core.GettextCatalog.GetString ("Error while loading:") + assemblyNameReference.FullName + "/" + e.Message));
				}
			}
			
			foreach (ModuleReference moduleRef in referenceFolder.ModuleReferences) {
				ctx.AddChild (moduleRef);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}
	}
}
