//
// CodeBehindClassBuilder.cs : Displays CodeBehind classes in the Solution Pad
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace AspNetAddIn
{
	class CodeBehindClassBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof (IClass); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ClassCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((IClass) dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IClass cls = (IClass) dataObject;
			icon = Context.GetIcon (MonoDevelop.Ide.Gui.IdeApp.Services.Icons.GetIcon (cls));
			string fileName = (cls.Region.FileName == null)? "" : System.IO.Path.GetFileName (cls.Region.FileName);
			label = String.Format ("Inherits {0} in {1}", cls.Name, fileName);
		}
	}
	
	class ClassCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			IClass cls = (IClass) CurrentNode.DataItem;
			if (cls.Region.FileName != null) {
				int line = cls.Region.BeginLine;
				string file = cls.Region.FileName;
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (file, Math.Max (1, line), 1, true);
			}
		}
	}
}
