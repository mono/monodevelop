//
// ReferenceFolderNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class ReferenceFolderNodeBuilder : TypeNodeBuilder
	{
//		AssemblyBrowserWidget widget;
		
		public ReferenceFolderNodeBuilder (AssemblyBrowserWidget widget)
		{
//			this.widget = widget;
		}
		
		public override Type NodeDataType {
			get { return typeof(ReferenceFolder); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "References";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = MonoDevelop.Core.GettextCatalog.GetString ("References");
			icon       = Context.GetIcon (Stock.OpenReferenceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			ReferenceFolder referenceFolder = (ReferenceFolder)dataObject;
			foreach (AssemblyNameReference assemblyNameReference in referenceFolder.ModuleDefinition.AssemblyReferences) {
//				AssemblyDefinition assembly = null;
				try {
					string assemblyFile = Runtime.SystemAssemblyService.DefaultRuntime.GetAssemblyLocation (assemblyNameReference.FullName);
					if (assemblyFile != null && System.IO.File.Exists (assemblyFile)) {
						ctx.AddChild (new Reference (assemblyFile));
					} else {
						ctx.AddChild (new Error (MonoDevelop.Core.GettextCatalog.GetString ("Can't load:") + assemblyNameReference.FullName));
					}
				} catch (Exception) {
				//	ctx.AddChild (new Error (MonoDevelop.Core.GettextCatalog.GetString ("Error while loading:") + assemblyNameReference.FullName + "/" + e.Message));
				}
			}
			foreach (ModuleReference moduleRef in referenceFolder.ModuleDefinition.ModuleReferences) {
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
