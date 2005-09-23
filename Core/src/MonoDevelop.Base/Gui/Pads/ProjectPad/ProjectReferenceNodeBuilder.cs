//
// ProjectReferenceNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Pads.ProjectPad
{
	public class ProjectReferenceNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectReference); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectReferenceNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProjectReference)dataObject).Reference;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ReferenceNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			
			switch (pref.ReferenceType) {
				case ReferenceType.Typelib:
					int index = pref.Reference.IndexOf("|");
					if (index > 0) {
						label = pref.Reference.Substring(0, index);
					} else {
						label = pref.Reference;
					}
					break;
				case ReferenceType.Project:
					label = pref.Reference;
					break;
				case ReferenceType.Assembly:
					label = Path.GetFileName(pref.Reference);
					break;
				case ReferenceType.Gac:
					label = pref.Reference.Split(',')[0];
					break;
				default:
					throw new NotImplementedException("reference type : " + pref.ReferenceType);
			}
			
			icon = Context.GetIcon (Stock.Reference);
		}
	}
	
	public class ProjectReferenceNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			ProjectReference pref = (ProjectReference) CurrentNode.DataItem;
			Project project = CurrentNode.GetParentDataItem (typeof(Project), false) as Project;
			project.ProjectReferences.Remove (pref);
			Runtime.ProjectService.SaveCombine ();
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy;
		}
	}
}
