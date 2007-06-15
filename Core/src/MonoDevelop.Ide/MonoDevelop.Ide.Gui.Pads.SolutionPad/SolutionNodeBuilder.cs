//
// SolutionNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class SolutionNodeBuilder : TypeNodeBuilder
	{
		public SolutionNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(Solution); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "SolutionNode";
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SolutionNodeCommandHandler); }
		}
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/CombineBrowserNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Solution solution = dataObject as Solution;
			
			if (solution != null) {
				switch (solution.Items.Count) {
					case 0:
						label = GettextCatalog.GetString ("Solution {0}", solution.Name);
						break;
					case 1:
						label = GettextCatalog.GetString ("Solution {0} (1 entry)", solution.Name);
						break;
					default:
						label = GettextCatalog.GetString ("Solution {0} ({1} entries)", solution.Name, solution.Items.Count);
						break;
				}
			}
			icon = Context.GetIcon (Stock.CombineIcon);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Solution solution = dataObject as Solution;
			if (solution != null) {
				foreach (SolutionItem item in solution.Items) {
					if (item.Parent == null) {
						ctx.AddChild (item);
					}
				}
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Solution solution = dataObject as Solution;
			return solution != null && solution.Items.Count > 0;
		}
	}
	
	public class SolutionNodeCommandHandler : NodeCommandHandler
	{
	}
}
