// 
// AssemblyReferenceNodeBuilder.cs
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

using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using ICSharpCode.Decompiler.Metadata;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyReferenceNodeBuilder : TypeNodeBuilder
	{
		internal AssemblyBrowserWidget Widget {
			get; 
			private set; 
		}
		
		public override Type NodeDataType {
			get { return typeof(AssemblyReference); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ReferenceNodeCommandHandler); }
		}
		
		public AssemblyReferenceNodeBuilder (AssemblyBrowserWidget assemblyBrowserWidget)
		{
			this.Widget = assemblyBrowserWidget;
		}
		
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var reference = (AssemblyReference)dataObject;
			return reference.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var reference = (AssemblyReference)dataObject;
			nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (reference.Name);
			nodeInfo.Icon = Context.GetIcon (Stock.Reference);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as AssemblyReference;
				var e2 = otherNode.DataItem as AssemblyReference;
				
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return 1;
				if (e2 == null)
					return -1;
				
				return string.Compare(e1.Name, e2.Name, StringComparison.Ordinal);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}
	}
	
	class ReferenceNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			var reference = (AssemblyReference)CurrentNode.DataItem;
			if (reference == null)
				return;
			var loader = (AssemblyLoader)CurrentNode.GetParentDataItem (typeof(AssemblyLoader), false);
			string fileName = loader.LookupAssembly (reference.FullName);
			if (fileName == null)
				return;
			var builder = (AssemblyReferenceNodeBuilder)this.CurrentNode.TypeNodeBuilder;
			builder.Widget.AddReferenceByFileName (fileName);
		}
	}

}
