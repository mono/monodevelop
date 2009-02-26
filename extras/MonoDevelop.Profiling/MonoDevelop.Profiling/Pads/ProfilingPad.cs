//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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

using Gtk;
using System;
using System.Resources;

using MonoDevelop.Core;
using Stock = MonoDevelop.Core.Gui.Stock;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.DesignerSupport.PropertyGrid;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Profiling
{
	public class ProfilingPad : TreeViewPad, IPropertyPadProvider
	{
		private VBox vbox;
		
		public ProfilingPad ()
		{
			vbox = new VBox ();
			
			Toolbar toolbar = IdeApp.CommandService.CreateToolbar ("/MonoDevelop/Profiling/ToolBar/ProfilingPad");
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			
			vbox.PackStart (toolbar, false, true, 0);
		}
		
		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string menuPath)
		{
			base.Initialize (builders, options, menuPath);
			vbox.PackStart (base.Control, true, true, 0);
			vbox.ShowAll ();

			TreeView.LoadTree (ProfilingService.ProfilingSnapshots);
		}
		
		public override Widget Control {
			get { return vbox; }
		}
		
		public object GetActiveComponent ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav != null)
				return nav.DataItem;
			return null;
		}
		
		public object GetProvider ()
		{
			return null;
		}

		public void OnEndEditing (object obj) {}
		public void OnChanged (object obj) {}
	}
}
