//
// MemberNodeBuilder.cs
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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassBrowser
{
	public class MemberNodeBuilder: MonoDevelop.Ide.Gui.Components.TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(MonoDevelop.Projects.Dom.IMember); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Member"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(MemberNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IMember member = dataObject as IMember;
			return member.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IMember member = dataObject as IMember;
			
			label = AmbienceService.GetAmbience (member).GetString (member, OutputFlags.ClassBrowserEntries);
			icon  = MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (thisNode.DataItem is System.IComparable) {
				return ((System.IComparable)thisNode.DataItem).CompareTo (otherNode.DataItem);
			}
			return -1;
		}
		
		internal class MemberNodeCommandHandler: NodeCommandHandler
		{
			public override void ActivateItem ()
			{			
				IMember member = CurrentNode.DataItem  as IMember;
				if (member.DeclaringType != null) {
					IdeApp.ProjectOperations.JumpToDeclaration (member);
				}
			}
		}
	}
}
