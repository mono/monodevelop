//
// LinkedFilesFolderNodeBuilder.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class LinkedFilesFolderNodeBuilder: TypeNodeBuilder
	{
		Gdk.Pixbuf overlay;
		
		Gdk.Pixbuf openFolder;
		Gdk.Pixbuf closedFolder;
		
		public override Type NodeDataType {
			get { return typeof(LinkedFilesFolder); }
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();
			overlay = Gdk.Pixbuf.LoadFromResource ("Icons.16x16.LinkOverlay.png");
			openFolder = IdeApp.Services.Resources.GetIcon (Stock.OpenFolder, Gtk.IconSize.Menu).Copy ();
			closedFolder = IdeApp.Services.Resources.GetIcon (Stock.ClosedFolder, Gtk.IconSize.Menu).Copy ();
			
			overlay.Composite (openFolder,
				0,  0,
				openFolder.Width, openFolder.Width,
				0, 0,
				1, 1, Gdk.InterpType.Bilinear, 255); 
			
			overlay.Composite (closedFolder,
				0,  0,
				closedFolder.Width, closedFolder.Width,
				0, 0,
				1, 1, Gdk.InterpType.Bilinear, 255); 
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			// Don't localize this string.
			return "ExternalFiles";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("External Files");
			icon = openFolder;
			closedIcon = closedFolder;
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = ((LinkedFilesFolder)dataObject).Project;
			foreach (ProjectFile file in project.Files)
				if (file.IsExternalToProject)
					builder.AddChild (file);
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectFile || otherNode.DataItem is ProjectFolder || otherNode.DataItem is SystemFile)
				return -1;
			else
				return 1;
		}
		
		public override object GetParentObject (object dataObject)
		{
			return ((LinkedFilesFolder) dataObject).Project;
		}
	}
}
