// OutputOptionsPanel.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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

using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui.Dialogs;

namespace PyBinding.Gui
{
	public class OutputOptionsPanel : MultiConfigItemOptionsPanel
	{
		PythonOptionsWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			widget = new PythonOptionsWidget ();
			widget.Show ();
			return widget;
		}

		public override void LoadConfigData ()
		{
			PythonConfiguration config = CurrentConfiguration as PythonConfiguration;
			
			widget.DefaultModule = config.Module;
			widget.Optimize = config.Optimize;
		}

		public override void ApplyChanges ()
		{
			PythonConfiguration config = CurrentConfiguration as PythonConfiguration;
			
			config.Module = widget.DefaultModule;
			config.Optimize = widget.Optimize;
		}
	}
}
